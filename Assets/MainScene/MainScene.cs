using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainScene : MonoBehaviour
{
    private const string PopeListNavigationButtonName = "PopeList";
    private const float PopeListNavigationIdleAlpha = 143f / 255f;

    private const string IntroNewspaperSceneName = "IntroNewspaperScene";

    [SerializeField] private GameObject startGameWarningPopup;
    [SerializeField] private GameObject loadWarningPopup;
    [SerializeField] private GameObject loadPopup;
    [SerializeField] private GameObject selectNamePopup;
    [SerializeField] private GameObject dictPopup;
    [SerializeField] private Button dictButton;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Button startNameButton;
    [SerializeField] private Component loadUserNameText;
    [SerializeField] private Component loadPlayerHpText;
    [SerializeField] private Component loadPlayerInfluenceText;
    [SerializeField] private Component loadPlayerPietyText;
    [SerializeField] private Component loadDayText;
    [SerializeField] private Component loadConclaveText;
    [SerializeField] private GameObject popeListPopup;
    [SerializeField] private Image popeListCreditImage;
    [SerializeField] private GameObject popeListCreditObject;
    [SerializeField] private List<Sprite> popeListCreditSprites = new();
    [SerializeField] private List<Button> popeListFrameButtons = new();
    [SerializeField] private Button popeListLeftArrowButton;
    [SerializeField] private Button popeListRightArrowButton;
    [SerializeField] private Button popeListBackButton;
    [SerializeField] private List<Selectable> popeListBlockedSelectables = new();
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera subCamera;
    [SerializeField] private float popeListCameraTransitionDuration = 0.6f;
    [SerializeField] private List<Graphic> popeListSpotlightGraphics = new();
    [SerializeField] private Color popeListFocusedTintColor = new Color32(0xB3, 0xB3, 0xB3, 0xFF);
    [SerializeField] private float popeListSpotlightFlickerAlphaStep = 10f / 255f;
    [SerializeField] private float popeListSpotlightFocusedAlphaStep = 40f / 255f;
    [SerializeField] private List<Button> navigationButtons = new();
    [SerializeField] private Color selectedOutlineColor = Color.black;
    [SerializeField] private Vector2 selectedOutlineDistance = new Vector2(8f, -8f);
    [SerializeField] private bool selectedOutlineUseGraphicAlpha = false;

    private int currentNavigationIndex = -1;
    private bool cachedSendNavigationEvents;
    private bool hasCachedSendNavigationEvents;
    private Button highlightedButton;
    private Outline highlightedOutline;
    private readonly Dictionary<Button, Outline> buttonOutlines = new();
    private readonly Dictionary<Button, Image> navigationButtonImages = new();
    private readonly Dictionary<Image, Color> navigationButtonImageColors = new();
    private readonly Dictionary<Button, Vector2Int> navigationButtonCoordinates = new();
    private readonly HashSet<Button> navigationPointerBoundButtons = new();
    private static readonly string[] navigationImageButtonNames =
    {
        "GameStartBtn",
        "Books",
        "Setting",
        "LoadBtn",
        "ResetData",
        "Dict",
        "PopeList",
    };
    private static readonly Dictionary<string, Vector2Int> navigationCoordinatesByButtonName = new()
    {
        { "LoadBtn", new Vector2Int(0, 0) },
        { "Setting", new Vector2Int(-2, 0) },
        { "GameStartBtn", new Vector2Int(-1, 1) },
        { "ResetData", new Vector2Int(1, 0) },
        { "Dict", new Vector2Int(2, 0) },
        { "PopeList", new Vector2Int(2, 1) },
    };
    private readonly Dictionary<Selectable, bool> popeListSelectableInteractableStates = new();
    private readonly List<Sprite> resolvedPopeListCreditSprites = new();
    private int currentPopeCreditIndex;
    private bool popeListRuntimeBindingsInitialized;
    private bool popeListFrameButtonListenersRegistered;
    private bool popeListArrowButtonListenersRegistered;
    private bool popeListHistoryPresenterInitialized;
    private PopeListHistoryPresenter popeListHistoryPresenter;
    private bool isViewingSubCamera;
    private bool hasCameraInitialStates;
    private CameraState mainCameraInitialState;
    private CameraState subCameraInitialState;
    private Coroutine cameraTransitionCoroutine;
    private RectTransform popeListPopupRect;
    private RectTransform popeListCanvasRect;
    private Vector2 popeListPopupInitialAnchoredPosition;
    private Vector3 popeListPopupInitialScale;
    private bool hasPopeListPopupInitialTransform;
    private Coroutine popeListZoomTransitionCoroutine;
    private readonly List<Graphic> popeListTintGraphics = new();
    private readonly Dictionary<Graphic, Color> popeListInitialGraphicColors = new();
    private bool hasPopeListInitialGraphicColors;

    private void Awake()
    {
        ResolveNameSelectionReferences();
        ResolveDictionaryPopupReferences();
        InitializePopeListRuntimeBindings();
        SetNavigationButtonImagesVisible(null);
        SetStartGameWarningPopup(false);
        SetLoadWarningPopup(false);
        SetLoadPopup(false);
        SetSelectNamePopup(false);
        SetDictPopup(false);
        SetPopeListPopup(false);
    }

    private void Start()
    {
        InitializePopeListRuntimeBindings();
        EnsureEventSystemNavigationDisabled();
        RefreshNavigation(true);
    }

    private void Update()
    {
        EnsureEventSystemNavigationDisabled();
        RefreshNavigation(false);
        if (IsSettingsInputCaptured())
        {
            return;
        }

        HandlePopeListPopupInput();
        HandleNavigationInput();
    }

    private void OnDisable()
    {
        ClearSelectionHighlight();
        ApplyPopeListMouseOnlyMode(false);
        popeListHistoryPresenter?.ExitBrowseMode();
        SwitchToMainCameraImmediate();
        RestoreEventSystemNavigation();
    }

    public void OnClickStartGame()
    {
        SetStartGameWarningPopup(true);
    }

    public void OnClickConfirmStartGame()
    {
        if (IsPopupOpen(dictPopup))
        {
            SetDictPopup(false);
            return;
        }

        SetStartGameWarningPopup(false);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DiscardCurrentGameSave();
        }

        LoadIntroNewspaperScene();
    }

    public void OnClickOpenDictPopup()
    {
        SetDictPopup(true);
    }

    public void OnClickCancelStartGame()
    {
        SetStartGameWarningPopup(false);
    }

    private void LoadIntroNewspaperScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(IntroNewspaperSceneName);
    }

    public void OnClickStartNamedGame()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        string playerName = GetPlayerInputName();
        SetSelectNamePopup(false);
        SaveManager.Instance.StartNewGame(playerName);
    }

    public void OnClickLoad()
    {
        if (SaveManager.Instance == null)
        {
            return;
        }

        SetLoadWarningPopup(false);
        SetLoadPopup(false);

        if (!SaveManager.Instance.HasSave())
        {
            SetLoadWarningPopup(true);
            return;
        }

        if (SaveManager.Instance.TryGetSavePreview(out SavePreviewData preview))
        {
            ApplyLoadPreview(preview);
            SetLoadPopup(true);
            return;
        }

        SetLoadWarningPopup(true);
    }

    public void OnClickPlayLoadGame()
    {
        SetLoadPopup(false);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }
    }

    public void OnClickCloseLoadPopup()
    {
        SetLoadPopup(false);
    }

    public void OnClickCloseLoadWarningPopup()
    {
        SetLoadWarningPopup(false);
    }

    public void OnClickOpenPopeListPopup()
    {
        SetPopeListPopup(true);
    }

    public void OnClickClosePopeListPopup()
    {
        SetPopeListPopup(false);
    }

    public void OnClickPopeListLeftArrow()
    {
        SoundManager.Instance.PlaySFX("ButtonLight");
        popeListHistoryPresenter?.MoveLeft();
        SyncPopeListNavigationSprite();
    }

    public void OnClickPopeListRightArrow()
    {
        SoundManager.Instance.PlaySFX("ButtonLight");
        popeListHistoryPresenter?.MoveRight();
        SyncPopeListNavigationSprite();
    }

    public void OnClickPopeListFrame()
    {
        SoundManager.Instance.PlaySFX("Frame");
        InitializePopeListRuntimeBindings();
        popeListHistoryPresenter?.EnterBrowseMode();
        if (popeListHistoryPresenter == null || !popeListHistoryPresenter.IsBrowsing)
        {
            return;
        }

        TransitionToSubCamera();
    }

    private void SetStartGameWarningPopup(bool isActive)
    {
        if (startGameWarningPopup != null)
        {
            startGameWarningPopup.SetActive(isActive);
        }

        RefreshNavigation(false);
    }

    private void SetLoadWarningPopup(bool isActive)
    {
        if (loadWarningPopup != null)
        {
            loadWarningPopup.SetActive(isActive);
        }

        RefreshNavigation(false);
    }

    private void SetLoadPopup(bool isActive)
    {
        if (loadPopup != null)
        {
            loadPopup.SetActive(isActive);
        }

        RefreshNavigation(false);
    }

    private void SetPopeListPopup(bool isActive)
    {
        if (isActive)
        {
            InitializePopeListRuntimeBindings();
            popeListHistoryPresenter?.ResetToLatest();
            SyncPopeListNavigationSprite();
            popeListHistoryPresenter?.ExitBrowseMode();
            ApplyPopeListMouseOnlyMode(true);
        }
        else
        {
            popeListHistoryPresenter?.ExitBrowseMode();
            ApplyPopeListMouseOnlyMode(false);
            SwitchToMainCameraImmediate();
        }

        if (popeListPopup != null)
        {
            popeListPopup.SetActive(isActive);
        }

        if (popeListCreditObject != null)
        {
            popeListCreditObject.SetActive(isActive);
        }

        if (isActive)
        {
            ConfigurePopeListRaycasts();
        }

        RefreshNavigation(false);
    }

    private void ApplyLoadPreview(SavePreviewData preview)
    {
        if (preview == null)
        {
            return;
        }

        SetText(loadUserNameText, preview.playerName);
        SetText(loadPlayerHpText, FormatStatValue(preview.playerHp));
        SetText(loadPlayerInfluenceText, FormatStatValue(preview.playerInfluence));
        SetText(loadPlayerPietyText, FormatStatValue(preview.playerPiety));
        SetText(loadDayText, preview.day.ToString());
        SetText(loadConclaveText, preview.conclaveName);
    }

    private string FormatStatValue(float value)
    {
        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private void SetText(Component textComponent, string value)
    {
        Component resolvedComponent = ResolveTextComponent(textComponent);
        if (resolvedComponent == null)
        {
            return;
        }

        if (resolvedComponent is TMP_Text tmpText)
        {
            tmpText.text = value;
            return;
        }

        if (resolvedComponent is Text legacyText)
        {
            legacyText.text = value;
        }
    }

    private Component ResolveTextComponent(Component sourceComponent)
    {
        if (sourceComponent == null)
        {
            return null;
        }

        if (sourceComponent is TMP_Text || sourceComponent is Text)
        {
            return sourceComponent;
        }

        TMP_Text tmpText = sourceComponent.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            return tmpText;
        }

        Text legacyText = sourceComponent.GetComponent<Text>();
        if (legacyText != null)
        {
            return legacyText;
        }

        return sourceComponent.GetComponentInChildren<TMP_Text>(true) as Component
            ?? sourceComponent.GetComponentInChildren<Text>(true);
    }

    private void HandleNavigationInput()
    {
        if (IsNavigationBlocked())
        {
            return;
        }

        if (WasMoveLeftPressed())
        {
            MoveSelection(Vector2Int.left);
        }
        else if (WasMoveRightPressed())
        {
            MoveSelection(Vector2Int.right);
        }
        else if (WasMoveUpPressed())
        {
            MoveSelection(Vector2Int.up);
        }
        else if (WasMoveDownPressed())
        {
            MoveSelection(Vector2Int.down);
        }

        if (WasSubmitPressed())
        {
            ActivateCurrentButton();
        }
    }

    private void HandlePopeListPopupInput()
    {
        if (isViewingSubCamera && WasCancelPressed())
        {
            popeListHistoryPresenter?.ExitBrowseMode();
            TransitionToMainCamera();
            return;
        }

        if (!IsPopupOpen(popeListPopup))
        {
            return;
        }

        if (popeListHistoryPresenter == null || !popeListHistoryPresenter.IsBrowsing)
        {
            return;
        }

        if (WasMoveLeftPressed())
        {
            popeListHistoryPresenter.MoveLeft();
        }
        else if (WasMoveRightPressed())
        {
            popeListHistoryPresenter.MoveRight();
        }
    }

    private void MoveSelection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero || navigationButtons == null || navigationButtons.Count == 0)
        {
            return;
        }

        if (!IsNavigationIndexEligible(currentNavigationIndex))
        {
            RefreshNavigation(true);
            return;
        }

        Button currentButton = GetCurrentNavigationButton();
        if (currentButton == null || !navigationButtonCoordinates.TryGetValue(currentButton, out Vector2Int currentCoordinate))
        {
            RefreshNavigation(true);
            return;
        }

        int bestIndex = -1;
        int bestPrimaryDistance = int.MaxValue;
        int bestSecondaryDistance = int.MaxValue;

        for (int i = 0; i < navigationButtons.Count; i++)
        {
            if (i == currentNavigationIndex || !IsNavigationIndexEligible(i))
            {
                continue;
            }

            Button candidateButton = navigationButtons[i];
            if (candidateButton == null || !navigationButtonCoordinates.TryGetValue(candidateButton, out Vector2Int candidateCoordinate))
            {
                continue;
            }

            Vector2Int delta = candidateCoordinate - currentCoordinate;
            if (!TryGetDirectionalDistance(delta, direction, out int primaryDistance, out int secondaryDistance))
            {
                continue;
            }

            if (primaryDistance < bestPrimaryDistance
                || (primaryDistance == bestPrimaryDistance && secondaryDistance < bestSecondaryDistance))
            {
                bestIndex = i;
                bestPrimaryDistance = primaryDistance;
                bestSecondaryDistance = secondaryDistance;
            }
        }

        if (bestIndex >= 0)
        {
            currentNavigationIndex = bestIndex;
            UpdateSelectionVisual();
        }
    }

    private void ActivateCurrentButton()
    {
        Button currentButton = GetCurrentNavigationButton();
        if (currentButton == null)
        {
            return;
        }

        currentButton.onClick.Invoke();
        RefreshNavigation(false);
    }

    private void RefreshNavigation(bool forceSelectFirstButton)
    {
        ResolveNavigationButtonImageReferences();
        RegisterNavigationPointerBindings();

        if (IsNavigationBlocked())
        {
            ClearSelectionHighlight();
            return;
        }

        int firstEligibleIndex = FindDefaultNavigationIndex();
        if (firstEligibleIndex < 0)
        {
            firstEligibleIndex = FindFirstEligibleNavigationIndex();
        }

        if (firstEligibleIndex < 0)
        {
            currentNavigationIndex = -1;
            ClearSelectionHighlight();
            return;
        }

        if (forceSelectFirstButton || !IsNavigationIndexEligible(currentNavigationIndex))
        {
            currentNavigationIndex = firstEligibleIndex;
        }

        UpdateSelectionVisual();
    }

    private int FindFirstEligibleNavigationIndex()
    {
        if (navigationButtons == null)
        {
            return -1;
        }

        for (int i = 0; i < navigationButtons.Count; i++)
        {
            if (IsNavigationIndexEligible(i))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindDefaultNavigationIndex()
    {
        if (navigationButtons == null)
        {
            return -1;
        }

        for (int i = 0; i < navigationButtons.Count; i++)
        {
            Button button = navigationButtons[i];
            if (button != null && button.name == "LoadBtn" && IsNavigationIndexEligible(i))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsNavigationIndexEligible(int index)
    {
        if (navigationButtons == null || index < 0 || index >= navigationButtons.Count)
        {
            return false;
        }

        Button button = navigationButtons[index];
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
        {
            return false;
        }

        return true;
    }

    private Button GetCurrentNavigationButton()
    {
        if (!IsNavigationIndexEligible(currentNavigationIndex))
        {
            return null;
        }

        return navigationButtons[currentNavigationIndex];
    }

    private void SelectNavigationButton(Button button)
    {
        if (button == null || navigationButtons == null)
        {
            return;
        }

        int index = navigationButtons.IndexOf(button);
        if (!IsNavigationIndexEligible(index))
        {
            return;
        }

        currentNavigationIndex = index;
        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        Button currentButton = GetCurrentNavigationButton();
        if (currentButton == null)
        {
            ClearSelectionHighlight();
            return;
        }

        Outline targetOutline = GetOrCreateOutline(currentButton);
        if (targetOutline == null)
        {
            ClearSelectionHighlight();
            return;
        }

        if (highlightedButton == currentButton && highlightedOutline == targetOutline)
        {
            if (!targetOutline.enabled)
            {
                targetOutline.enabled = true;
            }

            ApplyOutlineStyle(targetOutline);
            UpdateNavigationButtonImageVisibility(currentButton);
            return;
        }

        ClearSelectionHighlight();
        ApplyOutlineStyle(targetOutline);
        targetOutline.enabled = true;
        highlightedButton = currentButton;
        highlightedOutline = targetOutline;
        UpdateNavigationButtonImageVisibility(currentButton);
    }

    private void ClearSelectionHighlight()
    {
        if (highlightedOutline != null)
        {
            highlightedOutline.enabled = false;
        }

        highlightedButton = null;
        highlightedOutline = null;
        SetCachedNavigationButtonImagesVisible(null);
    }

    private void UpdateNavigationButtonImageVisibility(Button currentButton)
    {
        SetNavigationButtonImagesVisible(currentButton);
    }

    private void SetNavigationButtonImagesVisible(Button visibleButton)
    {
        ResolveNavigationButtonImageReferences();
        SetCachedNavigationButtonImagesVisible(visibleButton);
    }

    private void SetCachedNavigationButtonImagesVisible(Button visibleButton)
    {
        foreach (KeyValuePair<Button, Image> entry in navigationButtonImages)
        {
            if (entry.Value != null)
            {
                SetNavigationButtonImageAlpha(
                    entry.Key,
                    entry.Value,
                    entry.Key != null && entry.Key == visibleButton);
            }
        }
    }

    private void SetNavigationButtonImageAlpha(Button button, Image image, bool isVisible)
    {
        if (image == null)
        {
            return;
        }

        image.enabled = true;
        if (!navigationButtonImageColors.TryGetValue(image, out Color baseColor))
        {
            baseColor = image.color;
            navigationButtonImageColors[image] = baseColor;
        }

        Color color = baseColor;
        if (button != null && button.name == PopeListNavigationButtonName)
        {
            color.a = isVisible ? 1f : PopeListNavigationIdleAlpha;
        }
        else
        {
            color.a = isVisible ? (baseColor.a > 0f ? baseColor.a : 1f) : 0f;
        }

        image.color = color;
    }

    private void ResolveNavigationButtonImageReferences()
    {
        GameObject canvasObject = FindSceneObjectByNameIncludingInactive("Canvas");
        if (canvasObject == null)
        {
            return;
        }

        Transform mainScreenTransform = canvasObject.transform.Find("MainScreen");
        if (mainScreenTransform == null)
        {
            return;
        }

        if (navigationButtons != null)
        {
            foreach (Button button in navigationButtons)
            {
                CacheNavigationButtonReference(button, false);
            }
        }

        foreach (string buttonName in navigationImageButtonNames)
        {
            Transform buttonTransform = mainScreenTransform.Find(buttonName);
            if (buttonTransform == null)
            {
                continue;
            }

            Button button = buttonTransform.GetComponent<Button>();
            CacheNavigationButtonReference(button, true);
        }
    }

    private void CacheNavigationButtonReference(Button button, bool addToNavigationButtons)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            navigationButtonImages[button] = image;
            if (!navigationButtonImageColors.ContainsKey(image))
            {
                navigationButtonImageColors[image] = image.color;
            }
        }

        if (navigationCoordinatesByButtonName.TryGetValue(button.name, out Vector2Int coordinate))
        {
            navigationButtonCoordinates[button] = coordinate;
            if (addToNavigationButtons && navigationButtons != null && !navigationButtons.Contains(button))
            {
                navigationButtons.Add(button);
            }
        }
    }

    private void RegisterNavigationPointerBindings()
    {
        if (navigationButtons == null)
        {
            return;
        }

        foreach (Button button in navigationButtons)
        {
            if (button == null || navigationPointerBoundButtons.Contains(button))
            {
                continue;
            }

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            if (trigger.triggers == null)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }

            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown,
            };
            pointerDownEntry.callback.AddListener(_ => SelectNavigationButton(button));
            trigger.triggers.Add(pointerDownEntry);
            navigationPointerBoundButtons.Add(button);
        }
    }

    private static bool TryGetDirectionalDistance(
        Vector2Int delta,
        Vector2Int direction,
        out int primaryDistance,
        out int secondaryDistance)
    {
        primaryDistance = int.MaxValue;
        secondaryDistance = int.MaxValue;

        if (direction.x != 0)
        {
            int signedDistance = delta.x * direction.x;
            if (signedDistance <= 0)
            {
                return false;
            }

            primaryDistance = Mathf.Abs(delta.x);
            secondaryDistance = Mathf.Abs(delta.y);
            return true;
        }

        if (direction.y != 0)
        {
            int signedDistance = delta.y * direction.y;
            if (signedDistance <= 0)
            {
                return false;
            }

            primaryDistance = Mathf.Abs(delta.y);
            secondaryDistance = Mathf.Abs(delta.x);
            return true;
        }

        return false;
    }

    private bool IsNavigationBlocked()
    {
        return IsSettingsInputCaptured()
            || IsPopupOpen(startGameWarningPopup)
            || IsPopupOpen(loadWarningPopup)
            || IsPopupOpen(loadPopup)
            || IsPopupOpen(selectNamePopup)
            || IsPopupOpen(dictPopup)
            || IsPopupOpen(popeListPopup);
    }

    private static bool IsSettingsInputCaptured()
    {
        return SettingsService.Instance != null && SettingsService.Instance.IsInputCaptured;
    }

    private static bool IsPopupOpen(GameObject popup)
    {
        return popup != null && popup.activeInHierarchy;
    }

    private void ResolveNameSelectionReferences()
    {
        if (selectNamePopup == null)
        {
            selectNamePopup = FindSceneObjectByNameIncludingInactive("SelectNamePopup");
        }

        if (loadPopup == null)
        {
            loadPopup = FindSceneObjectByNameIncludingInactive("loadPopup");
        }

        if (playerNameInputField == null && selectNamePopup != null)
        {
            playerNameInputField = selectNamePopup.GetComponentInChildren<TMP_InputField>(true);
        }

        if (startNameButton == null && selectNamePopup != null)
        {
            Transform startButtonTransform = FindDeepChild(selectNamePopup.transform, "StagtBtn");
            startNameButton = startButtonTransform != null ? startButtonTransform.GetComponent<Button>() : null;
        }

        if (loadUserNameText == null && loadPopup != null)
        {
            Transform userNameTransform = FindDeepChild(loadPopup.transform, "UserName");
            loadUserNameText = userNameTransform != null ? ResolveTextComponent(userNameTransform) : null;
        }

        if (playerNameInputField != null)
        {
            playerNameInputField.characterLimit = 10;
        }

        if (startNameButton != null)
        {
            startNameButton.onClick.RemoveListener(OnClickStartNamedGame);
            startNameButton.onClick.AddListener(OnClickStartNamedGame);
        }
    }

    private void ResolveDictionaryPopupReferences()
    {
        if (dictPopup == null)
        {
            dictPopup = FindSceneObjectByNameIncludingInactive("DictPopUP");
        }

        if (dictButton == null)
        {
            GameObject dictObject = FindSceneObjectByNameIncludingInactive("Dict");
            dictButton = dictObject != null ? dictObject.GetComponent<Button>() : null;
        }

        if (dictButton != null)
        {
            dictButton.onClick.AddListener(OnClickOpenDictPopup);
        }
    }

    private void SetDictPopup(bool isActive)
    {
        if (dictPopup != null)
        {
            dictPopup.SetActive(isActive);
        }
    }

    private static GameObject FindSceneObjectByNameIncludingInactive(string objectName)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
        {
            return activeObject;
        }

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject candidate in objects)
        {
            if (candidate.name == objectName && candidate.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private void OpenNameSelectionPopup()
    {
        ResolveNameSelectionReferences();
        SetSelectNamePopup(true);

        if (playerNameInputField != null)
        {
            playerNameInputField.text = string.Empty;
            playerNameInputField.characterLimit = 10;
            playerNameInputField.Select();
            playerNameInputField.ActivateInputField();
        }
    }

    private void SetSelectNamePopup(bool isActive)
    {
        if (selectNamePopup != null)
        {
            selectNamePopup.SetActive(isActive);
        }

        RefreshNavigation(false);
    }

    private string GetPlayerInputName()
    {
        string inputName = playerNameInputField != null ? playerNameInputField.text : string.Empty;
        inputName = string.IsNullOrWhiteSpace(inputName) ? "Player" : inputName.Trim();
        return inputName.Length > 10 ? inputName.Substring(0, 10) : inputName;
    }

    private Outline GetOrCreateOutline(Button button)
    {
        if (button == null)
        {
            return null;
        }

        if (buttonOutlines.TryGetValue(button, out Outline cachedOutline) && cachedOutline != null)
        {
            return cachedOutline;
        }

        Graphic targetGraphic = GetButtonGraphic(button);
        if (targetGraphic == null)
        {
            return null;
        }

        Outline outline = targetGraphic.GetComponent<Outline>();
        if (outline == null)
        {
            outline = targetGraphic.gameObject.AddComponent<Outline>();
        }

        ApplyOutlineStyle(outline);
        outline.enabled = false;
        buttonOutlines[button] = outline;
        return outline;
    }

    private void ApplyOutlineStyle(Outline outline)
    {
        if (outline == null)
        {
            return;
        }

        outline.effectColor = selectedOutlineColor;
        outline.effectDistance = selectedOutlineDistance;
        outline.useGraphicAlpha = selectedOutlineUseGraphicAlpha;
    }

    private static Graphic GetButtonGraphic(Button button)
    {
        if (button == null)
        {
            return null;
        }

        if (button.targetGraphic != null)
        {
            return button.targetGraphic;
        }

        return button.GetComponent<Graphic>();
    }

    private void InitializePopeListRuntimeBindings()
    {
        ResolvePopeListReferences();
        RegisterPopeListFrameButtonListeners();
        RegisterPopeListArrowButtonListeners();
        ResolvePopeListHistoryPresenter();
        CacheCameraInitialStates();
        CachePopeListPopupTransform();
        ResolvePopeListEffectGraphics();
        CachePopeListInitialGraphicColors();
        ConfigurePopeListRaycasts();

        if (!popeListRuntimeBindingsInitialized)
        {
            SetCameraView(false);
            popeListRuntimeBindingsInitialized = true;
        }

        if (!popeListHistoryPresenterInitialized && popeListHistoryPresenter != null)
        {
            popeListHistoryPresenter.Initialize(
                popeListFrameButtons,
                popeListLeftArrowButton,
                popeListRightArrowButton);
            SyncPopeListNavigationSprite();
            popeListHistoryPresenterInitialized = true;
        }
    }

    private void ResolvePopeListReferences()
    {
        if (popeListPopup == null)
        {
            popeListPopup = GameObject.Find("PopeListPopUp");
        }

        if (popeListCreditObject == null && popeListPopup != null)
        {
            Transform creditTransform = FindDeepChild(popeListPopup.transform, "Credit");
            if (creditTransform != null)
            {
                popeListCreditObject = creditTransform.gameObject;
            }
        }

        if (popeListCreditImage == null && popeListCreditObject != null)
        {
            popeListCreditImage = popeListCreditObject.GetComponent<Image>();
        }

        if (popeListCreditImage != null)
        {
            popeListCreditImage.raycastTarget = false;
            popeListCreditImage.transform.SetAsFirstSibling();
        }

        if (popeListPopup != null)
        {
            if (popeListFrameButtons == null)
            {
                popeListFrameButtons = new List<Button>();
            }

            while (popeListFrameButtons.Count < 5)
            {
                popeListFrameButtons.Add(null);
            }

            for (int i = 1; i <= 5; i++)
            {
                if (popeListFrameButtons[i - 1] != null)
                {
                    continue;
                }

                Transform frameTransform = FindDeepChild(popeListPopup.transform, $"Frame{i}");
                Button frameButton = frameTransform != null ? frameTransform.GetComponent<Button>() : null;
                popeListFrameButtons[i - 1] = frameButton;
            }

        }

        if (popeListLeftArrowButton == null && popeListPopup != null)
        {
            Transform leftTransform = FindDeepChild(popeListPopup.transform, "Left");
            popeListLeftArrowButton = leftTransform != null ? leftTransform.GetComponent<Button>() : null;
        }

        if (popeListRightArrowButton == null && popeListPopup != null)
        {
            Transform rightTransform = FindDeepChild(popeListPopup.transform, "Right");
            popeListRightArrowButton = rightTransform != null ? rightTransform.GetComponent<Button>() : null;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main != null ? Camera.main : FindCameraByName("Main Camera");
        }

        if (subCamera == null)
        {
            subCamera = FindCameraByName("SubCamera");
        }
    }

    private void ResolvePopeListEffectGraphics()
    {
        if (popeListPopup == null)
        {
            return;
        }

        popeListTintGraphics.Clear();
        AddUniqueGraphic(popeListTintGraphics, popeListPopup.GetComponent<Graphic>());

        if (popeListFrameButtons != null)
        {
            foreach (Button frameButton in popeListFrameButtons)
            {
                AddUniqueGraphic(popeListTintGraphics, GetButtonGraphic(frameButton));
            }
        }

        if (popeListSpotlightGraphics == null)
        {
            popeListSpotlightGraphics = new List<Graphic>();
        }

        foreach (Graphic graphic in popeListPopup.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic == null)
            {
                continue;
            }

            string normalizedName = graphic.gameObject.name.Trim();
            if (normalizedName == "PopeListSpotlight_L" || normalizedName == "PopeListSpotlight_R")
            {
                AddUniqueGraphic(popeListSpotlightGraphics, graphic);
                graphic.raycastTarget = false;
                graphic.transform.SetAsLastSibling();
            }
        }
    }

    private void ConfigurePopeListRaycasts()
    {
        if (popeListPopup == null)
        {
            return;
        }

        foreach (Graphic graphic in popeListPopup.GetComponentsInChildren<Graphic>(true))
        {
            Button ownerButton = graphic.GetComponentInParent<Button>();
            graphic.raycastTarget = ownerButton != null && ownerButton.targetGraphic == graphic;
        }
    }

    private static void AddUniqueGraphic(List<Graphic> targets, Graphic graphic)
    {
        if (targets == null || graphic == null || targets.Contains(graphic))
        {
            return;
        }

        targets.Add(graphic);
    }

    private void RefreshPopeListCreditSpriteSources()
    {
        resolvedPopeListCreditSprites.Clear();
        AddSprites(resolvedPopeListCreditSprites, popeListCreditSprites);

        if (resolvedPopeListCreditSprites.Count == 0 && popeListFrameButtons != null)
        {
            foreach (Button frameButton in popeListFrameButtons)
            {
                Image frameImage = frameButton != null ? frameButton.GetComponent<Image>() : null;
                AddSprite(resolvedPopeListCreditSprites, frameImage != null ? frameImage.sprite : null);
            }
        }

        if (resolvedPopeListCreditSprites.Count == 0 && popeListCreditImage != null)
        {
            AddSprite(resolvedPopeListCreditSprites, popeListCreditImage.sprite);
        }
    }

    private void RegisterPopeListFrameButtonListeners()
    {
        if (popeListFrameButtonListenersRegistered || popeListFrameButtons == null)
        {
            return;
        }

        foreach (Button frameButton in popeListFrameButtons)
        {
            if (frameButton != null)
            {
                if (frameButton.onClick.GetPersistentEventCount() == 0)
                {
                    frameButton.onClick.AddListener(OnClickPopeListFrame);
                }
            }
        }

        popeListFrameButtonListenersRegistered = true;
    }

    private void RegisterPopeListArrowButtonListeners()
    {
        if (popeListArrowButtonListenersRegistered)
        {
            return;
        }

        if (popeListLeftArrowButton != null &&
            popeListLeftArrowButton.onClick.GetPersistentEventCount() == 0)
        {
            popeListLeftArrowButton.onClick.AddListener(OnClickPopeListLeftArrow);
        }

        if (popeListRightArrowButton != null &&
            popeListRightArrowButton.onClick.GetPersistentEventCount() == 0)
        {
            popeListRightArrowButton.onClick.AddListener(OnClickPopeListRightArrow);
        }

        popeListArrowButtonListenersRegistered = true;
    }

    private void ResolvePopeListHistoryPresenter()
    {
        if (popeListHistoryPresenter != null || popeListPopup == null)
        {
            return;
        }

        popeListHistoryPresenter = popeListPopup.GetComponent<PopeListHistoryPresenter>();
        if (popeListHistoryPresenter == null)
        {
            popeListHistoryPresenter = popeListPopup.AddComponent<PopeListHistoryPresenter>();
        }
    }

    private void SyncPopeListNavigationSprite()
    {
        ResolveNavigationButtonImageReferences();
        Sprite centerPortrait = popeListHistoryPresenter != null
            ? popeListHistoryPresenter.CurrentCenterPortrait
            : null;

        foreach (KeyValuePair<Button, Image> entry in navigationButtonImages)
        {
            if (entry.Key != null &&
                entry.Value != null &&
                entry.Key.name == PopeListNavigationButtonName)
            {
                entry.Value.sprite = centerPortrait;
                entry.Value.enabled = true;
            }
        }
    }

    private void MovePopeCredit(int direction)
    {
        if (direction == 0 || !IsPopupOpen(popeListPopup))
        {
            return;
        }

        InitializePopeListRuntimeBindings();
        if (resolvedPopeListCreditSprites.Count == 0)
        {
            return;
        }

        currentPopeCreditIndex = WrapIndex(
            currentPopeCreditIndex + direction,
            resolvedPopeListCreditSprites.Count);
        RefreshPopeCreditSprite();
    }

    private void RefreshPopeCreditSprite()
    {
        InitializePopeListRuntimeBindings();
        if (popeListCreditImage == null || resolvedPopeListCreditSprites.Count == 0)
        {
            UpdatePopeListArrowState();
            return;
        }

        currentPopeCreditIndex = WrapIndex(currentPopeCreditIndex, resolvedPopeListCreditSprites.Count);
        popeListCreditImage.sprite = resolvedPopeListCreditSprites[currentPopeCreditIndex];
        popeListCreditImage.enabled = popeListCreditImage.sprite != null;
        UpdatePopeListArrowState();
    }

    private void UpdatePopeListArrowState()
    {
        bool canSwitchCredit = resolvedPopeListCreditSprites.Count > 1;

        if (popeListLeftArrowButton != null)
        {
            popeListLeftArrowButton.interactable = canSwitchCredit;
        }

        if (popeListRightArrowButton != null)
        {
            popeListRightArrowButton.interactable = canSwitchCredit;
        }

        if (popeListBackButton != null)
        {
            popeListBackButton.interactable = true;
        }
    }

    private void ApplyPopeListMouseOnlyMode(bool isPopupOpen)
    {
        if (isPopupOpen)
        {
            popeListSelectableInteractableStates.Clear();

            foreach (Selectable selectable in GetPopeListBlockedSelectables())
            {
                if (selectable == null)
                {
                    continue;
                }

                if (!popeListSelectableInteractableStates.ContainsKey(selectable))
                {
                    popeListSelectableInteractableStates[selectable] = selectable.interactable;
                }

                selectable.interactable = false;
            }

            return;
        }

        foreach (KeyValuePair<Selectable, bool> entry in popeListSelectableInteractableStates)
        {
            if (entry.Key != null)
            {
                entry.Key.interactable = entry.Value;
            }
        }

        popeListSelectableInteractableStates.Clear();
    }

    private List<Selectable> GetPopeListBlockedSelectables()
    {
        List<Selectable> results = new();

        AddUniqueSelectable(results, popeListBlockedSelectables);

        if (navigationButtons != null)
        {
            foreach (Button navigationButton in navigationButtons)
            {
                AddUniqueSelectable(results, navigationButton);
            }
        }

        results.Remove(popeListLeftArrowButton);
        results.Remove(popeListRightArrowButton);
        results.Remove(popeListBackButton);
        if (popeListFrameButtons != null)
        {
            foreach (Button frameButton in popeListFrameButtons)
            {
                results.Remove(frameButton);
            }
        }

        return results;
    }

    private static void AddUniqueSelectable(List<Selectable> targets, IEnumerable<Selectable> source)
    {
        if (targets == null || source == null)
        {
            return;
        }

        foreach (Selectable selectable in source)
        {
            AddUniqueSelectable(targets, selectable);
        }
    }

    private static void AddUniqueSelectable(List<Selectable> targets, Selectable selectable, bool shouldAdd = true)
    {
        if (!shouldAdd || targets == null || selectable == null || targets.Contains(selectable))
        {
            return;
        }

        targets.Add(selectable);
    }

    private static void AddUniqueButton(List<Button> targets, Button button)
    {
        if (targets == null || button == null || targets.Contains(button))
        {
            return;
        }

        targets.Add(button);
    }

    private static void AddSprites(List<Sprite> targets, IEnumerable<Sprite> source)
    {
        if (targets == null || source == null)
        {
            return;
        }

        foreach (Sprite sprite in source)
        {
            AddSprite(targets, sprite);
        }
    }

    private static void AddSprite(List<Sprite> targets, Sprite sprite)
    {
        if (targets == null || sprite == null)
        {
            return;
        }

        targets.Add(sprite);
    }

    private void TransitionToSubCamera()
    {
        BeginPopeListViewTransition(true);
    }

    private void TransitionToMainCamera()
    {
        BeginPopeListViewTransition(false);
    }

    private void BeginPopeListViewTransition(bool useSubCamera)
    {
        InitializePopeListRuntimeBindings();
        isViewingSubCamera = useSubCamera;
        BeginPopeListZoomTransition(useSubCamera);
        BeginCameraTransition(useSubCamera);
    }

    private void BeginCameraTransition(bool useSubCamera)
    {
        InitializePopeListRuntimeBindings();
        if (mainCamera == null || subCamera == null || mainCamera == subCamera)
        {
            return;
        }

        if (cameraTransitionCoroutine != null)
        {
            StopCoroutine(cameraTransitionCoroutine);
        }

        cameraTransitionCoroutine = StartCoroutine(SmoothCameraTransition(useSubCamera));
    }

    private void BeginPopeListZoomTransition(bool useSubCamera)
    {
        CachePopeListPopupTransform();
        if (popeListPopupRect == null)
        {
            return;
        }

        if (popeListZoomTransitionCoroutine != null)
        {
            StopCoroutine(popeListZoomTransitionCoroutine);
        }

        popeListZoomTransitionCoroutine = StartCoroutine(SmoothPopeListZoomTransition(useSubCamera));
    }

    private IEnumerator SmoothCameraTransition(bool useSubCamera)
    {
        Camera sourceCamera = GetCurrentlyEnabledCamera();
        Camera targetCamera = useSubCamera ? subCamera : mainCamera;
        if (sourceCamera == null)
        {
            sourceCamera = useSubCamera ? mainCamera : subCamera;
        }

        if (sourceCamera == null || targetCamera == null)
        {
            cameraTransitionCoroutine = null;
            yield break;
        }

        CameraState startState = CameraState.From(sourceCamera);
        CameraState targetState = CameraState.From(targetCamera);

        sourceCamera.enabled = true;
        if (targetCamera != sourceCamera)
        {
            targetCamera.enabled = false;
        }

        SetCameraAudio(sourceCamera, true);
        if (targetCamera != sourceCamera)
        {
            SetCameraAudio(targetCamera, false);
        }

        float duration = Mathf.Max(0f, popeListCameraTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float easedT = t * t * (3f - 2f * t);
            CameraState.Lerp(startState, targetState, easedT).ApplyTo(sourceCamera);
            yield return null;
        }

        targetState.ApplyTo(targetCamera);
        targetCamera.enabled = true;
        SetCameraAudio(targetCamera, true);

        Camera inactiveCamera = useSubCamera ? mainCamera : subCamera;
        if (inactiveCamera != null && inactiveCamera != targetCamera)
        {
            startState.ApplyTo(inactiveCamera);
            inactiveCamera.enabled = false;
            SetCameraAudio(inactiveCamera, false);
        }

        isViewingSubCamera = useSubCamera;
        cameraTransitionCoroutine = null;
    }

    private IEnumerator SmoothPopeListZoomTransition(bool useSubCamera)
    {
        ResolvePopeListEffectGraphics();
        CachePopeListInitialGraphicColors();
        Vector2 startPosition = popeListPopupRect.anchoredPosition;
        Vector3 startScale = popeListPopupRect.localScale;
        Vector2 targetPosition = popeListPopupInitialAnchoredPosition;
        Vector3 targetScale = popeListPopupInitialScale;

        if (useSubCamera)
        {
            GetPopeListSubCameraTarget(out targetPosition, out targetScale);
        }

        yield return FlickerPopeListSpotlights();

        float duration = Mathf.Max(0f, popeListCameraTransitionDuration);
        float elapsed = 0f;
        Dictionary<Graphic, Color> startColors = GetPopeListCurrentGraphicColors();
        Dictionary<Graphic, Color> targetColors = GetPopeListTargetGraphicColors(useSubCamera, true);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float easedT = t * t * (3f - 2f * t);
            popeListPopupRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedT);
            popeListPopupRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedT);
            ApplyPopeListGraphicColors(startColors, targetColors, easedT);
            yield return null;
        }

        popeListPopupRect.anchoredPosition = targetPosition;
        popeListPopupRect.localScale = targetScale;
        ApplyPopeListGraphicColors(targetColors);

        if (useSubCamera)
        {
            yield return SmoothPopeListSpotlightsToFocusedAlpha();
        }
        else
        {
            ApplyPopeListGraphicColors(GetPopeListTargetGraphicColors(false, true));
        }

        popeListZoomTransitionCoroutine = null;
    }

    private void SwitchToMainCameraImmediate()
    {
        if (cameraTransitionCoroutine != null)
        {
            StopCoroutine(cameraTransitionCoroutine);
            cameraTransitionCoroutine = null;
        }

        if (popeListZoomTransitionCoroutine != null)
        {
            StopCoroutine(popeListZoomTransitionCoroutine);
            popeListZoomTransitionCoroutine = null;
        }

        CacheCameraInitialStates();
        RestoreCameraInitialState(mainCamera);
        RestoreCameraInitialState(subCamera);
        SetCameraView(false);
        RestorePopeListPopupTransform();
        RestorePopeListEffectColors();
        isViewingSubCamera = false;
    }

    private void CachePopeListInitialGraphicColors()
    {
        ResolvePopeListEffectGraphics();

        if (hasPopeListInitialGraphicColors)
        {
            CacheGraphicColors(popeListInitialGraphicColors, popeListTintGraphics);
            CacheGraphicColors(popeListInitialGraphicColors, popeListSpotlightGraphics);
            return;
        }

        popeListInitialGraphicColors.Clear();
        CacheGraphicColors(popeListInitialGraphicColors, popeListTintGraphics);
        CacheGraphicColors(popeListInitialGraphicColors, popeListSpotlightGraphics);
        hasPopeListInitialGraphicColors = popeListInitialGraphicColors.Count > 0;
    }

    private static void CacheGraphicColors(Dictionary<Graphic, Color> target, IEnumerable<Graphic> graphics)
    {
        if (target == null || graphics == null)
        {
            return;
        }

        foreach (Graphic graphic in graphics)
        {
            if (graphic != null && !target.ContainsKey(graphic))
            {
                target[graphic] = graphic.color;
            }
        }
    }

    private Dictionary<Graphic, Color> GetPopeListCurrentGraphicColors()
    {
        Dictionary<Graphic, Color> colors = new();
        CacheGraphicColors(colors, popeListTintGraphics);
        CacheGraphicColors(colors, popeListSpotlightGraphics);
        return colors;
    }

    private Dictionary<Graphic, Color> GetPopeListTargetGraphicColors(bool useSubCamera, bool restoreSpotlights)
    {
        Dictionary<Graphic, Color> colors = new();

        foreach (Graphic graphic in popeListTintGraphics)
        {
            if (graphic == null || !popeListInitialGraphicColors.TryGetValue(graphic, out Color initialColor))
            {
                continue;
            }

            Color targetColor = useSubCamera ? popeListFocusedTintColor : initialColor;
            targetColor.a = initialColor.a;
            colors[graphic] = targetColor;
        }

        foreach (Graphic graphic in popeListSpotlightGraphics)
        {
            if (graphic == null || !popeListInitialGraphicColors.TryGetValue(graphic, out Color initialColor))
            {
                continue;
            }

            Color targetColor = initialColor;
            if (useSubCamera && !restoreSpotlights)
            {
                targetColor.a = Mathf.Clamp01(initialColor.a + popeListSpotlightFocusedAlphaStep);
            }

            colors[graphic] = targetColor;
        }

        return colors;
    }

    private IEnumerator FlickerPopeListSpotlights()
    {
        if (popeListSpotlightGraphics == null || popeListSpotlightGraphics.Count == 0)
        {
            yield break;
        }

        Dictionary<Graphic, Color> baseColors = GetPopeListCurrentSpotlightColors();
        ApplySpotlightAlphaOffset(baseColors, popeListSpotlightFlickerAlphaStep);
        yield return WaitUnscaledSeconds(0.06f);
        ApplySpotlightAlphaOffset(baseColors, -popeListSpotlightFlickerAlphaStep);
        yield return WaitUnscaledSeconds(0.06f);
        ApplyPopeListGraphicColors(baseColors);
        yield return WaitUnscaledSeconds(0.04f);
    }

    private IEnumerator SmoothPopeListSpotlightsToFocusedAlpha()
    {
        Dictionary<Graphic, Color> startColors = GetPopeListCurrentSpotlightColors();
        Dictionary<Graphic, Color> targetColors = new();

        foreach (Graphic graphic in popeListSpotlightGraphics)
        {
            if (graphic == null || !popeListInitialGraphicColors.TryGetValue(graphic, out Color initialColor))
            {
                continue;
            }

            Color targetColor = initialColor;
            targetColor.a = Mathf.Clamp01(initialColor.a + popeListSpotlightFocusedAlphaStep);
            targetColors[graphic] = targetColor;
        }

        float duration = 0.16f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = t * t * (3f - 2f * t);
            ApplyPopeListGraphicColors(startColors, targetColors, easedT);
            yield return null;
        }

        ApplyPopeListGraphicColors(targetColors);
    }

    private Dictionary<Graphic, Color> GetPopeListCurrentSpotlightColors()
    {
        Dictionary<Graphic, Color> colors = new();
        CacheGraphicColors(colors, popeListSpotlightGraphics);
        return colors;
    }

    private void ApplySpotlightAlphaOffset(Dictionary<Graphic, Color> baseColors, float alphaOffset)
    {
        if (baseColors == null)
        {
            return;
        }

        foreach (KeyValuePair<Graphic, Color> entry in baseColors)
        {
            if (entry.Key == null)
            {
                continue;
            }

            Color color = entry.Value;
            color.a = Mathf.Clamp01(color.a + alphaOffset);
            entry.Key.color = color;
        }
    }

    private IEnumerator WaitUnscaledSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ApplyPopeListGraphicColors(Dictionary<Graphic, Color> colors)
    {
        if (colors == null)
        {
            return;
        }

        foreach (KeyValuePair<Graphic, Color> entry in colors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }
    }

    private void ApplyPopeListGraphicColors(
        Dictionary<Graphic, Color> startColors,
        Dictionary<Graphic, Color> targetColors,
        float t)
    {
        if (startColors == null || targetColors == null)
        {
            return;
        }

        foreach (KeyValuePair<Graphic, Color> entry in targetColors)
        {
            if (entry.Key == null)
            {
                continue;
            }

            Color startColor = startColors.TryGetValue(entry.Key, out Color color) ? color : entry.Key.color;
            entry.Key.color = Color.LerpUnclamped(startColor, entry.Value, t);
        }
    }

    private void RestorePopeListEffectColors()
    {
        if (!hasPopeListInitialGraphicColors)
        {
            return;
        }

        ApplyPopeListGraphicColors(popeListInitialGraphicColors);
    }

    private void CachePopeListPopupTransform()
    {
        if (popeListPopupRect == null && popeListPopup != null)
        {
            popeListPopupRect = popeListPopup.GetComponent<RectTransform>();
        }

        if (popeListCanvasRect == null && popeListPopupRect != null)
        {
            Canvas canvas = popeListPopupRect.GetComponentInParent<Canvas>();
            popeListCanvasRect = canvas != null ? canvas.rootCanvas.GetComponent<RectTransform>() : null;
        }

        if (hasPopeListPopupInitialTransform || popeListPopupRect == null)
        {
            return;
        }

        popeListPopupInitialAnchoredPosition = popeListPopupRect.anchoredPosition;
        popeListPopupInitialScale = popeListPopupRect.localScale;
        hasPopeListPopupInitialTransform = true;
    }

    private void RestorePopeListPopupTransform()
    {
        if (!hasPopeListPopupInitialTransform || popeListPopupRect == null)
        {
            return;
        }

        popeListPopupRect.anchoredPosition = popeListPopupInitialAnchoredPosition;
        popeListPopupRect.localScale = popeListPopupInitialScale;
    }

    private void GetPopeListSubCameraTarget(out Vector2 targetPosition, out Vector3 targetScale)
    {
        targetPosition = popeListPopupInitialAnchoredPosition;
        targetScale = popeListPopupInitialScale;

        if (subCamera == null || popeListCanvasRect == null)
        {
            return;
        }

        float zoomScale = 1f;
        if (subCamera.orthographic && subCamera.orthographicSize > 0f)
        {
            zoomScale = popeListCanvasRect.rect.height / (subCamera.orthographicSize * 2f);
        }

        zoomScale = Mathf.Max(0.01f, zoomScale);
        Vector2 subCameraCanvasPosition = popeListCanvasRect.InverseTransformPoint(subCamera.transform.position);
        targetScale = popeListPopupInitialScale * zoomScale;
        targetPosition = popeListPopupInitialAnchoredPosition - subCameraCanvasPosition * zoomScale;
    }

    private void CacheCameraInitialStates()
    {
        if (hasCameraInitialStates || mainCamera == null || subCamera == null)
        {
            return;
        }

        mainCameraInitialState = CameraState.From(mainCamera);
        subCameraInitialState = CameraState.From(subCamera);
        hasCameraInitialStates = true;
    }

    private void RestoreCameraInitialState(Camera targetCamera)
    {
        if (!hasCameraInitialStates || targetCamera == null)
        {
            return;
        }

        if (targetCamera == mainCamera)
        {
            mainCameraInitialState.ApplyTo(targetCamera);
        }
        else if (targetCamera == subCamera)
        {
            subCameraInitialState.ApplyTo(targetCamera);
        }
    }

    private void SetCameraView(bool useSubCamera)
    {
        if (mainCamera != null)
        {
            mainCamera.enabled = !useSubCamera;
            SetCameraAudio(mainCamera, !useSubCamera);
        }

        if (subCamera != null)
        {
            subCamera.enabled = useSubCamera;
            SetCameraAudio(subCamera, useSubCamera);
        }
    }

    private Camera GetCurrentlyEnabledCamera()
    {
        if (mainCamera != null && mainCamera.enabled)
        {
            return mainCamera;
        }

        if (subCamera != null && subCamera.enabled)
        {
            return subCamera;
        }

        return isViewingSubCamera ? subCamera : mainCamera;
    }

    private static void SetCameraAudio(Camera targetCamera, bool isEnabled)
    {
        if (targetCamera == null)
        {
            return;
        }

        AudioListener audioListener = targetCamera.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = isEnabled;
        }
    }

    private static Camera FindCameraByName(string cameraName)
    {
        if (string.IsNullOrWhiteSpace(cameraName))
        {
            return null;
        }

        foreach (Camera camera in FindObjectsByType<Camera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None))
        {
            if (camera != null && camera.name == cameraName)
            {
                return camera;
            }
        }

        return null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindDeepChild(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void EnsureEventSystemNavigationDisabled()
    {
        if (hasCachedSendNavigationEvents || EventSystem.current == null)
        {
            return;
        }

        cachedSendNavigationEvents = EventSystem.current.sendNavigationEvents;
        hasCachedSendNavigationEvents = true;
        EventSystem.current.sendNavigationEvents = false;
    }

    private void RestoreEventSystemNavigation()
    {
        if (!hasCachedSendNavigationEvents || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.sendNavigationEvents = cachedSendNavigationEvents;
        hasCachedSendNavigationEvents = false;
    }

    private static int WrapIndex(int index, int count)
    {
        if (count <= 0)
        {
            return -1;
        }

        int wrappedIndex = index % count;
        return wrappedIndex < 0 ? wrappedIndex + count : wrappedIndex;
    }

    private static bool WasMoveLeftPressed()
    {
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
    }

    private static bool WasMoveRightPressed()
    {
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
    }

    private static bool WasMoveUpPressed()
    {
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
    }

    private static bool WasMoveDownPressed()
    {
        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
    }

    private static bool WasSubmitPressed()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    private static bool WasCancelPressed()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }

    private struct CameraState
    {
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly bool orthographic;
        private readonly float orthographicSize;
        private readonly float fieldOfView;
        private readonly bool isValid;

        private CameraState(
            Vector3 position,
            Quaternion rotation,
            bool orthographic,
            float orthographicSize,
            float fieldOfView)
        {
            this.position = position;
            this.rotation = rotation;
            this.orthographic = orthographic;
            this.orthographicSize = orthographicSize;
            this.fieldOfView = fieldOfView;
            isValid = true;
        }

        public static CameraState From(Camera camera)
        {
            return new CameraState(
                camera.transform.position,
                camera.transform.rotation,
                camera.orthographic,
                camera.orthographicSize,
                camera.fieldOfView);
        }

        public static CameraState Lerp(CameraState from, CameraState to, float t)
        {
            return new CameraState(
                Vector3.LerpUnclamped(from.position, to.position, t),
                Quaternion.SlerpUnclamped(from.rotation, to.rotation, t),
                t < 1f ? from.orthographic : to.orthographic,
                Mathf.LerpUnclamped(from.orthographicSize, to.orthographicSize, t),
                Mathf.LerpUnclamped(from.fieldOfView, to.fieldOfView, t));
        }

        public void ApplyTo(Camera camera)
        {
            if (!isValid || camera == null)
            {
                return;
            }

            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographic = orthographic;
            camera.orthographicSize = orthographicSize;
            camera.fieldOfView = fieldOfView;
        }
    }
}
