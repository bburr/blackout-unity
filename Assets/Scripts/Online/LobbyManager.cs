using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : IDisposable
{
    private const string KeyRelayCode = nameof(LocalLobby.RelayCode);
    private const string KeyLobbyState = nameof(LocalLobby.LocalLobbyState);
    
    private Lobby _currentLobby;
    private LobbyEventCallbacks _lobbyEventCallbacks = new LobbyEventCallbacks();
    
    private Task _heartBeatTask;
    
    #region Rate Limiting

    public enum RequestType
    {
        Join,
        Host
    }
    
    public bool InLobby()
    {
        if (_currentLobby == null)
        {
            Debug.LogWarning("LobbyManager not currently in a lobby");
            return false;
        }

        return true;
    }
    
    public ServiceRateLimiter GetRateLimit(RequestType type)
    {
        return type switch
        {
            RequestType.Join => _joinCooldown,
            RequestType.Host => _createCooldown,
            _ => _queryCooldown
        };
    }
    
    ServiceRateLimiter _queryCooldown = new ServiceRateLimiter(1, 1f);
    ServiceRateLimiter _createCooldown = new ServiceRateLimiter(2, 6f);
    ServiceRateLimiter _joinCooldown = new ServiceRateLimiter(2, 6f);
    ServiceRateLimiter _getLobbyCooldown = new ServiceRateLimiter(1, 1f);
    ServiceRateLimiter _deleteLobbyCooldown = new ServiceRateLimiter(2, 1f);
    ServiceRateLimiter _updateLobbyCooldown = new ServiceRateLimiter(5, 5f);
    ServiceRateLimiter _updatePlayerCooldown = new ServiceRateLimiter(5, 5f);
    ServiceRateLimiter _leaveLobbyOrRemovePlayer = new ServiceRateLimiter(5, 1);
    ServiceRateLimiter _heartBeatCooldown = new ServiceRateLimiter(5, 30);
    
    #endregion

    private Dictionary<string, PlayerDataObject> CreateInitialPlayerData(LocalPlayer player)
    {
        var data = new Dictionary<string, PlayerDataObject>();

        // todo
        
        return data;
    }

    public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, bool isPrivate, LocalPlayer localPlayer)
    {
        if (_createCooldown.IsCoolingDown)
        {
            Debug.LogWarning("Rate limit reached for create lobby");
            return null;
        }

        await _createCooldown.QueueUntilCooldown();

        try
        {
            var uasId = AuthenticationService.Instance.PlayerId;

            var options = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Player = new Player(id: uasId, data: CreateInitialPlayerData(localPlayer))
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            StartHeartBeat();

            return _currentLobby;
        }
        catch (Exception e)
        {
            Debug.LogError($"Create lobby failed: {e}");
            return null;
        }
    }

    public async Task<Lobby> JoinLobbyAsync(string lobbyCode, LocalPlayer localUser)
    {
        if (_joinCooldown.IsCoolingDown || lobbyCode == null)
        {
            return null;
        }

        await _joinCooldown.QueueUntilCooldown();

        var uasId = AuthenticationService.Instance.PlayerId;
        var playerData = CreateInitialPlayerData(localUser);

        var options = new JoinLobbyByCodeOptions { Player = new Player(id: uasId, data: playerData) };
        _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);

        return _currentLobby;
    }

    public async Task BindLocalLobbyToRemote(string lobbyId, LocalLobby localLobby)
    {
        _lobbyEventCallbacks.LobbyChanged += async changes =>
        {
            if (changes.LobbyDeleted)
            {
                await LeaveLobbyAsync();
                return;
            }

            if (changes.HostId.Changed)
                localLobby.HostID.Value = changes.HostId.Value;

            if (changes.LastUpdated.Changed)
                localLobby.LastUpdated.Value = changes.LastUpdated.Value.ToFileTimeUtc();

            //Custom Lobby Fields
            if (changes.Data.Changed)
                LobbyChanged();

            if (changes.PlayerJoined.Changed)
                PlayersJoined();

            if (changes.PlayerLeft.Changed)
                PlayersLeft();

            if (changes.PlayerData.Changed)
                PlayerDataChanged();

            void LobbyChanged()
            {
                foreach (var change in changes.Data.Value)
                {
                    var changedValue = change.Value;
                    var changedKey = change.Key;

                    if (changedValue.Removed)
                    {
                        RemoveCustomLobbyData(changedKey);
                    }

                    if (changedValue.Changed)
                    {
                        ParseCustomLobbyData(changedKey, changedValue.Value);
                    }
                }

                void RemoveCustomLobbyData(string changedKey)
                {
                    if (changedKey == KeyRelayCode)
                        localLobby.RelayCode.Value = "";
                }

                void ParseCustomLobbyData(string changedKey, DataObject playerDataObject)
                {
                    if (changedKey == KeyRelayCode)
                        localLobby.RelayCode.Value = playerDataObject.Value;

                    if (changedKey == KeyLobbyState)
                        localLobby.LocalLobbyState.Value = (LobbyState)int.Parse(playerDataObject.Value);

                }
            }

            void PlayersJoined()
            {
                // todo clean up
                foreach (var playerChanges in changes.PlayerJoined.Value)
                {
                    Player joinedPlayer = playerChanges.Player;

                    var id = joinedPlayer.Id;
                    var index = playerChanges.PlayerIndex;
                    var isHost = localLobby.HostID.Value == id;

                    var newPlayer = new LocalPlayer(id, index, isHost);

                    // foreach (var dataEntry in joinedPlayer.Data)
                    // {
                    // var dataObject = dataEntry.Value;
                    // ParseCustomPlayerData(newPlayer, dataEntry.Key, dataObject.Value);
                    // }

                    localLobby.AddPlayer(index, newPlayer);
                }
            }

            void PlayersLeft()
            {
                foreach (var leftPlayerIndex in changes.PlayerLeft.Value)
                {
                    localLobby.RemovePlayer(leftPlayerIndex);
                }
            }

            void PlayerDataChanged()
            {
                // todo clean up
                foreach (var lobbyPlayerChanges in changes.PlayerData.Value)
                {
                    var playerIndex = lobbyPlayerChanges.Key;
                    var localPlayer = localLobby.GetLocalPlayer(playerIndex);
                    if (localPlayer == null)
                        continue;
                    var playerChanges = lobbyPlayerChanges.Value;
                    if (playerChanges.ConnectionInfoChanged.Changed)
                    {
                        var connectionInfo = playerChanges.ConnectionInfoChanged.Value;
                        Debug.Log(
                            $"ConnectionInfo for player {playerIndex} changed to {connectionInfo}");
                    }

                    if (playerChanges.LastUpdatedChanged.Changed)
                    {
                    }

                    //There are changes on the Player
                    if (playerChanges.ChangedData.Changed)
                    {
                        foreach (var playerChange in playerChanges.ChangedData.Value)
                        {
                            var changedValue = playerChange.Value;

                            //There are changes on some of the changes in the player list of changes

                            if (changedValue.Changed)
                            {
                                if (changedValue.Removed)
                                {
                                    Debug.LogWarning("This Sample does not remove Player Values currently.");
                                    continue;
                                }

                                // var playerDataObject = changedValue.Value;
                                // ParseCustomPlayerData(localPlayer, playerChange.Key, playerDataObject.Value);
                            }
                        }
                    }
                }
            }
        };
        
        _lobbyEventCallbacks.LobbyEventConnectionStateChanged += lobbyEventConnectionState =>
        {
            Debug.Log($"Lobby ConnectionState Changed to {lobbyEventConnectionState}");
        };

        _lobbyEventCallbacks.KickedFromLobby += () =>
        {
            Debug.Log("Left Lobby");
            Dispose();
        };
        
        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, _lobbyEventCallbacks);
    }

    public async Task LeaveLobbyAsync()
    {
        await _leaveLobbyOrRemovePlayer.QueueUntilCooldown();

        if (!InLobby())
        {
            return;
        }

        var playerId = AuthenticationService.Instance.PlayerId;

        await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, playerId);
        _currentLobby = null;
    }
    
    public async Task UpdatePlayerDataAsync(Dictionary<string, string> data)
    {
        if (!InLobby())
            return;

        var playerId = AuthenticationService.Instance.PlayerId;
        var dataCurr = new Dictionary<string, PlayerDataObject>();
        
        foreach (var dataNew in data)
        {
            var dataObj = new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Member,
                value: dataNew.Value);
            if (dataCurr.ContainsKey(dataNew.Key))
                dataCurr[dataNew.Key] = dataObj;
            else
                dataCurr.Add(dataNew.Key, dataObj);
        }

        if (_updatePlayerCooldown.TaskQueued)
            return;
        await _updatePlayerCooldown.QueueUntilCooldown();

        var updateOptions = new UpdatePlayerOptions
        {
            Data = dataCurr,
            AllocationId = null,
            ConnectionInfo = null
        };
        _currentLobby = await LobbyService.Instance.UpdatePlayerAsync(_currentLobby.Id, playerId, updateOptions);
    }
    
    public async Task UpdateLobbyDataAsync(Dictionary<string, string> data)
    {
        if (!InLobby())
            return;

        var dataCurr = _currentLobby.Data ?? new Dictionary<string, DataObject>();

        var shouldLock = false;
        foreach (var dataNew in data)
        {
            // Special case: We want to be able to filter on our color data, so we need to supply an arbitrary index to retrieve later. Uses N# for numerics, instead of S# for strings.
            var index = dataNew.Key == "LocalLobbyColor" ? DataObject.IndexOptions.N1 : 0;
            var dataObj = new DataObject(DataObject.VisibilityOptions.Public, dataNew.Value,
                index); // Public so that when we request the list of lobbies, we can get info about them for filtering.
            if (dataCurr.ContainsKey(dataNew.Key))
                dataCurr[dataNew.Key] = dataObj;
            else
                dataCurr.Add(dataNew.Key, dataObj);

            //Special Use: Get the state of the Local lobby so we can lock it from appearing in queries if it's not in the "Lobby" LocalLobbyState
            if (dataNew.Key == "LocalLobbyState")
            {
                Enum.TryParse(dataNew.Value, out LobbyState lobbyState);
                shouldLock = lobbyState != LobbyState.Lobby;
            }
        }

        //We can still update the latest data to send to the service, but we will not send multiple UpdateLobbySyncCalls
        if (_updateLobbyCooldown.TaskQueued)
            return;
        await _updateLobbyCooldown.QueueUntilCooldown();

        var updateOptions = new UpdateLobbyOptions { Data = dataCurr, IsLocked = shouldLock };
        _currentLobby = await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, updateOptions);
    }

    public void Dispose()
    {
        _currentLobby = null;
        _lobbyEventCallbacks = new LobbyEventCallbacks();
    }
    
    private async Task SendHeartbeatPingAsync()
    {
        if (!InLobby())
        {
            return;
        }

        if (_heartBeatCooldown.IsCoolingDown)
        {
            return;
        }
        
        await _heartBeatCooldown.QueueUntilCooldown();

        await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
    }

    private void StartHeartBeat()
    {
#pragma warning disable 4014
        _heartBeatTask = HeartBeatLoop();
#pragma warning restore 4014
    }
    
    private async Task HeartBeatLoop()
    {
        while (_currentLobby != null)
        {
            await SendHeartbeatPingAsync();
            await Task.Delay(8000);
        }
    }
}

public class ServiceRateLimiter
{
    public Action<bool> OnCooldownChange;
    public readonly int CoolDownMS;
    public bool TaskQueued { get; private set; }

    private readonly int _serviceCallTimes;
    private bool _coolingDown;
    private int _taskCounter;

    //(If you're still getting rate limit errors, try increasing the pingBuffer)
    public ServiceRateLimiter(int callTimes, float coolDown, int pingBuffer = 100)
    {
        _serviceCallTimes = callTimes;
        _taskCounter = _serviceCallTimes;
        CoolDownMS =
            Mathf.CeilToInt(coolDown * 1000) +
            pingBuffer;
    }

    public async Task QueueUntilCooldown()
    {
        if (!_coolingDown)
        {
#pragma warning disable 4014
            ParallelCooldownAsync();
#pragma warning restore 4014
        }

        _taskCounter--;

        if (_taskCounter > 0)
        {
            return;
        }

        if (!TaskQueued)
            TaskQueued = true;
        else
            return;

        while (_coolingDown)
        {
            await Task.Delay(10);
        }
    }

    private async Task ParallelCooldownAsync()
    {
        IsCoolingDown = true;
        await Task.Delay(CoolDownMS);
        IsCoolingDown = false;
        TaskQueued = false;
        _taskCounter = _serviceCallTimes;
    }

    public bool IsCoolingDown
    {
        get => _coolingDown;
        private set
        {
            if (_coolingDown != value)
            {
                _coolingDown = value;
                OnCooldownChange?.Invoke(_coolingDown);
            }
        }
    }
}