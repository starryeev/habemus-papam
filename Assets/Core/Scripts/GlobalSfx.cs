using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GlobalSfx : MonoBehaviour
{
    private const string MainSceneName = "MainScene";
    private const string GameSceneName = "GameScene";
    private const string OnMouseSfxName = "OnMouse";
    private const string ButtonLightSfxName = "ButtonLight";
    private const string ButtonHeavySfxName = "ButtonHeavy";
    private const string UnavailableSfxName = "25 인게임- 발동불가";
    private static readonly HashSet<string> selectionEffectButtonNames = new()
    {
        "GameStartBtn",
        "LoadBtn",
        "Setting",
        "ResetData",
        "Dict",
        "PopeList",
    };

    private static GlobalSfx instance;
    private readonly List<RaycastResult> raycastResults = new();
    private EventSystem pointerEventSystem;
    private PointerEventData pointerEventData;
    private Button hoveredButton;
    private Button pointedButton;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != MainSceneName && sceneName != GameSceneName)
        {
            hoveredButton = null;
            pointedButton = null;
            return;
        }

        if (sceneName == GameSceneName && CardinalManager.Instance != null &&
            CardinalManager.Instance.IsConclaveTransitionInProgress)
        {
            hoveredButton = null;
            pointedButton = null;
            return;
        }

        bool clicked = Input.GetMouseButtonDown(0);
        UpdateButtonHover(!clicked, sceneName == MainSceneName);

        if (clicked)
        {
            PlayClickSound(sceneName);
        }
    }

    private void UpdateButtonHover(bool playSound, bool isMainScene)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            hoveredButton = null;
            pointedButton = null;
            return;
        }

        if (pointerEventData == null || pointerEventSystem != eventSystem)
        {
            pointerEventSystem = eventSystem;
            pointerEventData = new PointerEventData(eventSystem);
        }

        pointerEventData.Reset();
        pointerEventData.position = Input.mousePosition;
        raycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, raycastResults);

        Button currentButton = raycastResults.Count > 0
            ? raycastResults[0].gameObject.GetComponentInParent<Button>()
            : null;
        pointedButton = currentButton;

        if (currentButton != null &&
            (!currentButton.isActiveAndEnabled || !currentButton.interactable))
        {
            currentButton = null;
        }

        if (currentButton == hoveredButton)
        {
            return;
        }

        hoveredButton = currentButton;
        if (playSound && hoveredButton != null &&
            ShouldPlayHoverSound(hoveredButton, isMainScene))
        {
            PlayOnMouse();
        }
    }

    private static bool ShouldPlayHoverSound(Button button, bool isMainScene)
    {
        return !isMainScene ||
            !selectionEffectButtonNames.Contains(button.name) ||
            HasActiveSelectionEffect(button);
    }

    private static bool HasActiveSelectionEffect(Button button)
    {
        Graphic targetGraphic = button.targetGraphic;
        Outline outline = targetGraphic != null
            ? targetGraphic.GetComponent<Outline>()
            : button.GetComponentInChildren<Outline>(true);

        return outline != null && outline.enabled;
    }

    private static void PlayOnMouse()
    {
        SoundManager.Instance.PlaySFX(OnMouseSfxName);
    }

    private void PlayClickSound(string sceneName)
    {
        if (sceneName == GameSceneName && IsUnavailableAction(pointedButton))
        {
            SoundManager.Instance.PlaySFX(UnavailableSfxName);
            return;
        }

        string sfxName = GetClickSfxName(hoveredButton, sceneName);
        if (!string.IsNullOrEmpty(sfxName))
        {
            SoundManager.Instance.PlaySFX(sfxName);
        }
    }

    private static bool IsUnavailableAction(Button button)
    {
        if (button == null)
        {
            return false;
        }

        ChoiceButton choiceButton = button.GetComponentInParent<ChoiceButton>();
        if (choiceButton != null && choiceButton.IsConditionUnavailable)
        {
            return true;
        }

        PlotUI plotUI = button.GetComponentInParent<PlotUI>();
        return plotUI != null && plotUI.IsConditionUnavailable(button);
    }

    private static string GetClickSfxName(Button button, string sceneName)
    {
        if (button == null)
        {
            return OnMouseSfxName;
        }

        string buttonName = button.name;
        if (sceneName == GameSceneName && button.GetComponent<VoteButton>() != null)
        {
            return null;
        }

        if (sceneName == MainSceneName)
        {
            if (buttonName == "Start" &&
                HasAncestor(button.transform, "WarningPopUp"))
            {
                return ButtonHeavySfxName;
            }

            if ((buttonName == "Start" || buttonName == "Back") &&
                HasAncestor(button.transform, "ResetPopUP") ||
                buttonName == "Back" &&
                HasAncestor(button.transform, "WarningPopUp"))
            {
                return ButtonLightSfxName;
            }

            if ((buttonName == "Yes" || buttonName == "Yes (1)") &&
                HasAncestor(button.transform, "DictPopUP"))
            {
                return ButtonLightSfxName;
            }

            if (buttonName == "Back" &&
                HasAncestor(button.transform, "loadWarningPopup"))
            {
                return ButtonLightSfxName;
            }

            if (buttonName == "LoadBtn" &&
                HasAncestor(button.transform, "loadPopup"))
            {
                return ButtonHeavySfxName;
            }
        }

        if (sceneName == GameSceneName &&
            (buttonName == "ChoiceButton1" || buttonName == "ChoiceButton2" ||
             HasAncestor(button.transform, "ChoiceButton1") ||
             HasAncestor(button.transform, "ChoiceButton2")) &&
            HasAncestor(button.transform, "EventWindow"))
        {
            return ButtonHeavySfxName;
        }

        return OnMouseSfxName;
    }

    private static bool HasAncestor(Transform transform, string ancestorName)
    {
        for (Transform current = transform.parent; current != null; current = current.parent)
        {
            if (current.name == ancestorName)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
