using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UNITY_EDITOR
using ParrelSync;
#endif
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[Flags]
public enum GameState
{
    Menu = 1,
    Lobby = 2,
    Game = 4,
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private SetupInGame setupInGame;

    public LocalLobby LocalLobby => _localLobby;
    public Action<GameState> OnGameStateChanged;
    
    public GameState LocalGameState { get; private set; }

    public LobbyManager LobbyManager { get; private set; }
    
    private LocalLobby _localLobby;
    private LocalPlayer _localUser;

    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }

            return _instance = FindObjectOfType<GameManager>();
        }
    }

    private async void Awake()
    {
        Debug.Log("GameManager awaking");
        Application.wantsToQuit += OnWantToQuit;
        _localUser = new LocalPlayer("", 0, false);
        _localLobby = new LocalLobby{LocalLobbyState = {Value = LobbyState.Lobby}};
        LobbyManager = new LobbyManager();
        Debug.Log("Before InitializeServices()");
        await InitializeServices();
        AuthenticatePlayer();
        SetGameState(GameState.Menu);
    }

    private async Task InitializeServices()
    {
        string serviceProfileName = "player";
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            serviceProfileName = $"{serviceProfileName}_{ClonesManager.GetCurrentProject().name}_{ClonesManager.GetArgument()}";
        }
#endif
        await Authentication.Authenticate(serviceProfileName);
    }

    private void AuthenticatePlayer()
    {
        var localId = AuthenticationService.Instance.PlayerId;
        
        // todo
        _localUser.ID.Value = localId;
    }

    private void SetGameState(GameState state)
    {
        var isLeavingLobby = state == GameState.Menu &&
                             LocalGameState is GameState.Lobby or GameState.Game;
        LocalGameState = state;
        
        Debug.Log($"Switching game state to: {LocalGameState}");

        if (isLeavingLobby)
        {
            LeaveLobby();
        }

        OnGameStateChanged?.Invoke(LocalGameState);
    }
    
    public async Task<LocalPlayer> AwaitLocalUserInitialization()
    {
        while (_localUser == null)
            await Task.Delay(100);
        return _localUser;
    }

    public async void CreateLobby(string lobbyName, bool isPrivate, int maxPlayers = 4)
    {
        try
        {
            var lobby = await LobbyManager.CreateLobbyAsync(lobbyName, maxPlayers, isPrivate, _localUser);

            LobbyConverter.RemoteToLocal(lobby, _localLobby);
            await CreateLobby();
        }
        catch (Exception e)
        {
            SetGameState(GameState.Menu);
            Debug.LogError($"Error creating lobby: {e}");
        }
    }

    public async void JoinLobby(string lobbyCode)
    {
        try
        {
            // todo validation on code / error handling
            var lobby = await LobbyManager.JoinLobbyAsync(lobbyCode, _localUser);

            LobbyConverter.RemoteToLocal(lobby, _localLobby);
            await JoinLobby();
        }
        catch (Exception e)
        {
            SetGameState(GameState.Menu);
            Debug.LogError($"Error joining lobby: {e}");
        }
    }

    private async Task CreateLobby()
    {
        _localUser.IsHost.Value = true;

        try
        {
            await BindLobby();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error joining lobby: {e}");
        }
    }

    private async Task JoinLobby()
    {
        _localUser.IsHost.ForceSet(false);
        await BindLobby();
    }

    private async Task BindLobby()
    {
        await LobbyManager.BindLocalLobbyToRemote(_localLobby.LobbyID.Value, _localLobby);
        _localLobby.LocalLobbyState.OnChanged += OnLobbyStateChanged;
        SetLobbyView();
    }
    
    public void LeaveLobby()
    {
        _localUser.ResetState();
#pragma warning disable 4014
        LobbyManager.LeaveLobbyAsync();
#pragma warning restore 4014
        ResetLocalLobby();
    }
    
    public void EndGame()
    {
        if (_localUser.IsHost.Value)
        {
            // todo
            // _localLobby.LocalLobbyState.Value = LobbyState.Lobby;
            // SendLocalLobbyData();
        }

        SetLobbyView();
    }
    
    public void SetLocalUserStatus(PlayerStatus status)
    {
        _localUser.UserStatus.Value = status;
        SendLocalUserData();
    }
    
    private async void SendLocalLobbyData()
    {
        await LobbyManager.UpdateLobbyDataAsync(LobbyConverter.LocalToRemoteLobbyData(_localLobby));
    }
    
    private async void SendLocalUserData()
    {
        await LobbyManager.UpdatePlayerDataAsync(LobbyConverter.LocalToRemoteUserData(_localUser));
    }

    public void HostSetRelayCode(string code)
    {
        _localLobby.RelayCode.Value = code;
        SendLocalLobbyData();
    }

    private void OnLobbyStateChanged(LobbyState state)
    {
        if (state == LobbyState.InGame)
        {
            _localUser.UserStatus.Value = PlayerStatus.InGame;
            setupInGame.StartNetworkedGame(_localLobby, _localUser);
        }
        else if (state == LobbyState.Lobby)
        {
            // todo
        }
    }

    private void SetLobbyView()
    {
        SetGameState(GameState.Lobby);
        SetLocalUserStatus(PlayerStatus.Lobby);
    }
    
    private void ResetLocalLobby()
    {
        _localLobby.ResetLobby();
    }

    private IEnumerator LeaveBeforeQuit()
    {
        ForceLeaveAttempt();
        yield return null;
        Application.Quit();
    }

    private bool OnWantToQuit()
    {
        var canQuit = string.IsNullOrEmpty(_localLobby.LobbyID.Value);
        StartCoroutine(LeaveBeforeQuit());
        return canQuit;
    }

    private void OnDestroy()
    {
        ForceLeaveAttempt();
        LobbyManager.Dispose();
    }

    private void ForceLeaveAttempt()
    {
        if (!string.IsNullOrEmpty(_localLobby?.LobbyID.Value))
        {
#pragma warning disable CS4014
            LobbyManager.LeaveLobbyAsync();
#pragma warning restore CS4014
            _localLobby = null;
        }
    }

    public void StartGame()
    {
        _localLobby.LocalLobbyState.Value = LobbyState.InGame;
        SendLocalLobbyData();
    }

    // todo move
    // void CreateDeck()
    // {
    //     var cardShoeState = new CardShoeState();
    //
    //     for (var i = 0; i < 3; i++)
    //     {
    //         var card = Instantiate(cardImage, new Vector3(0, 0, 0), Quaternion.identity);
    //         card.GetComponent<CardDisplayController>().CardState = cardShoeState.DealCard();
    //         card.GetComponentInChildren<Image>().transform.position += new Vector3(i * 15, 0, 0);
    //     }
    // }
}
