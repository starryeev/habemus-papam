using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ActionPriorityPopupController : MonoBehaviour
{
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
    private ActionType pendingAction;
    private bool isConfirming;

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

        CloseAllPopups();
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
        if (isConfirming || IsAnyPopupOpen())
        {
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
        if (isConfirming)
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
        if (balance == null)
        {
            return;
        }

        if (actionType == ActionType.Prayer)
        {
            SetPopupText(popup, "Desc/Info1/Plus", FormatSigned(balance.PraySuccessDeltaPiety));
            SetPopupText(popup, "Desc/Info1/Plus-2", FormatSigned(balance.PraySuccessDeltaHp));
            SetPopupText(popup, "Desc/Info1 (1)/Minus", FormatSigned(balance.PrayFailDeltaPiety));
            SetPopupText(popup, "Desc/Info1 (1)/Minus-2", FormatSigned(balance.PrayFailDeltaHp));
            return;
        }

        SetPopupText(popup, "Desc/Info1/Plus", FormatSignedRange(
            balance.SpeechSuccessDeltaInfluenceMin,
            balance.SpeechSuccessDeltaInfluenceMax));
        SetPopupText(popup, "Desc/Info1/Plus-2", FormatSigned(balance.SpeechSuccessDeltaHp));
        SetPopupText(popup, "Desc/Info1 (1)/Minus", FormatSigned(balance.SpeechFailDeltaInfluence));
        SetPopupText(popup, "Desc/Info1 (1)/Minus-2", FormatSigned(balance.SpeechFailDeltaHp));
    }

    private void ConfirmAction()
    {
        if (isConfirming || pendingAction == ActionType.None)
        {
            return;
        }

        Transform playerTransform = CardinalManager.Instance != null ? CardinalManager.Instance.PlayerTransform : null;
        StateController playerState = playerTransform != null ? playerTransform.GetComponent<StateController>() : null;
        if (playerState == null)
        {
            return;
        }

        isConfirming = true;
        bool started = pendingAction == ActionType.Prayer
            ? (gamsil != null && gamsil.TryStartPlayerPrayerImmediately(playerState))
            : (lecture != null && lecture.TryStartPlayerSpeechImmediately(playerState));

        if (started)
        {
            CloseAllPopups();
            GameSceneCameraZoom.ReleaseAllGameCameraZoomAndFollow(1f);
        }

        isConfirming = false;
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
