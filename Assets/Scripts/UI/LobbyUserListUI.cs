using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class LobbyUserListUI : UIPanelBase
{
    [SerializeField] private List<InLobbyUserUI> userUIObjects = new();

    private LocalLobby _localLobby;

    public override void Start()
    {
        base.Start();
        _localLobby = Manager.LocalLobby;
        _localLobby.OnUserJoined += OnUserJoined;
        _localLobby.OnUserLeft += OnUserLeft;
    }

    private void OnUserJoined(LocalPlayer localPlayer)
    {
        SyncPlayerUI();
    }

    private void OnUserLeft(int i)
    {
        SyncPlayerUI();
    }

    private void SyncPlayerUI()
    {
        foreach (var ui in userUIObjects)
        {
            ui.ResetUI();
        }

        for (var i = 0; i < _localLobby.PlayerCount; i++)
        {
            var lobbySlot = userUIObjects[i];
            var player = _localLobby.GetLocalPlayer(i);
            
            if (player == null)
            {
                continue;
            }

            lobbySlot.SetUser(player);
        }
    }
}