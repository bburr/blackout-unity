using UnityEngine;

namespace UI
{
    public class MainMenuUI : UIPanelBase
    {
        private string _lobbyCode;
        
        public void OnCreatePressed()
        {
            Manager.CreateLobby("lobby_name", true, 4);
        }

        public void OnJoinPressed()
        {
            Manager.JoinLobby(_lobbyCode);
        }

        public void SetLobbyCode(string lobbyCode)
        {
            _lobbyCode = lobbyCode;
        }
    }
}