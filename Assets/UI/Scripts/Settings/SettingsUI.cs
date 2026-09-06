using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum PopupType
{
    EmptyHotKey,
    NewGame,
    QuitGame,
    HotKeyDuplicateConfirm
}

public class SettingsUI : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string IntroNewspaperSceneName = "IntroNewspaperScene";

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private ScrollRect settingsScrollRect;

    [Header("키보드 내비게이션")]
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Sprite selectionArrowSprite;
    [SerializeField] private Image selectionLeftArrow;
    [SerializeField] private Image selectionRightArrow;

    [Header("씬별 표시 설정")]
    [SerializeField] private GameObject inGameOnlyGroup;
    [SerializeField] private Button newGameButton;

    [Header("음향 설정")]
    [SerializeField] private VolumeSet masterVolume;
    [SerializeField] private VolumeSet bgmVolume;
    [SerializeField] private VolumeSet sfxVolume;

    [Header("단축키 설정")]
    [SerializeField] private Button upKey;
    [SerializeField] private Button downKey;
    [SerializeField] private Button rightKey;
    [SerializeField] private Button leftKey;
    [SerializeField] private Button prayHotKey;
    [SerializeField] private Button speechHotKey;
    [SerializeField] private Button resetHotKeysButton;

    [Header("플레이 방법 설정")]
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button closeHowToPlayButton;
    [SerializeField] private ScrollRect howToPlayScrollRect;

    [Header("메인 화면 이동 설정")]
    [SerializeField] private Button quitGameButton;

    [Header("팝업창 설정")]
    [SerializeField] private GameObject confirmPopup;
    [SerializeField] private Button popupConfirmButton;
    [SerializeField] private Button popupCancelButton;
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private string hotKeyWarningMessage = "비어 있는 단축키가 있습니다.\n 설정창을 닫으시겠습니까?";
    [SerializeField] private string newGameWarningMessage = "새 게임을 시작하면 현재 저장된 진행 상황이 삭제됩니다.\n계속하시겠습니까?";
    [SerializeField] private string quitGameWarningMessage = "메인 화면으로 돌아가시겠습니까?\n현재 콘클라베 진행 상황은 저장되지 않습니다.";
    [SerializeField] private string duplicateHotKeyWarningMessage = "이미 다른 단축키로 사용 중입니다.\n기존 단축키를 해제하고 바꾸시겠습니까?";
    private PopupType currentPopupType;

    private readonly System.Collections.Generic.Dictionary<HotKeyAction, Button> hotKeyButtons =
        new System.Collections.Generic.Dictionary<HotKeyAction, Button>();
    private HotKeyAction pendingDuplicateAction;
    private Key pendingDuplicateKey = Key.None;
    private bool hasPendingDuplicateHotKey = false;
    private float previousTimeScale = 1f;
    private bool isSettingsPausingGame = false;
    private SettingsKeyboardNavigator _keyboardNavigator;
    private int _lastClosedFrame = -1;

    public bool IsOpen => settingsPanel != null && settingsPanel.activeInHierarchy;
    public bool IsInputCaptured => IsOpen || Time.frameCount == _lastClosedFrame;

    //private UIManager.UIState prevState;

    private void Awake()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        CloseHowToPlayPanel();
        CacheHotKeyButtons();
        RegisterEvents();
        InitializeKeyboardNavigation();
        SyncHotKeyButtonsFromManager();
        CloseConfirmPopup();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshUI();
        RefreshSceneDependentUI();

        //prevState = UIManager.Instance.State;
        //UIManager.Instance.SetUIState(UIManager.UIState.SETTINGS);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _keyboardNavigator?.Deactivate();
        ResumeGameFromSettings();
        //UIManager.Instance.SetUIState(prevState);
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }

    private void Update()
    {
        _keyboardNavigator?.Tick();
        if (_keyboardNavigator != null && _keyboardNavigator.DidConsumeEscapeThisFrame)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (IsHowToPlayPanelOpen())
            {
                CloseHowToPlayPanel();
                return;
            }

            ToggleSettingsPanel();
            return;
        }

        /*
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isCredit) CloseCredits();
            else if (isHelp) CloseHelp();
        }
        */
    }

    // =========================================================
    // 설정창 열기 / 닫기
    // =========================================================

    public void ToggleSettingsPanel()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("Settings Panel이 연결되지 않았습니다.");
            return;
        }

        if (settingsPanel.activeSelf)
        {
            if (IsHowToPlayPanelOpen())
            {
                CloseHowToPlayPanel();
                return;
            }

            TryCloseSettingsPanel();
        }
        else
        {
            RefreshSceneDependentUI();
            settingsPanel.SetActive(true);
            PauseGameForSettings();
            CloseHowToPlayPanel();
            CloseConfirmPopup();
            ResetScrollToTop();
            _keyboardNavigator?.Activate();
        }
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("Settings Panel이 연결되지 않았습니다.");
            return;
        }

        RefreshSceneDependentUI();
        settingsPanel.SetActive(true);
        PauseGameForSettings();
        CloseHowToPlayPanel();
        CloseConfirmPopup();
        ResetScrollToTop();
        _keyboardNavigator?.Activate();
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("Settings Panel이 연결되지 않았습니다.");
            return;
        }

        TryCloseSettingsPanel();
    }

    private void RegisterEvents()
    {
        CacheSettingsScrollRect();

        if (masterVolume != null)
        {
            masterVolume.VolumeSlider.onValueChanged.AddListener(OnChangeMasterVolume);
            masterVolume.MuteToggle.onValueChanged.AddListener(OnChangeMasterMute);
        }

        if (bgmVolume != null)
        {
            bgmVolume.VolumeSlider.onValueChanged.AddListener(OnChangeBgmVolume);
            bgmVolume.MuteToggle.onValueChanged.AddListener(OnChangeBgmMute);
        }

        if (sfxVolume != null)
        {
            sfxVolume.VolumeSlider.onValueChanged.AddListener(OnChangeSfxVolume);
            sfxVolume.MuteToggle.onValueChanged.AddListener(OnChangeSfxMute);
        }

        if (upKey != null)
        {
            upKey.onClick.AddListener(OnClickUpKey);
        }

        if (downKey != null)
        {
            downKey.onClick.AddListener(OnClickDownKey);
        }

        if (rightKey != null)
        {
            rightKey.onClick.AddListener(OnClickRightKey);
        }

        if (leftKey != null)
        {
            leftKey.onClick.AddListener(OnClickLeftKey);
        }

        if (prayHotKey != null)
        {
            prayHotKey.onClick.AddListener(OnClickPrayHotKey);
        }

        if (speechHotKey != null)
        {
            speechHotKey.onClick.AddListener(OnClickSpeechHotKey);
        }

        if (resetHotKeysButton != null)
        {
            resetHotKeysButton.onClick.AddListener(OnClickResetHotKeys);
        }

        if (howToPlayButton != null)
        {
            howToPlayButton.onClick.AddListener(OnClickHowToPlay);
        }

        if (closeHowToPlayButton != null)
        {
            closeHowToPlayButton.onClick.AddListener(OnClickCloseHowToPlay);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(OnClickQuitGame);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneDependentUI();
    }

    public void RefreshSceneDependentUI()
    {
        if (inGameOnlyGroup == null)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        bool isGameScene = activeScene.name == GameSceneName || activeScene.path.EndsWith($"/{GameSceneName}.unity");
        inGameOnlyGroup.SetActive(isGameScene);
    }

    private void UnregisterEvents()
    {
        if (masterVolume != null)
        {
            masterVolume.VolumeSlider.onValueChanged.RemoveListener(OnChangeMasterVolume);
            masterVolume.MuteToggle.onValueChanged.RemoveListener(OnChangeMasterMute);
        }

        if (bgmVolume != null)
        {
            bgmVolume.VolumeSlider.onValueChanged.RemoveListener(OnChangeBgmVolume);
            bgmVolume.MuteToggle.onValueChanged.RemoveListener(OnChangeBgmMute);
        }

        if (sfxVolume != null)
        {
            sfxVolume.VolumeSlider.onValueChanged.RemoveListener(OnChangeSfxVolume);
            sfxVolume.MuteToggle.onValueChanged.RemoveListener(OnChangeSfxMute);
        }

        if (upKey != null)
        {
            upKey.onClick.RemoveListener(OnClickUpKey);
        }

        if (downKey != null)
        {
            downKey.onClick.RemoveListener(OnClickDownKey);
        }

        if (rightKey != null)
        {
            rightKey.onClick.RemoveListener(OnClickRightKey);
        }

        if (leftKey != null)
        {
            leftKey.onClick.RemoveListener(OnClickLeftKey);
        }

        if (prayHotKey != null)
        {
            prayHotKey.onClick.RemoveListener(OnClickPrayHotKey);
        }

        if (speechHotKey != null)
        {
            speechHotKey.onClick.RemoveListener(OnClickSpeechHotKey);
        }

        if (resetHotKeysButton != null)
        {
            resetHotKeysButton.onClick.RemoveListener(OnClickResetHotKeys);
        }

        if (howToPlayButton != null)
        {
            howToPlayButton.onClick.RemoveListener(OnClickHowToPlay);
        }

        if (closeHowToPlayButton != null)
        {
            closeHowToPlayButton.onClick.RemoveListener(OnClickCloseHowToPlay);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.RemoveListener(OnClickQuitGame);
        }
    }

    public void RefreshUI()
    {
        SettingsManager sm = SettingsManager.Instance;
        if (sm == null) return;

        masterVolume.SetValue(sm.MasterVolume);
        masterVolume.SetMute(sm.IsMasterMuted);

        bgmVolume.SetValue(sm.BgmVolume);
        bgmVolume.SetMute(sm.IsBgmMuted);

        sfxVolume.SetValue(sm.SfxVolume);
        sfxVolume.SetMute(sm.IsSfxMuted);

        RefreshDependentVolumeVisuals();
        SyncHotKeyButtonsFromManager();
    }

    // =========================================================
    // 스크롤 초기화
    // =========================================================

    private void CacheSettingsScrollRect()
    {
        if (settingsScrollRect == null && settingsPanel != null)
        {
            settingsScrollRect = settingsPanel.GetComponentInChildren<ScrollRect>(true);
        }
    }

    private void ResetScrollToTop()
    {
        if (settingsScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        settingsScrollRect.StopMovement();
        settingsScrollRect.verticalNormalizedPosition = 1f;
        settingsScrollRect.horizontalNormalizedPosition = 0f;
    }

    // =========================================================
    // 볼륨 설정
    // =========================================================

    private void InitializeKeyboardNavigation()
    {
        SettingsKeyboardNavigationBindings bindings = new SettingsKeyboardNavigationBindings
        {
            SettingsPanel = settingsPanel,
            ConfirmPopup = confirmPopup,
            PopupConfirmButton = popupConfirmButton,
            PopupCancelButton = popupCancelButton,
            HowToPlayPanel = howToPlayPanel,
            SettingsScrollRect = settingsScrollRect,
            SelectionArrowSprite = selectionArrowSprite,
            SelectionLeftArrow = selectionLeftArrow,
            SelectionRightArrow = selectionRightArrow,
            CloseSettingsButton = closeSettingsButton,
            MasterVolume = masterVolume,
            BgmVolume = bgmVolume,
            SfxVolume = sfxVolume,
            UpKey = upKey,
            LeftKey = leftKey,
            DownKey = downKey,
            RightKey = rightKey,
            PrayKey = prayHotKey,
            SpeechKey = speechHotKey,
            ResetHotKeysButton = resetHotKeysButton,
            NewGameButton = newGameButton,
            QuitGameButton = quitGameButton,
            HowToPlayButton = howToPlayButton,
            CloseHowToPlayButton = closeHowToPlayButton
        };

        _keyboardNavigator = new SettingsKeyboardNavigator(this, bindings);
    }

    private void OnChangeMasterVolume(float value)
    {
        masterVolume.RefreshText();
        SettingsManager.Instance.SetMasterVolume(value);
    }

    private void OnChangeMasterMute(bool isMuted)
    {
        masterVolume.ApplyMutedVisual(isMuted);
        RefreshDependentVolumeVisuals();
        SettingsManager.Instance.SetMasterMute(isMuted);
    }

    private void OnChangeBgmVolume(float value)
    {
        bgmVolume.RefreshText();
        SettingsManager.Instance.SetBgmVolume(value);
    }

    private void OnChangeBgmMute(bool isMuted)
    {
        RefreshDependentVolumeVisuals();
        SettingsManager.Instance.SetBgmMute(isMuted);
    }

    private void OnChangeSfxVolume(float value)
    {
        sfxVolume.RefreshText();
        SettingsManager.Instance.SetSfxVolume(value);
    }

    private void OnChangeSfxMute(bool isMuted)
    {
        RefreshDependentVolumeVisuals();
        SettingsManager.Instance.SetSfxMute(isMuted);
    }

    private void RefreshDependentVolumeVisuals()
    {
        bool isMasterMuted = masterVolume != null && masterVolume.IsMuted();

        if (bgmVolume != null)
        {
            bgmVolume.ApplyMutedVisual(isMasterMuted || bgmVolume.IsMuted());
        }

        if (sfxVolume != null)
        {
            sfxVolume.ApplyMutedVisual(isMasterMuted || sfxVolume.IsMuted());
        }

    }

    // =========================================================
    // 단축키 설정
    // =========================================================

    private void OnClickUpKey()
    {
        StartWaitingHotKeyInput(HotKeyAction.MoveUp);
    }

    private void OnClickDownKey()
    {
        StartWaitingHotKeyInput(HotKeyAction.MoveDown);
    }

    private void OnClickRightKey()
    {
        StartWaitingHotKeyInput(HotKeyAction.MoveRight);
    }

    private void OnClickLeftKey()
    {
        StartWaitingHotKeyInput(HotKeyAction.MoveLeft);
    }

    private void OnClickPrayHotKey()
    {
        StartWaitingHotKeyInput(HotKeyAction.Pray);
    }

    private void OnClickSpeechHotKey()
    {
        StartWaitingHotKeyInput(HotKeyAction.Speech);
    }

    public void OnClickResetHotKeys()
    {
        _keyboardNavigator?.CancelCurrentInteraction();
        ClearPendingDuplicateHotKey();

        if (SettingsManager.Instance == null)
        {
            return;
        }

        SettingsManager.Instance.ResetHotKeysToDefault();
        SyncHotKeyButtonsFromManager();
        CloseConfirmPopup();
    }

    private void StartWaitingHotKeyInput(HotKeyAction action)
    {
        _keyboardNavigator?.BeginHotKeyRebind(action);
    }

    internal void PrepareHotKeyRebind()
    {
        ClearPendingDuplicateHotKey();
        CloseConfirmPopup();
    }

    internal void CommitHotKeyChange(HotKeyAction action, Key pressedKey)
    {
        if (pressedKey < Key.A || pressedKey > Key.Z)
        {
            SyncHotKeyButtonsFromManager();
            return;
        }

        SettingsManager sm = SettingsManager.Instance;
        if (sm == null)
        {
            SyncHotKeyButtonsFromManager();
            return;
        }

        if (sm.TryGetActionUsingHotKey(pressedKey, action, out _))
        {
            pendingDuplicateAction = action;
            pendingDuplicateKey = pressedKey;
            hasPendingDuplicateHotKey = true;
            SyncHotKeyButtonsFromManager();
            ShowConfirmPopup(PopupType.HotKeyDuplicateConfirm);
            return;
        }

        UpdateManagerHotKey(action, pressedKey);
        SyncHotKeyButtonsFromManager();

        if (!HasEmptyHotKeys())
        {
            CloseConfirmPopup();
        }
    }

    private void CacheHotKeyButtons()
    {
        hotKeyButtons.Clear();
        hotKeyButtons[HotKeyAction.MoveUp] = upKey;
        hotKeyButtons[HotKeyAction.MoveDown] = downKey;
        hotKeyButtons[HotKeyAction.MoveRight] = rightKey;
        hotKeyButtons[HotKeyAction.MoveLeft] = leftKey;
        hotKeyButtons[HotKeyAction.Pray] = prayHotKey;
        hotKeyButtons[HotKeyAction.Speech] = speechHotKey;
    }

    internal void SyncHotKeyButtonsFromManager()
    {
        SettingsManager sm = SettingsManager.Instance;
        if (sm == null)
        {
            return;
        }

        foreach (System.Collections.Generic.KeyValuePair<HotKeyAction, Button> pair in hotKeyButtons)
        {
            SetHotKeyButtonText(pair.Key, sm.GetHotKey(pair.Key), null);
        }
    }

    internal void SetHotKeyButtonPreview(HotKeyAction action, Key key, float candidateAlpha)
    {
        string colorTag = null;
        if (candidateAlpha > 0f)
        {
            Color candidateColor = new Color(1f, 0.82f, 0f, Mathf.Clamp01(candidateAlpha));
            colorTag = ColorUtility.ToHtmlStringRGBA(candidateColor);
        }

        SetHotKeyButtonText(action, key, colorTag);
    }

    private void SetHotKeyButtonText(HotKeyAction action, Key key, string colorTag)
    {
        if (!hotKeyButtons.TryGetValue(action, out Button button))
        {
            return;
        }

        string keyLabel = SettingsManager.FormatHotKeyLabel(key);
        if (!string.IsNullOrEmpty(colorTag))
        {
            keyLabel = $"<color=#{colorTag}>{keyLabel}</color>";
        }

        TMP_Text targetText = GetButtonText(button);
        if (targetText == null)
        {
            return;
        }

        targetText.richText = true;
        targetText.color = Color.white;
        targetText.text = keyLabel;
    }

    private void UpdateManagerHotKey(HotKeyAction action, Key key)
    {
        SettingsManager sm = SettingsManager.Instance;

        if (sm == null)
        {
            return;
        }

        sm.SetHotKey(action, key);
    }

    private void ApplyPendingDuplicateHotKey()
    {
        if (!hasPendingDuplicateHotKey)
        {
            CloseConfirmPopup();
            return;
        }

        UpdateManagerHotKey(pendingDuplicateAction, pendingDuplicateKey);
        ClearPendingDuplicateHotKey();
        SyncHotKeyButtonsFromManager();
        CloseConfirmPopup();
    }

    private void ClearPendingDuplicateHotKey()
    {
        pendingDuplicateAction = default;
        pendingDuplicateKey = Key.None;
        hasPendingDuplicateHotKey = false;
    }

    private bool HasEmptyHotKeys()
    {
        SettingsManager sm = SettingsManager.Instance;
        if (sm == null)
        {
            return false;
        }

        foreach (HotKeyAction action in hotKeyButtons.Keys)
        {
            if (sm.GetHotKey(action) == Key.None)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // 새 게임 시작 설정
    // =========================================================

    public void OnClickNewGame()
    {
        ShowConfirmPopup(PopupType.NewGame);
    }

    // =========================================================
    // 메인 화면 이동 설정
    // =========================================================

    public void OnClickQuitGame()
    {
        ShowConfirmPopup(PopupType.QuitGame);
    }

    // =========================================================
    // 플레이 방법 설정
    // =========================================================

    public void OnClickHowToPlay()
    {
        OpenHowToPlayPanel();
    }

    public void OnClickCloseHowToPlay()
    {
        CloseHowToPlayPanel();
    }

    private void OpenHowToPlayPanel()
    {
        _keyboardNavigator?.CancelCurrentInteraction();

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(true);
        }

        ResetHowToPlayScrollToTop();
    }

    private void CloseHowToPlayPanel()
    {
        ResetHowToPlayScrollToTop();

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }
    }

    private bool IsHowToPlayPanelOpen()
    {
        return howToPlayPanel != null && howToPlayPanel.activeSelf;
    }

    private void ResetHowToPlayScrollToTop()
    {
        if (howToPlayScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        howToPlayScrollRect.StopMovement();
        howToPlayScrollRect.verticalNormalizedPosition = 1f;
        howToPlayScrollRect.horizontalNormalizedPosition = 0f;
    }

    private bool TryCloseSettingsPanel()
    {
        if (HasEmptyHotKeys())
        {
            ShowConfirmPopup(PopupType.EmptyHotKey);
            return false;
        }

        CloseConfirmPopup();
        CloseHowToPlayPanel();
        _keyboardNavigator?.Deactivate();
        HideSettingsPanel();
        ResumeGameFromSettings();
        return true;
    }

    private void ForceCloseSettingsPanel()
    {
        CloseConfirmPopup();
        CloseHowToPlayPanel();
        _keyboardNavigator?.Deactivate();

        if (settingsPanel != null)
        {
            HideSettingsPanel();
        }

        ResumeGameFromSettings();
    }

    private void HideSettingsPanel()
    {
        if (settingsPanel == null)
        {
            return;
        }

        bool wasOpen = settingsPanel.activeInHierarchy;
        settingsPanel.SetActive(false);
        if (wasOpen)
        {
            _lastClosedFrame = Time.frameCount;
        }
    }

    private void PauseGameForSettings()
    {
        if (isSettingsPausingGame)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isSettingsPausingGame = true;
    }

    private void ResumeGameFromSettings()
    {
        if (!isSettingsPausingGame)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        isSettingsPausingGame = false;
    }

    private void ShowConfirmPopup(PopupType popupType)
    {
        _keyboardNavigator?.CancelCurrentInteraction();
        currentPopupType = popupType;

        if (popupText != null)
        {
            switch (popupType)
            {
                case PopupType.EmptyHotKey:
                    popupText.text = hotKeyWarningMessage;
                    break;
                case PopupType.NewGame:
                    popupText.text = newGameWarningMessage;
                    break;
                case PopupType.QuitGame:
                    popupText.text = quitGameWarningMessage;
                    break;
                case PopupType.HotKeyDuplicateConfirm:
                    popupText.text = duplicateHotKeyWarningMessage;
                    break;
            }
        }

        if (confirmButtonText != null)
        {
            switch (popupType)
            {
                case PopupType.EmptyHotKey:
                    confirmButtonText.text = "확인";
                    break;

                case PopupType.NewGame:
                    confirmButtonText.text = "새 게임 시작";
                    break;

                case PopupType.QuitGame:
                    confirmButtonText.text = "메인 화면";
                    break;

                case PopupType.HotKeyDuplicateConfirm:
                    confirmButtonText.text = "바꾸기";
                    break;
            }
        }

        if (confirmPopup != null)
        {
            confirmPopup.SetActive(true);
        }
    }

    private void CloseConfirmPopup()
    {
        if (popupText != null)
        {
            popupText.text = string.Empty;
        }

        if (confirmButtonText != null)
        {
            confirmButtonText.text = "확인";
        }

        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false);
        }
    }

    public void OnClickPopupConfirm()
    {
        switch (currentPopupType)
        {
            case PopupType.EmptyHotKey:
                ForceCloseSettingsPanel();
                break;
            case PopupType.NewGame:
                ForceCloseSettingsPanel();
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.DiscardCurrentGameSave();
                }
                Time.timeScale = 1f;
                SceneManager.LoadScene(IntroNewspaperSceneName);
                break;
            case PopupType.QuitGame:
                ForceCloseSettingsPanel();
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.GoToMainMenu();
                }
                break;
            case PopupType.HotKeyDuplicateConfirm:
                ApplyPendingDuplicateHotKey();
                break;
            default:
                CloseConfirmPopup();
                break;
        }
    }

    public void OnClickPopupCancel()
    {
        if (currentPopupType == PopupType.HotKeyDuplicateConfirm)
        {
            ClearPendingDuplicateHotKey();
            SyncHotKeyButtonsFromManager();
        }

        CloseConfirmPopup();
    }

    private static TMP_Text GetButtonText(Button button)
    {
        if (button == null)
        {
            return null;
        }

        return button.GetComponentInChildren<TMP_Text>(true);
    }

    private static void SetButtonText(Button button, string label)
    {
        TMP_Text targetText = GetButtonText(button);

        if (targetText != null)
        {
            targetText.text = label;
        }
    }

    bool isCredit = false;
    bool isHelp = false;
    void OpenCredits()
    {
        isCredit = true;
    }
    void CloseCredits()
    {
        isCredit = false;
    }
    void OpenHelp()
    {
        isHelp = true;
    }
    void CloseHelp()
    {
        isHelp = false;
    }

    // 어떤 방식으로 credits와 help를 열지는 나중에 결정
}
