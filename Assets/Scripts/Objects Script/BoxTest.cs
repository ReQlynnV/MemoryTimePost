using UnityEngine;

public class BoxTest : UnityEngine.MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform dropPoint;

    private bool hasDroppedItem;

    private void Awake()
    {
        if (itemPrefab == null)
        {
            Debug.LogError("BoxTest requires an item prefab to drop.", this);
        }
    }

    public void interact()
    {
        if (!isInteractable())
        {
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Instantiate(itemPrefab, spawnPosition, itemPrefab.transform.rotation);
        hasDroppedItem = true;
    }

    public bool isInteractable()
    {
        return !hasDroppedItem && itemPrefab != null;
    }

    private Vector3 GetSpawnPosition()
    {
        if (dropPoint != null)
        {
            return dropPoint.position;
        }

        SpriteRenderer itemRenderer = itemPrefab.GetComponentInChildren<SpriteRenderer>();
        float itemHalfHeight = itemRenderer != null ? itemRenderer.bounds.extents.y : 0f;
        Collider2D boxCollider = GetComponent<Collider2D>();
        if (boxCollider == null)
        {
            return transform.position + Vector3.down * (1f + itemHalfHeight);
        }

        Bounds boxBounds = boxCollider.bounds;
        return new Vector3(boxBounds.center.x, boxBounds.min.y - itemHalfHeight, transform.position.z);
    }
}
