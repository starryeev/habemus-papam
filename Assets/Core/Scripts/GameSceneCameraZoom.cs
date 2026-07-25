using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public sealed class GameSceneCameraZoom : MonoBehaviour
{
    public const float MinZoomSize = 5.4f;
    public const float MaxZoomSize = 10.55f;
    public const float FullUiAlphaSize = 9f;

    [SerializeField] private float zoomSizePerScrollUnit = 0.5f;

    private Camera targetCamera;
    private CanvasGroup uiCanvasGroup;

    public static void Attach(Camera camera, CanvasGroup uiGroup)
    {
        if (camera == null)
        {
            return;
        }

        GameSceneCameraZoom zoom = camera.GetComponent<GameSceneCameraZoom>();
        if (zoom == null)
        {
            zoom = camera.gameObject.AddComponent<GameSceneCameraZoom>();
        }

        zoom.Configure(uiGroup);
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (!Mathf.Approximately(scrollDelta, 0f))
        {
            // Scroll up reduces orthographic size (zoom in); scroll down increases it.
            targetCamera.orthographicSize = Mathf.Clamp(
                targetCamera.orthographicSize - scrollDelta * zoomSizePerScrollUnit,
                MinZoomSize,
                MaxZoomSize);
        }

        UpdateUiAlpha();
    }

    private void Configure(CanvasGroup group)
    {
        uiCanvasGroup = group;
        UpdateUiAlpha();
    }

    private void UpdateUiAlpha()
    {
        if (targetCamera == null || uiCanvasGroup == null)
        {
            return;
        }

        // 9.0~10.55: fully visible. 5.4~9.0: linearly fades to transparent.
        uiCanvasGroup.alpha = Mathf.InverseLerp(MinZoomSize, FullUiAlphaSize, targetCamera.orthographicSize);
    }
}
