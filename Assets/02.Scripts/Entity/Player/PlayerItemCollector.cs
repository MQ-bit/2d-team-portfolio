using UnityEngine;

public class PlayerItemCollector : MonoBehaviour
{
    [SerializeField] private PlayerInventoryModule _inventoryModule;
    [SerializeField] private Transform _collectTarget;

    private CircleCollider2D _collectArea;
    private PlayerStatus _status;

    private void Awake()
    {
        _collectArea = GetComponent<CircleCollider2D>();
        _status = GetComponentInParent<PlayerStatus>();
        if (_status == null || _status.Data == null || _collectArea == null)
        {
            Debug.LogError($"{name}: 수집 센서에 PlayerStatus/PlayerData/CircleCollider2D가 필요합니다.", this);
            enabled = false;
            return;
        }
        _collectArea.radius = _status.Data.Collection.LocalRadius;

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
