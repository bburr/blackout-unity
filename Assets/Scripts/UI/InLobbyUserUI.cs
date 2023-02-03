using TMPro;
using UnityEngine;

public class InLobbyUserUI : UIPanelBase
{
    [SerializeField] private TMP_Text displayNameText;
    
    private LocalPlayer _localPlayer;
    
    public string UserId { get; set; }
    
    public void SetUser(LocalPlayer localPlayer)
    {
        Debug.Log($"Set User: {localPlayer.ID.Value}");
        Show();
        _localPlayer = localPlayer;
        UserId = $"Player {localPlayer.Index.Value}";
        displayNameText.SetText(UserId);
    }

    public void ResetUI()
    {
        if (_localPlayer == null)
        {
            return;
        }

        UserId = null;
        Hide();
        _localPlayer = null;
    }
}