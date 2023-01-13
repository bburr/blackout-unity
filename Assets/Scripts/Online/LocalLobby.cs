using System;
using System.Collections.Generic;

[Flags]
public enum LobbyState
{
    Lobby,
    InGame
}

public class LocalLobby
{
    public Action<LocalPlayer> OnUserJoined;
    public Action<int> OnUserLeft;
    
    public CallbackValue<string> LobbyID = new();
    public CallbackValue<string> LobbyCode = new();
    public CallbackValue<string> RelayCode = new();
    public CallbackValue<string> HostID = new();
    public CallbackValue<LobbyState> LocalLobbyState = new();
    public CallbackValue<int> MaxPlayerCount = new();
    public CallbackValue<long> LastUpdated = new();

    public int PlayerCount => _localPlayers.Count;
    public List<LocalPlayer> LocalPlayers => _localPlayers;

    private List<LocalPlayer> _localPlayers = new();
    
    public LocalLobby()
    {
        LastUpdated.Value = DateTime.Now.ToFileTimeUtc();
    }

    public LocalPlayer GetLocalPlayer(int index)
    {
        return PlayerCount > index ? _localPlayers[index] : null;
    }

    public void AddPlayer(int index, LocalPlayer user)
    {
        _localPlayers.Insert(index, user);
        OnUserJoined?.Invoke(user);
    }

    public void RemovePlayer(int playerIndex)
    {
        _localPlayers.RemoveAt(playerIndex);
        OnUserLeft?.Invoke(playerIndex);
    }
    
    public void ResetLobby()
    {
        _localPlayers.Clear();

        LobbyID.Value = "";
        LobbyCode.Value = "";
        MaxPlayerCount.Value = 4;
        OnUserJoined = null;
        OnUserLeft = null;
    }
}