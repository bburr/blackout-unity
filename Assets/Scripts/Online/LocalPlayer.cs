public enum PlayerStatus
{
    None = 0,
    Connecting = 1,
    Lobby = 2,
    InGame = 4,
    Menu = 16,
}

public class LocalPlayer
{
    // todo do we need PlayerState?
    
    public CallbackValue<bool> IsHost = new(false);
    public CallbackValue<string> ID = new("");
    public CallbackValue<int> Index = new(0);
    public CallbackValue<PlayerStatus> UserStatus = new(PlayerStatus.None);
    
    public LocalPlayer(string id, int index, bool isHost, PlayerStatus status = default)
    {
        ID.Value = id;
        IsHost.Value = isHost;
        Index.Value = index;
        UserStatus.Value = status;
    }
    
    public void ResetState()
    {
        IsHost.Value = false;
        UserStatus.Value = PlayerStatus.Menu;
    }
}