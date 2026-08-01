using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSet : MonoBehaviour
{
    private const float DisabledAlpha = 0.8f;
    private const float EnabledAlpha = 1f;
    private const float HandleDarkDuration = 0.5f;
    private const float HandleBrightDuration = 1.5f;
    private const float HandleDarkenAmount = 0.55f;
    private const float ArrowHorizontalOffset = 18f;
    private const string GrayscaleShaderName = "UI/Grayscale";
    private const string GrayscaleShaderResourcePath = "UI/Shaders/UIGrayscale";

    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    private Graphic[] _sliderGraphics;
    private Material[] _sliderOriginalMaterials;
    private Material _grayscaleMaterial;
    private Graphic _sliderHandleGraphic;
    private Color _sliderHandleBaseColor;
    private GameObject _sliderLeftArrow;
    private GameObject _sliderRightArrow;
    private GameObject _muteLeftArrow;
    private GameObject _muteRightArrow;
    private Coroutine _handleBlinkCoroutine;
    private WaitForSecondsRealtime _handleDarkWait;
    private WaitForSecondsRealtime _handleBrightWait;

    public Slider VolumeSlider => volumeSlider;
    public TMP_Text ValueText => valueText;
    public Toggle MuteToggle => muteToggle;
    public bool CanEditVolume => volumeSlider != null && volumeSlider.interactable;

    private void Awake()
    {
        CacheVisuals();
        CreateSelectionArrows();
        SetSelectionArrows(false, false);
    }

    private void OnDisable()
    {
        StopHandleBlink();
        SetSelectionArrows(false, false);
    }

    private void OnDestroy()
    {
        if (_grayscaleMaterial != null)
        {
            Destroy(_grayscaleMaterial);
        }
    }

    private void Reset()
    {
        valueText = transform.Find("Value")?.GetComponent<TMP_Text>();
        volumeSlider = transform.Find("Slider")?.GetComponent<Slider>();
        muteToggle = transform.Find("Mute")?.GetComponent<Toggle>();
    }

    public void SetValue(float value)
    {
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(value);
        }

        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(value).ToString();
        }
    }

    public void SetMute(bool isMuted)
    {
        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(isMuted);
        }

        ApplyMutedVisual(isMuted);
    }

    public float GetValue()
    {
        return volumeSlider != null ? volumeSlider.value : 0f;
    }

    public bool IsMuted()
    {
        return muteToggle != null && muteToggle.isOn;
    }

    public void AdjustVolume(float delta)
    {
        if (!CanEditVolume || Mathf.Approximately(delta, 0f))
        {
            return;
        }

        volumeSlider.value = Mathf.Clamp(
            volumeSlider.value + delta,
            volumeSlider.minValue,
            volumeSlider.maxValue);
    }

    public void ToggleMute()
    {
        if (muteToggle != null && muteToggle.interactable)
        {
            muteToggle.isOn = !muteToggle.isOn;
        }
    }

    public void SetKeyboardSelection(bool isSelected, VolumeControlTarget target, bool isEditing)
    {
        bool isSliderSelected = isSelected && target == VolumeControlTarget.Slider;
        bool isMuteSelected = isSelected && target == VolumeControlTarget.Mute;

        SetSelectionArrows(isSliderSelected, isMuteSelected);
        SetHandleBlink(isSliderSelected && isEditing && CanEditVolume);
    }

    public void RefreshText()
    {
        if (volumeSlider != null && valueText != null)
        {
            valueText.text = Mathf.RoundToInt(volumeSlider.value).ToString();
        }
    }

    public void ApplyMutedVisual(bool isMuted)
    {
        if (volumeSlider != null)
        {
            volumeSlider.interactable = !isMuted;
            SetCanvasGroupState(volumeSlider.gameObject, isMuted);
            ApplyGrayscaleMaterial(isMuted);

            if (isMuted)
            {
                StopHandleBlink();
            }
        }

        if (valueText != null)
        {
            SetCanvasGroupState(valueText.gameObject, isMuted);
        }
    }

    private void SetCanvasGroupState(GameObject target, bool isMuted)
    {
        CanvasGroup canvasGroup = GetOrAddCanvasGroup(target);
        canvasGroup.alpha = isMuted ? DisabledAlpha : EnabledAlpha;
        canvasGroup.interactable = !isMuted;
        canvasGroup.blocksRaycasts = !isMuted;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private void CacheVisuals()
    {
        if (volumeSlider == null)
        {
            return;
        }

        _sliderGraphics = volumeSlider.GetComponentsInChildren<Graphic>(true);
        _sliderOriginalMaterials = new Material[_sliderGraphics.Length];
        for (int index = 0; index < _sliderGraphics.Length; index++)
        {
            _sliderOriginalMaterials[index] = _sliderGraphics[index].material;
        }

        _sliderHandleGraphic = volumeSlider.targetGraphic;
        if (_sliderHandleGraphic != null)
        {
            _sliderHandleBaseColor = _sliderHandleGraphic.color;
        }

        Shader grayscaleShader = Resources.Load<Shader>(GrayscaleShaderResourcePath);
        if (grayscaleShader == null)
        {
            grayscaleShader = Shader.Find(GrayscaleShaderName);
        }
        if (grayscaleShader != null)
        {
            _grayscaleMaterial = new Material(grayscaleShader)
            {
                name = $"{name}_RuntimeGrayscale"
            };
        }
        else
        {
            Debug.LogWarning($"Grayscale shader not found: {GrayscaleShaderName}", this);
        }

        _handleDarkWait = new WaitForSecondsRealtime(HandleDarkDuration);
        _handleBrightWait = new WaitForSecondsRealtime(HandleBrightDuration);
    }

    private void ApplyGrayscaleMaterial(bool isMuted)
    {
        if (_sliderGraphics == null || _sliderOriginalMaterials == null)
        {
            return;
        }

        for (int index = 0; index < _sliderGraphics.Length; index++)
        {
            _sliderGraphics[index].material = isMuted && _grayscaleMaterial != null
                ? _grayscaleMaterial
                : _sliderOriginalMaterials[index];
        }
    }

    private void CreateSelectionArrows()
    {
        RectTransform rowRect = transform as RectTransform;
        if (rowRect != null && TryGetSliderSelectionBounds(rowRect, out Bounds sliderBounds))
        {
            _sliderLeftArrow = CreateArrowAtLocalPosition(
                rowRect,
                new Vector2(sliderBounds.min.x - ArrowHorizontalOffset, sliderBounds.center.y),
                sliderBounds.size.y,
                true,
                "SliderLeftSelectionArrow");

            _sliderRightArrow = CreateArrowAtLocalPosition(
                rowRect,
                new Vector2(sliderBounds.max.x + ArrowHorizontalOffset, sliderBounds.center.y),
                sliderBounds.size.y,
                false,
                "SliderRightSelectionArrow");
        }

        if (muteToggle != null)
        {
            RectTransform muteRect = muteToggle.transform as RectTransform;
            _muteLeftArrow = CreateArrow(muteRect, true, "MuteLeftSelectionArrow");
            _muteRightArrow = CreateArrow(muteRect, false, "MuteRightSelectionArrow");
        }
    }

    private bool TryGetSliderSelectionBounds(RectTransform rowRect, out Bounds bounds)
    {
        Vector3[] worldCorners = new Vector3[4];
        bool hasBounds = false;
        Vector3 minimum = Vector3.zero;
        Vector3 maximum = Vector3.zero;

        for (int childIndex = 0; childIndex < rowRect.childCount; childIndex++)
        {
            RectTransform childRect = rowRect.GetChild(childIndex) as RectTransform;
            if (childRect == null || (muteToggle != null && childRect == muteToggle.transform))
            {
                continue;
            }

            childRect.GetWorldCorners(worldCorners);
            for (int cornerIndex = 0; cornerIndex < worldCorners.Length; cornerIndex++)
            {
                Vector3 localCorner = rowRect.InverseTransformPoint(worldCorners[cornerIndex]);
                if (!hasBounds)
                {
                    minimum = localCorner;
                    maximum = localCorner;
                    hasBounds = true;
                    continue;
                }

                minimum = Vector3.Min(minimum, localCorner);
                maximum = Vector3.Max(maximum, localCorner);
            }
        }

        bounds = hasBounds
            ? new Bounds((minimum + maximum) * 0.5f, maximum - minimum)
            : default;
        return hasBounds;
    }

    private GameObject CreateArrowAtLocalPosition(
        RectTransform parent,
        Vector2 localPosition,
        float referenceHeight,
        bool isLeftSide,
        string objectName)
    {
        GameObject arrowObject = CreateArrowObject(objectName, isLeftSide, referenceHeight);
        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.SetParent(parent, false);
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = arrowRect.anchorMin;
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        return arrowObject;
    }

    private GameObject CreateArrow(RectTransform parent, bool isLeftSide, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject arrowObject = CreateArrowObject(objectName, isLeftSide, parent.rect.height);
        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.SetParent(parent, false);
        arrowRect.anchorMin = new Vector2(isLeftSide ? 0f : 1f, 0.5f);
        arrowRect.anchorMax = arrowRect.anchorMin;
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(isLeftSide ? -ArrowHorizontalOffset : ArrowHorizontalOffset, 0f);
        return arrowObject;
    }

    private GameObject CreateArrowObject(string objectName, bool isLeftSide, float referenceHeight)
    {
        GameObject arrowObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(32f, Mathf.Max(24f, referenceHeight));

        TextMeshProUGUI arrowText = arrowObject.GetComponent<TextMeshProUGUI>();
        arrowText.raycastTarget = false;
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.textWrappingMode = TextWrappingModes.NoWrap;
        arrowText.fontSize = Mathf.Clamp(referenceHeight * 0.7f, 18f, 36f);

        if (valueText != null)
        {
            arrowText.font = valueText.font;
            arrowText.color = valueText.color;
            arrowText.fontStyle = valueText.fontStyle;
        }

        arrowText.text = GetArrowText(arrowText.font, isLeftSide);
        return arrowObject;
    }

    private static string GetArrowText(TMP_FontAsset font, bool isLeftSide)
    {
        char arrowCharacter = isLeftSide ? '\u2192' : '\u2190';
        if (font != null && font.HasCharacter(arrowCharacter))
        {
            return arrowCharacter.ToString();
        }

        return isLeftSide ? ">" : "<";
    }

    private void SetSelectionArrows(bool isSliderSelected, bool isMuteSelected)
    {
        SetActive(_sliderLeftArrow, isSliderSelected);
        SetActive(_sliderRightArrow, isSliderSelected);
        SetActive(_muteLeftArrow, isMuteSelected);
        SetActive(_muteRightArrow, isMuteSelected);
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null && target.activeSelf != isActive)
        {
            target.SetActive(isActive);
        }
    }

    private void SetHandleBlink(bool shouldBlink)
    {
        if (shouldBlink)
        {
            if (_handleBlinkCoroutine == null && _sliderHandleGraphic != null)
            {
                _handleBlinkCoroutine = StartCoroutine(BlinkHandle());
            }

            return;
        }

        StopHandleBlink();
    }

    private IEnumerator BlinkHandle()
    {
        while (true)
        {
            _sliderHandleGraphic.color = Color.Lerp(_sliderHandleBaseColor, Color.black, HandleDarkenAmount);
            yield return _handleDarkWait;

            _sliderHandleGraphic.color = _sliderHandleBaseColor;
            yield return _handleBrightWait;
        }
    }

    private void StopHandleBlink()
    {
        if (_handleBlinkCoroutine != null)
        {
            StopCoroutine(_handleBlinkCoroutine);
            _handleBlinkCoroutine = null;
        }

        if (_sliderHandleGraphic != null)
        {
            _sliderHandleGraphic.color = _sliderHandleBaseColor;
        }
    }
}
