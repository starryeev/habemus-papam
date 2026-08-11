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
    private const string GrayscaleShaderName = "UI/Grayscale";
    private const string GrayscaleShaderResourcePath = "UI/Shaders/UIGrayscale";

    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    private readonly Vector3[] _worldCorners = new Vector3[4];
    private Graphic[] _sliderGraphics;
    private Material[] _sliderOriginalMaterials;
    private Material _grayscaleMaterial;
    private Graphic _sliderHandleGraphic;
    private Color _sliderHandleBaseColor;
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
    }

    private void OnDisable()
    {
        StopHandleBlink();
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

    public void SetEditingVisual(bool isEditing)
    {
        SetHandleBlink(isEditing && CanEditVolume);
    }

    public bool TryGetSelectionBounds(
        VolumeControlTarget target,
        RectTransform relativeTo,
        out Bounds bounds)
    {
        if (relativeTo == null)
        {
            bounds = default;
            return false;
        }

        if (target == VolumeControlTarget.Mute)
        {
            RectTransform muteRect = muteToggle != null ? muteToggle.transform as RectTransform : null;
            return TryGetRectBounds(muteRect, relativeTo, out bounds);
        }

        return TryGetSliderSelectionBounds(relativeTo, out bounds);
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

    private static void SetCanvasGroupState(GameObject target, bool isMuted)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = isMuted ? DisabledAlpha : EnabledAlpha;
        canvasGroup.interactable = !isMuted;
        canvasGroup.blocksRaycasts = !isMuted;
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

    private bool TryGetSliderSelectionBounds(RectTransform relativeTo, out Bounds bounds)
    {
        RectTransform rowRect = transform as RectTransform;
        if (rowRect == null)
        {
            bounds = default;
            return false;
        }

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

            childRect.GetWorldCorners(_worldCorners);
            for (int cornerIndex = 0; cornerIndex < _worldCorners.Length; cornerIndex++)
            {
                Vector3 localCorner = relativeTo.InverseTransformPoint(_worldCorners[cornerIndex]);
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

    private bool TryGetRectBounds(RectTransform target, RectTransform relativeTo, out Bounds bounds)
    {
        if (target == null)
        {
            bounds = default;
            return false;
        }

        target.GetWorldCorners(_worldCorners);
        Vector3 minimum = relativeTo.InverseTransformPoint(_worldCorners[0]);
        Vector3 maximum = minimum;
        for (int index = 1; index < _worldCorners.Length; index++)
        {
            Vector3 corner = relativeTo.InverseTransformPoint(_worldCorners[index]);
            minimum = Vector3.Min(minimum, corner);
            maximum = Vector3.Max(maximum, corner);
        }

        bounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
        return true;
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
