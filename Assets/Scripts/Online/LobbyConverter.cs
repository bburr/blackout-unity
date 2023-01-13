using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyConverter
{
    private const string KeyRelayCode = nameof(LocalLobby.RelayCode);
    private const string KeyLobbyState = nameof(LocalLobby.LocalLobbyState);
    private const string KeyLastEdit = nameof(LocalLobby.LastUpdated);
    private const string KeyUserStatus = nameof(LocalPlayer.UserStatus);
    
    public static Dictionary<string, string> LocalToRemoteLobbyData(LocalLobby lobby)
    {
        var data = new Dictionary<string, string>
        {
            { KeyRelayCode, lobby.RelayCode.Value },
            { KeyLobbyState, ((int)lobby.LocalLobbyState.Value).ToString() },
            { KeyLastEdit, lobby.LastUpdated.Value.ToString() }
        };

        return data;
    }
    
    public static Dictionary<string, string> LocalToRemoteUserData(LocalPlayer user)
    {
        var data = new Dictionary<string, string>();
        if (user == null || string.IsNullOrEmpty(user.ID.Value))
            return data;
        data.Add(KeyUserStatus, ((int)user.UserStatus.Value).ToString());
        return data;
    }
    
    public static void RemoteToLocal(Lobby remoteLobby, LocalLobby localLobby)
    {
        if (remoteLobby == null)
        {
            Debug.LogError("Remote lobby is null");
            return;
        }

        if (localLobby == null)
        {
            Debug.LogError("Local lobby is null");
            return;
        }

        localLobby.LobbyID.Value = remoteLobby.Id;
        localLobby.LobbyCode.Value = remoteLobby.LobbyCode;
        localLobby.MaxPlayerCount.Value = remoteLobby.MaxPlayers;
        localLobby.LastUpdated.Value = remoteLobby.LastUpdated.ToFileTimeUtc();

        localLobby.RelayCode.Value = remoteLobby.Data?.ContainsKey(KeyRelayCode) == true
            ? remoteLobby.Data[KeyRelayCode].Value
            : localLobby.RelayCode.Value;

        localLobby.LocalLobbyState.Value = remoteLobby.Data?.ContainsKey(KeyLobbyState) == true
            ? (LobbyState)int.Parse(remoteLobby.Data[KeyLobbyState].Value)
            : LobbyState.Lobby;

        var index = 0;
        
        foreach (var player in remoteLobby.Players)
        {
            var id = player.Id;
            var isHost = remoteLobby.HostId.Equals(id);
            
            var userStatus = player.Data?.ContainsKey(KeyUserStatus) == true
                ? (PlayerStatus)int.Parse(player.Data[KeyUserStatus].Value)
                : PlayerStatus.Lobby;

            var localPlayer = localLobby.GetLocalPlayer(index);

            if (localPlayer == null)
            {
                localPlayer = new LocalPlayer(id, index, isHost, userStatus);
                localLobby.AddPlayer(index, localPlayer);
            }
            else
            {
                localPlayer.ID.Value = id;
                localPlayer.Index.Value = index;
                localPlayer.IsHost.Value = isHost;
                localPlayer.UserStatus.Value = userStatus;
            }

            index++;
        }
    }
}