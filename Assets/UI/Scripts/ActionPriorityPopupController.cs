using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ActionPriorityPopupController : MonoBehaviour
{
    private const string HpColor = "#5BD65B";
    private const string PietyColor = "#FFD84D";
    private const string InfluenceColor = "#4488FF";
    private const float TutorialArrowMinY = 0.65f;
    private const float TutorialArrowMaxY = 1f;
    private const float TutorialArrowTravelDuration = 0.5f;

    private enum ActionType
    {
        None,
        Prayer,
        Speech
    }

    private GameObject prayerPopup;
    private GameObject speechPopup;
    private Gamsil gamsil;
    private Lecture lecture;
    private BoxCollider2D prayerClickCollider;
    private BoxCollider2D speechClickCollider;
    private Transform prayerTutorialArrow;
    private Transform speechTutorialArrow;
    private ActionType pendingAction;
    private bool isConfirming;
    private bool? tutorialSchemeLocked;

    public static void Attach(GameObject host)
    {
        if (host == null)
        {
            return;
        }

        ActionPriorityPopupController controller = host.GetComponent<ActionPriorityPopupController>();
        if (controller == null)
        {
            controller = host.AddComponent<ActionPriorityPopupController>();
        }

        controller.Configure();
    }

    private void Configure()
    {
        prayerPopup = FindSceneObjectIncludingInactive("PrayPopUP");
        speechPopup = FindSceneObjectIncludingInactive("SpeechPopUP");
        gamsil = FindAnyObjectByType<Gamsil>();
        lecture = FindAnyObjectByType<Lecture>();

        ConfigurePopup(prayerPopup);
        ConfigurePopup(speechPopup);
        prayerClickCollider = ConfigureWorldClickCollider("gamsil_0");
        speechClickCollider = ConfigureWorldClickCollider("lecturn_0");
        prayerTutorialArrow = FindTutorialArrow("gamsil_0");
        speechTutorialArrow = FindTutorialArrow("lecturn_0");

        CloseAllPopups();
        SetTutorialArrow(prayerTutorialArrow, false);
        SetTutorialArrow(speechTutorialArrow, false);
    }

    private void ConfigurePopup(GameObject popup)
    {
        if (popup == null)
        {
            return;
        }

        Button yesButton = FindDeepChild(popup.transform, "Yes")?.GetComponent<Button>();
        Button noButton = FindDeepChild(popup.transform, "No")?.GetComponent<Button>();

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(ConfirmAction);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(CloseAllPopups);
        }
    }

    private static BoxCollider2D ConfigureWorldClickCollider(string objectName)
    {
        GameObject target = FindSceneObjectIncludingInactive(objectName);
        if (target == null)
        {
            return null;
        }

        return WorldActionButtonRaycastTarget.Configure(target);
    }

    private void Update()
    {
        UpdateInitialTutorialGuidance();

        if (isConfirming || IsAnyPopupOpen() ||
            CardinalManager.Instance != null && CardinalManager.Instance.IsConclaveTransitionInProgress)
        {
            return;
        }

        if (SettingsService.Instance?.IsInputCaptured == true)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (IsHotKeyPressedThisFrame(keyboard, HotKeyAction.Pray, Key.F))
        {
            TryStartActionImmediately(ActionType.Prayer);
            return;
        }

        if (IsHotKeyPressedThisFrame(keyboard, HotKeyAction.Speech, Key.G))
        {
            TryStartActionImmediately(ActionType.Speech);
            return;
        }

        Mouse mouse = Mouse.current;
        Camera mainCamera = Camera.main;
        if (mouse == null || mainCamera == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 screenPosition = mouse.position.ReadValue();
        if (IsPointerOverCollider(prayerClickCollider, mainCamera, screenPosition))
        {
            OpenPopup(ActionType.Prayer);
        }
        else if (IsPointerOverCollider(speechClickCollider, mainCamera, screenPosition))
        {
            OpenPopup(ActionType.Speech);
        }
    }

    private void OpenPopup(ActionType actionType)
    {
        if (isConfirming || !CanUseAction(actionType))
        {
            return;
        }

        pendingAction = actionType;
        GameObject popup = actionType == ActionType.Prayer ? prayerPopup : speechPopup;
        if (popup == null)
        {
            return;
        }

        PopulatePopup(popup, actionType);
        popup.SetActive(true);
    }

    private void PopulatePopup(GameObject popup, ActionType actionType)
    {
        GameBalance balance = InGameManager.Instance != null ? InGameManager.Instance.Balance : null;
        Cardinal player = InventoryManager.Instance != null ? InventoryManager.Instance.Player : null;
        if (balance == null || player == null)
        {
            SetUndefinedPopupValuesToZero(popup);
            return;
        }

        if (actionType == ActionType.Prayer)
        {
            player.GetPrayerDeltaPreview(balance, true, out float successPiety, out float successHp);
            player.GetPrayerDeltaPreview(balance, false, out float failPiety, out float failHp);

            SetPopupText(popup, "Desc/Info1/Plus", FormatAdjusted(
                balance.PraySuccessDeltaPiety, successPiety, PietyColor));
            SetPopupText(popup, "Desc/Info1/Plus-2", FormatAdjusted(
                balance.PraySuccessDeltaHp, successHp, HpColor));
            SetPopupText(popup, "Desc/Info1 (1)/Minus", FormatAdjusted(
                balance.PrayFailDeltaPiety, failPiety, PietyColor));
            SetPopupText(popup, "Desc/Info1 (1)/MInus-2", FormatAdjusted(
                balance.PrayFailDeltaHp, failHp, HpColor));
            return;
        }

        player.GetSpeechDeltaPreview(balance, true, balance.SpeechSuccessDeltaInfluenceMin,
            out float successInfluenceMin, out float successSpeechHp);
        player.GetSpeechDeltaPreview(balance, true, balance.SpeechSuccessDeltaInfluenceMax,
            out float successInfluenceMax, out _);
        player.GetSpeechDeltaPreview(balance, false, balance.SpeechFailDeltaInfluence,
            out float failInfluence, out float failSpeechHp);

        SetPopupText(popup, "Desc/Info1/Plus", FormatAdjustedRange(
            balance.SpeechSuccessDeltaInfluenceMin,
            balance.SpeechSuccessDeltaInfluenceMax,
            successInfluenceMin,
            successInfluenceMax,
            InfluenceColor));
        SetPopupText(popup, "Desc/Info1/Plus-2", FormatAdjusted(
            balance.SpeechSuccessDeltaHp, successSpeechHp, HpColor));
        SetPopupText(popup, "Desc/Info1 (1)/Minus", FormatAdjusted(
            balance.SpeechFailDeltaInfluence, failInfluence, InfluenceColor));
        SetPopupText(popup, "Desc/Info1 (1)/MInus-2", FormatAdjusted(
            balance.SpeechFailDeltaHp, failSpeechHp, HpColor));
    }

    private void ConfirmAction()
    {
        if (isConfirming || pendingAction == ActionType.None)
        {
            return;
        }

        TryStartActionImmediately(pendingAction);
    }

    private bool TryStartActionImmediately(ActionType actionType)
    {
        if (isConfirming || actionType == ActionType.None)
        {
            return false;
        }

        Transform playerTransform = CardinalManager.Instance != null ? CardinalManager.Instance.PlayerTransform : null;
        StateController playerState = playerTransform != null ? playerTransform.GetComponent<StateController>() : null;
        if (playerState == null || !CanUseAction(actionType, playerState))
        {
            CloseAllPopups();
            return false;
        }

        isConfirming = true;
        bool started = actionType == ActionType.Prayer
            ? (gamsil != null && gamsil.TryStartPlayerPrayerImmediately(playerState))
            : (lecture != null && lecture.TryStartPlayerSpeechImmediately(playerState));

        if (started)
        {
            CloseAllPopups();
            GameSceneCameraZoom.ZoomAllGameCamerasToMinimum(1f);
        }

        isConfirming = false;
        return started;
    }

    private static bool IsHotKeyPressedThisFrame(
        Keyboard keyboard,
        HotKeyAction action,
        Key fallbackKey)
    {
        if (keyboard == null)
        {
            return false;
        }

        Key key = SettingsManager.Instance != null
            ? SettingsManager.Instance.GetHotKey(action)
            : fallbackKey;
        return key != Key.None && keyboard[key].wasPressedThisFrame;
    }

    private void UpdateInitialTutorialGuidance()
    {
        InGameManager manager = InGameManager.Instance;
        NPCBehaviour requiredAction = manager != null
            ? manager.InitialTutorialRequiredAction
            : NPCBehaviour.None;
        Transform playerTransform = CardinalManager.Instance != null
            ? CardinalManager.Instance.PlayerTransform
            : null;
        StateController playerState = playerTransform != null
            ? playerTransform.GetComponent<StateController>()
            : null;

        SetTutorialArrow(prayerTutorialArrow, requiredAction == NPCBehaviour.Pray &&
            playerState?.IsPerformingPrayerAction != true);
        SetTutorialArrow(speechTutorialArrow, requiredAction == NPCBehaviour.Speech &&
            playerState?.IsPerformingSpeechAction != true);

        bool schemeLocked = manager != null && manager.IsInitialTutorialLocked;
        if (tutorialSchemeLocked == schemeLocked) return;

        tutorialSchemeLocked = schemeLocked;
        CardinalManager.Instance?.SetInitialTutorialSchemeLock(schemeLocked);
    }

    private bool CanUseAction(ActionType actionType, StateController playerState = null)
    {
        if (actionType == ActionType.None) return false;
        if (playerState == null)
        {
            Transform playerTransform = CardinalManager.Instance != null
                ? CardinalManager.Instance.PlayerTransform
                : null;
            playerState = playerTransform != null ? playerTransform.GetComponent<StateController>() : null;
        }

        if (playerState == null) return false;
        NPCBehaviour action = actionType == ActionType.Prayer ? NPCBehaviour.Pray : NPCBehaviour.Speech;
        return InGameManager.Instance == null
            ? playerState.CanAcceptManualInteraction()
            : InGameManager.Instance.CanStartPlayerWorldAction(action, playerState);
    }

    private static Transform FindTutorialArrow(string parentName)
    {
        GameObject parent = FindSceneObjectIncludingInactive(parentName);
        return parent != null ? FindDeepChild(parent.transform, "Arrow") : null;
    }

    private static void SetTutorialArrow(Transform arrow, bool active)
    {
        if (arrow == null) return;

        arrow.gameObject.SetActive(active);
        Vector3 position = arrow.localPosition;
        if (active)
        {
            float phase = Mathf.PingPong(Time.unscaledTime / TutorialArrowTravelDuration, 1f);
            float wave = (1f - Mathf.Cos(Mathf.PI * phase)) * 0.5f;
            position.y = Mathf.Lerp(TutorialArrowMinY, TutorialArrowMaxY, wave);
        }
        else
        {
            position.y = TutorialArrowMinY;
        }
        arrow.localPosition = position;
    }

    private void CloseAllPopups()
    {
        if (prayerPopup != null)
        {
            prayerPopup.SetActive(false);
        }

        if (speechPopup != null)
        {
            speechPopup.SetActive(false);
        }

        pendingAction = ActionType.None;
    }

    private bool IsAnyPopupOpen()
    {
        return (prayerPopup != null && prayerPopup.activeSelf) ||
               (speechPopup != null && speechPopup.activeSelf);
    }

    private static bool IsPointerOverCollider(Collider2D collider, Camera camera, Vector2 screenPosition)
    {
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(
            screenPosition.x,
            screenPosition.y,
            -camera.transform.position.z));
        return collider.OverlapPoint(new Vector2(worldPosition.x, worldPosition.y));
    }

    private static void SetPopupText(GameObject popup, string path, string value)
    {
        Transform target = FindPath(popup.transform, path);
        TMP_Text text = target != null ? target.GetComponent<TMP_Text>() : null;
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetUndefinedPopupValuesToZero(GameObject popup)
    {
        SetPopupText(popup, "Desc/Info1/Plus", "0");
        SetPopupText(popup, "Desc/Info1/Plus-2", "0");
        SetPopupText(popup, "Desc/Info1 (1)/Minus", "0");
        SetPopupText(popup, "Desc/Info1 (1)/MInus-2", "0");
    }

    private static string FormatSigned(float value)
    {
        return value.ToString("+0.#;-0.#;0");
    }

    private static string FormatSignedRange(float minimum, float maximum)
    {
        return Mathf.Approximately(minimum, maximum)
            ? FormatSigned(minimum)
            : $"{FormatSigned(minimum)}~{FormatSigned(maximum)}";
    }

    private static string FormatAdjusted(float baseValue, float adjustedValue, string color)
    {
        if (Mathf.Approximately(baseValue, adjustedValue))
        {
            return FormatSigned(baseValue);
        }

        float adjustment = adjustedValue - baseValue;
        return $"<color={color}>{FormatSigned(adjustedValue)}</color> " +
               $"({FormatSigned(baseValue)} {FormatAdjustment(adjustment)})";
    }

    private static string FormatAdjustedRange(
        float baseMinimum,
        float baseMaximum,
        float adjustedMinimum,
        float adjustedMaximum,
        string color)
    {
        string baseText = FormatSignedRange(baseMinimum, baseMaximum);
        if (Mathf.Approximately(baseMinimum, adjustedMinimum) &&
            Mathf.Approximately(baseMaximum, adjustedMaximum))
        {
            return baseText;
        }

        if (Mathf.Approximately(baseMinimum, baseMaximum) &&
            Mathf.Approximately(adjustedMinimum, adjustedMaximum))
        {
            return FormatAdjusted(baseMinimum, adjustedMinimum, color);
        }

        float minimumAdjustment = adjustedMinimum - baseMinimum;
        float maximumAdjustment = adjustedMaximum - baseMaximum;
        string calculation = Mathf.Approximately(minimumAdjustment, maximumAdjustment)
            ? $"{baseText} {FormatAdjustment(minimumAdjustment)}"
            : $"{FormatSigned(baseMinimum)} {FormatAdjustment(minimumAdjustment)} ~ " +
              $"{FormatSigned(baseMaximum)} {FormatAdjustment(maximumAdjustment)}";

        return $"<color={color}>{FormatSignedRange(adjustedMinimum, adjustedMaximum)}</color> ({calculation})";
    }

    private static string FormatAdjustment(float adjustment)
    {
        string operation = adjustment >= 0f ? "+" : "-";
        return $"{operation} {Mathf.Abs(adjustment):0.#}";
    }

    private static Transform FindPath(Transform root, string path)
    {
        Transform current = root;
        foreach (string part in path.Split('/'))
        {
            current = FindDeepChild(current, part);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private static GameObject FindSceneObjectIncludingInactive(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDeepChild(root.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int childIndex = 0; childIndex < root.childCount; childIndex++)
        {
            Transform found = FindDeepChild(root.GetChild(childIndex), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
