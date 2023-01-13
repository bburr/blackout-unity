using System;
using UnityEngine.Serialization;

[Flags]
public enum UserPermission
{
    Client = 1,
    Host = 2
}

public class UserStateVisibilityUI : UIPanelBase
{
    public PlayerStatus showThisWhen;
    public UserPermission permissions;
    private bool _hasStatusFlags;
    private bool _hasPermissions;

    public override async void Start()
    {
        base.Start();
        var localUser = await Manager.AwaitLocalUserInitialization();

        localUser.IsHost.OnChanged += OnUserHostChanged;

        localUser.UserStatus.OnChanged += OnUserStatusChanged;
    }

    private void OnUserStatusChanged(PlayerStatus observedStatus)
    {
        _hasStatusFlags = showThisWhen.HasFlag(observedStatus);
        CheckVisibility();
    }

    private void OnUserHostChanged(bool isHost)
    {
        _hasPermissions = false;
        if (permissions.HasFlag(UserPermission.Host) && isHost)
        {
            _hasPermissions = true;
        }

        if (permissions.HasFlag(UserPermission.Client) && !isHost)
        {
            _hasPermissions = true;
        }

        CheckVisibility();
    }

    private void CheckVisibility()
    {
        if (_hasStatusFlags && _hasPermissions)
            Show();
        else
            Hide();
    }
}