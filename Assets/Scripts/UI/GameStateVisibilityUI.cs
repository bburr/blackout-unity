using UnityEngine;
using UnityEngine.Serialization;

public class GameStateVisibilityUI : UIPanelBase
{
    [SerializeField] 
    private GameState showThisWhen;

    void GameStateChanged(GameState state)
    {
        if (!showThisWhen.HasFlag(state))
            Hide();
        else
            Show();
    }

    public override void Start()
    {
        base.Start();
        GameStateChanged(Manager.LocalGameState); // init with current state
        Manager.OnGameStateChanged += GameStateChanged;
    }

    void OnDestroy()
    {
        if (Manager == null)
            return;
        Manager.OnGameStateChanged -= GameStateChanged;
    }
}