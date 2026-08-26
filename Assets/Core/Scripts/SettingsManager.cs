using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum HotKeyAction
{
    MoveUp,
    MoveDown,
    MoveRight,
    MoveLeft,
    Pray,
    Speech
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private static readonly Dictionary<HotKeyAction, Key> DefaultHotKeys = new Dictionary<HotKeyAction, Key>
    {
        { HotKeyAction.MoveUp, Key.W },
        { HotKeyAction.MoveDown, Key.S },
        { HotKeyAction.MoveRight, Key.D },
        { HotKeyAction.MoveLeft, Key.A },
        { HotKeyAction.Pray, Key.F },
        { HotKeyAction.Speech, Key.G },
    };

    private int masterVolume = 100;
    private bool isMasterMuted = false;
    private int bgmVolume = 100;
    private bool isBgmMuted = false;
    private int sfxVolume = 100;
    private bool isSfxMuted = false;
    private readonly Dictionary<HotKeyAction, Key> hotKeys = new Dictionary<HotKeyAction, Key>();

    public int MasterVolume => masterVolume;
    public bool IsMasterMuted => isMasterMuted;
    public int BgmVolume => bgmVolume;
    public bool IsBgmMuted => isBgmMuted;
    public int SfxVolume => sfxVolume;
    public bool IsSfxMuted => isSfxMuted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject settingsManagerObject = new GameObject(nameof(SettingsManager));
        DontDestroyOnLoad(settingsManagerObject);
        settingsManagerObject.AddComponent<SettingsManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetHotKeysToDefault();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
        ApplySettings();
    }

    public void SetMasterMute(bool isMuted)
    {
        isMasterMuted = isMuted;
        ApplySettings();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
        ApplySettings();
    }

    public void SetBgmMute(bool isMuted)
    {
        isBgmMuted = isMuted;
        ApplySettings();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
        ApplySettings();
    }

    public void SetSfxMute(bool isMuted)
    {
        isSfxMuted = isMuted;
        ApplySettings();
    }

    public void ResetHotKeysToDefault()
    {
        hotKeys.Clear();

        foreach (KeyValuePair<HotKeyAction, Key> pair in DefaultHotKeys)
        {
            hotKeys[pair.Key] = pair.Value;
        }
    }

    public Key GetHotKey(HotKeyAction action)
    {
        if (hotKeys.TryGetValue(action, out Key key))
        {
            return key;
        }

        return DefaultHotKeys.TryGetValue(action, out Key defaultKey) ? defaultKey : Key.None;
    }

    public string GetHotKeyLabel(HotKeyAction action)
    {
        return FormatHotKeyLabel(GetHotKey(action));
    }

    public void SetHotKey(HotKeyAction action, Key key)
    {
        Key normalizedKey = NormalizeHotKey(key);

        if (normalizedKey != Key.None)
        {
            ClearDuplicateHotKey(action, normalizedKey);
        }

        hotKeys[action] = normalizedKey;
    }

    public bool TryGetActionUsingHotKey(Key key, HotKeyAction exceptAction, out HotKeyAction usedAction)
    {
        Key normalizedKey = NormalizeHotKey(key);
        if (normalizedKey == Key.None)
        {
            usedAction = default;
            return false;
        }

        foreach (HotKeyAction action in DefaultHotKeys.Keys)
        {
            if (action == exceptAction)
            {
                continue;
            }

            if (GetHotKey(action) == normalizedKey)
            {
                usedAction = action;
                return true;
            }
        }

        usedAction = default;
        return false;
    }

    private void ApplySettings()
    {
        float finalMasterVolume = isMasterMuted ? 0f : masterVolume / 100f;
        AudioListener.volume = finalMasterVolume;

        // BGM / SFX 분리는 나중에 AudioMixer나 AudioSource 분리 후 적용
    }

    private void ClearDuplicateHotKey(HotKeyAction currentAction, Key key)
    {
        foreach (HotKeyAction action in DefaultHotKeys.Keys)
        {
            if (action == currentAction)
            {
                continue;
            }

            if (GetHotKey(action) == key)
            {
                hotKeys[action] = Key.None;
            }
        }
    }

    private static Key NormalizeHotKey(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
        {
            return key;
        }

        return Key.None;
    }

    public static string FormatHotKeyLabel(Key key)
    {
        return key == Key.None ? string.Empty : key.ToString().ToUpper();
    }
}
