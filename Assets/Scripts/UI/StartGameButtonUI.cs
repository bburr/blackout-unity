using UnityEngine;

public class StartGameButtonUI : UserStateVisibilityUI
{
    public void OnStartGamePressed()
    {
        Manager.StartGame();
    }
}