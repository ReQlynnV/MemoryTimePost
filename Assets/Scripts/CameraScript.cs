
using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Camera))]
public class CameraScript : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private PolygonCollider2D boundary;

    private Camera targetCamera;
    private SpriteRenderer targetSprite;
    private bool warnedAboutMissingTarget;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ResolveTarget();

        if (boundary == null)
        {
            boundary = FindFirstObjectByType<PolygonCollider2D>();
        }

        if (boundary == null)
        {
            Debug.LogWarning(
                "CameraScript needs a PolygonCollider2D boundary to confine the camera.",
                this);
        }
    }

    private void LateUpdate()
    {
        if (!ResolveTarget())
        {
            return;
        }

        Vector3 targetPosition = targetSprite != null
            ? targetSprite.bounds.center
            : target.position;

        transform.position = targetPosition + offset;

        if (boundary != null)
        {
            ConfineToBoundary();
        }
    }

    private bool ResolveTarget()
    {
        if (target == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    target = playerObject.transform;
                }
            }
        }

        if (target == null)
        {
            if (!warnedAboutMissingTarget)
            {
                Debug.LogWarning(
                    "CameraScript cannot follow because no target is assigned or Player object is available.",
                    this);
                warnedAboutMissingTarget = true;
            }

            return false;
        }

        warnedAboutMissingTarget = false;
        if (targetSprite == null || targetSprite.transform != target)
        {
            targetSprite = target.GetComponent<SpriteRenderer>();
            if (targetSprite == null)
            {
                targetSprite = target.GetComponentInChildren<SpriteRenderer>();
            }
        }

        return true;
    }

    private void ConfineToBoundary()
    {
        if (!TryGetViewportCornerOffsets(out Vector2[] cornerOffsets))
        {
            Debug.LogWarning(
                "The camera view does not intersect the PolygonCollider2D boundary plane.",
                this);
            return;
        }

        Vector3 bestPosition = transform.position;
        Vector3 candidatePosition = bestPosition;
        float bestError = GetBoundaryError(candidatePosition, cornerOffsets);

        for (int iteration = 0; iteration < 16; iteration++)
        {
            Vector2 correction = Vector2.zero;
            int outsideCornerCount = 0;

            foreach (Vector2 cornerOffset in cornerOffsets)
            {
                Vector2 corner = (Vector2)candidatePosition + cornerOffset;
                if (boundary.OverlapPoint(corner))
                {
                    continue;
                }

                correction += boundary.ClosestPoint(corner) - corner;
                outsideCornerCount++;
            }

            if (outsideCornerCount == 0)
            {
                transform.position = candidatePosition;
                return;
            }

            correction /= outsideCornerCount;
            if (correction.sqrMagnitude < 0.000001f)
            {
                break;
            }

            candidatePosition += new Vector3(correction.x, correction.y, 0f);

            float error = GetBoundaryError(candidatePosition, cornerOffsets);
            if (error < bestError)
            {
                bestError = error;
                bestPosition = candidatePosition;
            }
        }

        transform.position = bestPosition;
    }

    private bool TryGetViewportCornerOffsets(out Vector2[] cornerOffsets)
    {
        cornerOffsets = new Vector2[4];
        Plane boundaryPlane = new(
            Vector3.forward,
            new Vector3(0f, 0f, boundary.bounds.center.z));
        Vector2[] viewportCorners =
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f)
        };

        for (int i = 0; i < viewportCorners.Length; i++)
        {
            Vector2 viewportCorner = viewportCorners[i];
            Ray ray = targetCamera.ViewportPointToRay(
                new Vector3(viewportCorner.x, viewportCorner.y, 0f));
            if (!boundaryPlane.Raycast(ray, out float distance))
            {
                return false;
            }

            cornerOffsets[i] = ray.GetPoint(distance) - transform.position;
        }

        return true;
    }

    private float GetBoundaryError(Vector3 cameraPosition, Vector2[] cornerOffsets)
    {
        float error = 0f;
        foreach (Vector2 cornerOffset in cornerOffsets)
        {
            Vector2 corner = (Vector2)cameraPosition + cornerOffset;
            if (!boundary.OverlapPoint(corner))
            {
                Vector2 difference = boundary.ClosestPoint(corner) - corner;
                error += difference.sqrMagnitude;
            }
        }

        return error;
    }
}
