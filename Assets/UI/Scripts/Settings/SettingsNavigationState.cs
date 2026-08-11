using UnityEngine.InputSystem;

public enum VolumeControlTarget
{
    Slider,
    Mute
}

public enum SettingsNavigationTarget
{
    CloseSettings,
    MasterSlider,
    MasterMute,
    BgmSlider,
    BgmMute,
    SfxSlider,
    SfxMute,
    MoveUp,
    MoveLeft,
    MoveDown,
    MoveRight,
    ResetHotKeys,
    NewGame,
    QuitGame,
    HowToPlay,
    CloseHowToPlay
}

public enum SettingsNavigationMode
{
    Inactive,
    Navigation,
    VolumeEditing,
    HotKeyRebinding
}

public sealed class SettingsNavigationState
{
    public SettingsNavigationTarget SelectedTarget { get; private set; }
    public SettingsNavigationMode Mode { get; private set; } = SettingsNavigationMode.Inactive;
    public HotKeyAction RebindingAction { get; private set; }
    public Key CandidateKey { get; private set; } = Key.None;

    public bool IsActive => Mode != SettingsNavigationMode.Inactive;
    public bool IsRebinding => Mode == SettingsNavigationMode.HotKeyRebinding;

    public void Activate(SettingsNavigationTarget initialTarget)
    {
        SelectedTarget = initialTarget;
        Mode = SettingsNavigationMode.Navigation;
        CandidateKey = Key.None;
    }

    public void Deactivate()
    {
        Mode = SettingsNavigationMode.Inactive;
        CandidateKey = Key.None;
    }

    public bool Select(SettingsNavigationTarget target)
    {
        if (Mode != SettingsNavigationMode.Navigation || target == SelectedTarget)
        {
            return false;
        }

        SelectedTarget = target;
        return true;
    }

    public bool BeginVolumeEditing(bool canEdit)
    {
        if (Mode != SettingsNavigationMode.Navigation || !canEdit)
        {
            return false;
        }

        Mode = SettingsNavigationMode.VolumeEditing;
        return true;
    }

    public void BeginHotKeyRebinding(SettingsNavigationTarget target, HotKeyAction action)
    {
        SelectedTarget = target;
        RebindingAction = action;
        CandidateKey = Key.None;
        Mode = SettingsNavigationMode.HotKeyRebinding;
    }

    public void SetCandidateKey(Key key)
    {
        if (Mode == SettingsNavigationMode.HotKeyRebinding)
        {
            CandidateKey = key;
        }
    }

    public void ReturnToNavigation()
    {
        if (Mode == SettingsNavigationMode.Inactive)
        {
            return;
        }

        CandidateKey = Key.None;
        Mode = SettingsNavigationMode.Navigation;
    }
}
