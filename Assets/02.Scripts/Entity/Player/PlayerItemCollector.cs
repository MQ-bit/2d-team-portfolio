using UnityEngine;
using Entity.Inventory;

public class PlayerItemCollector : MonoBehaviour
{
    [SerializeField] private PlayerInventoryModule _inventoryModule;
    [SerializeField] private Transform _collectTarget;

    private CircleCollider2D _collectArea;

    private void Awake()
    {
        _collectArea = GetComponent<CircleCollider2D>();

        if (_inventoryModule == null)
        {
            _inventoryModule = GetComponentInParent<PlayerInventoryModule>();
        }

        if (_collectTarget == null)
        {
            _collectTarget = (_inventoryModule != null) ? _inventoryModule.transform : transform.parent;
        }

        if (_inventoryModule == null)
        {
            Debug.LogError($"{gameObject.name} : PlayerInventoryModule을 찾을 수 없음.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        if (_inventoryModule == null || _collectTarget == null)
        {
            return;
        }

        IItemCollectable collectable = other.GetComponentInParent<IItemCollectable>();

        if (collectable == null)
        {
            return;
        }

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        float worldCollectRadius = _collectArea.radius * scale;

        collectable.TryCollect(_inventoryModule, _collectTarget, worldCollectRadius);
    }
}