using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GlobalSfx : MonoBehaviour
{
    private const string MainSceneName = "MainScene";
    private const string GameSceneName = "GameScene";
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
            return;
        }

        bool clicked = Input.GetMouseButtonDown(0);
        UpdateButtonHover(!clicked, sceneName == MainSceneName);

        if (clicked)
        {
            PlayOnMouse();
        }
    }

    private void UpdateButtonHover(bool playSound, bool isMainScene)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            hoveredButton = null;
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
        SoundManager.Instance.PlaySFX("OnMouse");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
