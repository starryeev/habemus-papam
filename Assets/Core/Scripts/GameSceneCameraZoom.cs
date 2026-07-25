using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public sealed class GameSceneCameraZoom : MonoBehaviour
{
    public const float MinZoomSize = 5.4f;
    public const float MaxZoomSize = 10.55f;
    public const float FullUiAlphaSize = 9f;

    [SerializeField] private float zoomSizePerScrollUnit = 0.5f;
    [SerializeField] private float followSmoothTime = 0.2f;

    private static GameSceneCameraZoom activeInstance;
    private Camera targetCamera;
    private SpriteRenderer cameraBorderRenderer;
    private CanvasGroup uiCanvasGroup;
    private Transform playerTarget;
    private Vector3 followVelocity;
    private Vector3 initialCameraPosition;

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
        activeInstance = this;
        targetCamera = GetComponent<Camera>();
        initialCameraPosition = transform.position;
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
        }
    }

    private void Update()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (!Mathf.Approximately(scrollDelta, 0f))
        {
            if (scrollDelta > 0f && !TryFindPlayerTarget())
            {
                return;
            }

            // Scroll up reduces orthographic size (zoom in); scroll down increases it.
            targetCamera.orthographicSize = Mathf.Clamp(
                targetCamera.orthographicSize - scrollDelta * zoomSizePerScrollUnit,
                MinZoomSize,
                MaxZoomSize);
        }

        UpdateUiAlpha();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (targetCamera.orthographicSize <= FullUiAlphaSize && TryFindPlayerTarget())
        {
            Vector3 destination = playerTarget.position;
            destination.z = transform.position.z;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                destination,
                ref followVelocity,
                followSmoothTime);
        }
        else
        {
            StopFollowing();
        }

        ClampPositionToCameraBorder();
    }

    public void ReleaseZoom()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographicSize = MaxZoomSize;
        UpdateUiAlpha();
    }

    public void StopFollowing()
    {
        playerTarget = null;
        followVelocity = Vector3.zero;
    }

    public void ReleaseZoomAndFollow()
    {
        ReleaseZoom();
        StopFollowing();
        transform.position = initialCameraPosition;
        ClampPositionToCameraBorder();
    }

    public static void ReleaseActiveZoomAndFollow()
    {
        if (activeInstance != null)
        {
            activeInstance.ReleaseZoomAndFollow();
        }
    }

    private void Configure(CanvasGroup group)
    {
        uiCanvasGroup = group;
        UpdateUiAlpha();
    }

    private bool TryFindPlayerTarget()
    {
        playerTarget = CardinalManager.Instance != null
            ? CardinalManager.Instance.PlayerTransform
            : null;

        return playerTarget != null && playerTarget.gameObject.activeInHierarchy;
    }

    private void ClampPositionToCameraBorder()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (cameraBorderRenderer == null)
        {
            GameObject cameraBorder = GameObject.Find("CameraBorder");
            if (cameraBorder != null)
            {
                cameraBorderRenderer = cameraBorder.GetComponent<SpriteRenderer>();
            }
        }

        if (cameraBorderRenderer == null)
        {
            return;
        }

        Bounds borderBounds = cameraBorderRenderer.bounds;
        float halfHeight = targetCamera.orthographicSize;
        float halfWidth = halfHeight * targetCamera.aspect;
        float minX = borderBounds.min.x + halfWidth;
        float maxX = borderBounds.max.x - halfWidth;
        float minY = borderBounds.min.y + halfHeight;
        float maxY = borderBounds.max.y - halfHeight;

        Vector3 clampedPosition = transform.position;
        clampedPosition.x = minX > maxX
            ? borderBounds.center.x
            : Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.y = minY > maxY
            ? borderBounds.center.y
            : Mathf.Clamp(clampedPosition.y, minY, maxY);
        transform.position = clampedPosition;
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
