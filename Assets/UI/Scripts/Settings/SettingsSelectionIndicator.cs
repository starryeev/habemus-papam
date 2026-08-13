using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsSelectionIndicator
{
    private const float ArrowSpacing = 8f;

    private readonly RectTransform _layer;
    private readonly RectTransform _leftArrow;
    private readonly RectTransform _rightArrow;
    private readonly Vector3[] _worldCorners = new Vector3[4];

    public SettingsSelectionIndicator(
        RectTransform layer,
        Sprite arrowSprite,
        Image leftArrow,
        Image rightArrow)
    {
        _layer = layer;
        _leftArrow = ConfigureArrow(leftArrow, arrowSprite, false);
        _rightArrow = ConfigureArrow(rightArrow, arrowSprite, true);
        Hide();
    }

    public void Show(RectTransform target, bool shouldUpdatePosition)
    {
        if (target == null || !TryGetBounds(target, out Bounds bounds))
        {
            Hide();
            return;
        }

        Show(bounds, shouldUpdatePosition);
    }

    public void Show(
        VolumeSet volumeSet,
        VolumeControlTarget target,
        bool shouldUpdatePosition)
    {
        if (volumeSet == null ||
            !volumeSet.TryGetSelectionBounds(target, _layer, out Bounds bounds))
        {
            Hide();
            return;
        }

        Show(bounds, shouldUpdatePosition);
    }

    public void Hide()
    {
        SetActive(_leftArrow, false);
        SetActive(_rightArrow, false);
    }

    private void Show(Bounds bounds, bool shouldUpdatePosition)
    {
        if (_leftArrow == null || _rightArrow == null)
        {
            return;
        }

        if (shouldUpdatePosition)
        {
            float leftQuarterWidth = _leftArrow.rect.width * 0.25f;
            float rightQuarterWidth = _rightArrow.rect.width * 0.25f;
            _leftArrow.anchoredPosition = new Vector2(
                bounds.min.x - ArrowSpacing - leftQuarterWidth,
                bounds.center.y);
            _rightArrow.anchoredPosition = new Vector2(
                bounds.max.x + ArrowSpacing + rightQuarterWidth,
                bounds.center.y);
        }

        SetActive(_leftArrow, true);
        SetActive(_rightArrow, true);
        _leftArrow.SetAsLastSibling();
        _rightArrow.SetAsLastSibling();
    }

    private static RectTransform ConfigureArrow(Image arrowImage, Sprite arrowSprite, bool isRightSide)
    {
        if (arrowImage == null)
        {
            return null;
        }

        if (arrowSprite != null)
        {
            arrowImage.sprite = arrowSprite;
        }

        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;
        RectTransform arrowRect = arrowImage.rectTransform;
        arrowRect.localRotation = Quaternion.identity;
        Vector3 localScale = arrowRect.localScale;
        localScale.x = Mathf.Abs(localScale.x) * (isRightSide ? -1f : 1f);
        arrowRect.localScale = localScale;
        return arrowRect;
    }

    private bool TryGetBounds(RectTransform target, out Bounds bounds)
    {
        if (_layer == null)
        {
            bounds = default;
            return false;
        }

        target.GetWorldCorners(_worldCorners);
        Vector3 minimum = _layer.InverseTransformPoint(_worldCorners[0]);
        Vector3 maximum = minimum;
        for (int index = 1; index < _worldCorners.Length; index++)
        {
            Vector3 corner = _layer.InverseTransformPoint(_worldCorners[index]);
            minimum = Vector3.Min(minimum, corner);
            maximum = Vector3.Max(maximum, corner);
        }

        bounds = new Bounds((minimum + maximum) * 0.5f, maximum - minimum);
        return true;
    }

    private static void SetActive(Component target, bool isActive)
    {
        if (target != null && target.gameObject.activeSelf != isActive)
        {
            target.gameObject.SetActive(isActive);
        }
    }
}
