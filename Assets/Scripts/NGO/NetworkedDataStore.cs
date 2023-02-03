using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkedDataStore : NetworkBehaviour
{
    public static NetworkedDataStore Instance;

    public string[] PlayerNames => _playerNames;

    private string[] _playerNames;
    private Dictionary<ulong, PlayerData> _playerData = new();
    private RoundState _roundState;
    private ulong _localId;
    private ulong[] _clientIds;

    private Action<ulong[]> _onGetClientIds;
    private Action<PlayerData> _onGetPlayerDataCallback;
    private Action<RoundState> _onGetRoundDataCallback;
    
    public void Awake()
    {
        Instance = this;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
            Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        _localId = NetworkManager.Singleton.LocalClientId;
    }

    public int GetLocalPlayerIndex()
    {
        return GetIndexForClientId(_localId);
    }

    public int GetIndexForClientId(ulong clientId)
    {
        return Array.IndexOf(_clientIds, clientId);
    }

    public void InitGameData()
    {
        if (!IsServer)
        {
            return;
        }
        
        _clientIds = _playerData.Keys.ToArray();
    }

    public void InitPlayerNames()
    {
        Debug.Log("Init Player Names");
        
        // todo 
        _playerNames = new string[GameManager.Instance.LocalLobby.PlayerCount];

        for (var i = 0; i < _playerNames.Length; i++)
        {
            _playerNames[i] = $"Player {i}";
        }
    }
    
    public void AddPlayer(ulong id, string name)
    {
        if (!IsServer)
        {
            return;
        }

        if (!_playerData.ContainsKey(id))
        {
            _playerData.Add(id, new PlayerData(name, id));
        }
        else
        {
            _playerData[id] = new PlayerData(name, id);
        }
    }

    public void SetPlayerHand(int index, CardState[] hand)
    {
        if (!IsServer)
        {
            return;
        }
        
        _playerData[_clientIds[index]].Hand = hand;
    }

    public CardState[] GetPlayerHand(int playerIndex)
    {
        return !IsServer ? Array.Empty<CardState>() : _playerData[_clientIds[playerIndex]].Hand;
    }

    public void RemoveCardFromPlayerHand(int index, CardState card)
    {
        if (!IsServer)
        {
            return;
        }
        
        var hand = _playerData[_clientIds[index]].Hand.ToList();
        hand.Remove(card);
        
        SetPlayerHand(index, hand.ToArray());
    }

    public void SetCurrentRound(RoundState roundState)
    {
        if (!IsServer)
        {
            return;
        }
        
        _roundState = roundState;
    }
    
    private void OnGetClientIds(ulong[] clientIds)
    {
        if (IsServer)
        {
            return;
        }
        
        _clientIds = clientIds;
        InGameRunner.Instance.LocalPlayerIndex = GetLocalPlayerIndex();
    }

    public void GetFullState(Action<PlayerData> onGetPlayerData, Action<RoundState> onGetRoundData)
    {
        _onGetClientIds = OnGetClientIds;
        _onGetPlayerDataCallback = onGetPlayerData;
        _onGetRoundDataCallback = onGetRoundData;
        GetFullState_ServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void GetFullState_ServerRpc(ServerRpcParams serverRpcParams = default)
    {
        SendFullStateToClient(serverRpcParams.Receive.SenderClientId);
    }

    private void SendFullStateToClient(ulong clientId)
    {
        GetFullState_ClientRpc(_clientIds, _playerData[clientId] ?? new PlayerData(), _roundState ?? new RoundState(), 
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    // todo cache target client ids
                    TargetClientIds = new[]{clientId}
                }
            });
    }

    [ClientRpc]
    public void GetFullState_ClientRpc(ulong[] clientIds, PlayerData playerData, RoundState roundData, ClientRpcParams clientRpcParams)
    {
        _onGetClientIds?.Invoke(clientIds);
        _onGetClientIds = null;
        
        _onGetPlayerDataCallback?.Invoke(playerData);
        _onGetPlayerDataCallback = null;

        _onGetRoundDataCallback?.Invoke(roundData);
        _onGetRoundDataCallback = null;
    }
}