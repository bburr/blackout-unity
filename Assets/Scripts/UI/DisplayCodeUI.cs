using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class DisplayCodeUI : UIPanelBase
{
    public enum CodeType { Lobby = 0, Relay = 1 }

    [SerializeField]
    private TMP_InputField _outputText;
    [SerializeField]
    private CodeType _codeType;

    void LobbyCodeChanged(string newCode)
    {
        if (!string.IsNullOrEmpty(newCode))
        {
            _outputText.text = newCode;
            Show();
        }
        else
        {
            Hide();
        }
    }

    public override void Start()
    {
        base.Start();
        if(_codeType==CodeType.Lobby)
            Manager.LocalLobby.LobbyCode.OnChanged += LobbyCodeChanged;
        if(_codeType==CodeType.Relay)
            Manager.LocalLobby.RelayCode.OnChanged += LobbyCodeChanged;
    }
}