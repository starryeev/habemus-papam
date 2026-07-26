using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WorldActionButtonRaycastTarget : MonoBehaviour
{
    private static readonly HashSet<Collider2D> ActiveTargets = new HashSet<Collider2D>();

    private Collider2D targetCollider;

    private void OnEnable()
    {
        if (targetCollider == null)
        {
            targetCollider = GetComponent<Collider2D>();
        }

        if (targetCollider != null)
        {
            ActiveTargets.Add(targetCollider);
        }
    }

    private void OnDisable()
    {
        if (targetCollider != null)
        {
            ActiveTargets.Remove(targetCollider);
        }
    }

    public static BoxCollider2D Configure(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        WorldActionButtonRaycastTarget raycastTarget = target.GetComponent<WorldActionButtonRaycastTarget>();
        if (raycastTarget == null)
        {
            raycastTarget = target.AddComponent<WorldActionButtonRaycastTarget>();
        }

        BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = target.AddComponent<BoxCollider2D>();
        }

        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.sprite != null)
        {
            collider.offset = renderer.sprite.bounds.center;
            collider.size = renderer.sprite.bounds.size;
        }

        collider.isTrigger = true;
        raycastTarget.targetCollider = collider;
        ActiveTargets.Add(collider);
        return collider;
    }

    public static bool IsPointerOverAnyTarget(Camera camera, Vector2 screenPosition)
    {
        if (camera == null)
        {
            return false;
        }

        Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(
            screenPosition.x,
            screenPosition.y,
            -camera.transform.position.z));
        Vector2 point = new Vector2(worldPosition.x, worldPosition.y);

        foreach (Collider2D collider in ActiveTargets)
        {
            if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy && collider.OverlapPoint(point))
            {
                return true;
            }
        }

        return false;
    }
}
