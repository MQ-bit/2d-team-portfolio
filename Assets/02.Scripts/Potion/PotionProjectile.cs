using System.Collections.Generic;
using UnityEngine;

public class PotionProjectile : MonoBehaviour  //  장판 생성 또는 기화로 장판 제거 기능 추가
{
    [SerializeField] private Transform _visual;
    [SerializeField] private PotionBreakEffect _breakEffectPrefab;
    [SerializeField] private PotionGroundArea _groundAreaPrefab;
    [SerializeField] private SpriteRenderer _renderer;

    private PotionItem _itemData;
    private PotionRunCatalog _catalog;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _flightDuration;
    private float _rotationSpeed;
    private float _elapsedTime;
    private float _verticalVelocity;
    private float _gravity;
    private PotionData _potionData => _itemData.PotionData;
    private readonly List<Collider2D> _areaHits = new();

    private void Awake()
    {
        _renderer = GetComponentInChildren<SpriteRenderer>();
        enabled = false;
    }

    private void Update()
    {
        _elapsedTime = Mathf.Min(_elapsedTime + Time.deltaTime, _flightDuration);
        float progress = _elapsedTime / _flightDuration;
        float virtualHeight = Mathf.Max(0f, _verticalVelocity * _elapsedTime - 0.5f * _gravity * _elapsedTime * _elapsedTime);

        transform.position = Vector3.Lerp(_startPosition, _targetPosition, progress);
        if (_visual != null)
        {
            _visual.localPosition = Vector3.up * virtualHeight;
            _visual.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
        }

        if (_elapsedTime >= _flightDuration)
        {
            Land();
        }
    }
    public void Initiate(PotionItem potionItem, PotionRunCatalog catalog, IDamageable owner)
    {
        _itemData = potionItem;
        _catalog = catalog;
        _renderer.sprite = potionItem.BottleSprite;
    }

    public void Launch(Vector3 targetPosition, float flightDuration, float arcHeight, float rotationSpeed)
    {
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _flightDuration = Mathf.Max(0.01f, flightDuration);
        _rotationSpeed = rotationSpeed;
        _elapsedTime = 0f;

        float height = Mathf.Max(0f, arcHeight);
        _gravity = 8f * height / (_flightDuration * _flightDuration);
        _verticalVelocity = 0.5f * _gravity * _flightDuration;

        enabled = true;
    }

    private void Land()
    {
        enabled = false;
        transform.position = _targetPosition;

        if (_breakEffectPrefab != null)
        {
            PotionBreakEffect breakEffect = Instantiate(_breakEffectPrefab, _targetPosition, Quaternion.identity);
            breakEffect.Play();
        }

        if (_potionData != null && _potionData.Element == PotionElement.Vaporization)
        {
            RemoveGroundAreas();
        }
        else if (_groundAreaPrefab != null)
        {
            PotionGroundArea groundArea = Instantiate(_groundAreaPrefab, _targetPosition, Quaternion.identity);
            groundArea.Initialize(_potionData);
            groundArea.Activate();
        }

        if (_catalog != null)
        {
            _catalog.TryIdentify(_itemData);
        }
        Destroy(gameObject);
    }

    private void RemoveGroundAreas()
    {
        float radius = _potionData.AreaWidth * 0.5f;
        if (radius <= 0f) return;

        // Include trigger colliders regardless of the global queriesHitTriggers setting.
        ContactFilter2D filter = new() { useTriggers = true };
        Physics2D.OverlapCircle(_targetPosition, radius, filter, _areaHits);

        foreach (Collider2D hit in _areaHits)
        {
            PotionGroundArea area = hit.GetComponentInParent<PotionGroundArea>();
            if (area == null || !area.gameObject.activeSelf) continue;

            // Stop collisions and periodic effects before deferred destruction.
            area.gameObject.SetActive(false);
            Destroy(area.gameObject);
        }

        _areaHits.Clear();
    }
}
