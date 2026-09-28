using System.Collections.Generic;
using UnityEngine;

public sealed class PotionProjectile : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private PotionBreakEffect _breakEffectPrefab;
    [SerializeField] private PotionGroundArea _groundAreaPrefab;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _flightDuration;
    private float _rotationSpeed;
    private float _elapsedTime;
    private float _verticalVelocity;
    private float _gravity;
    private PotionData _potionData;
    private SpriteRenderer _visualRenderer;

    private void Awake()
    {
        if (_visual != null)
        {
            _visualRenderer = _visual.GetComponent<SpriteRenderer>();
        }

        enabled = false;
    }

    public void Launch(PotionData potionData, Vector3 targetPosition, float flightDuration, float arcHeight, float rotationSpeed)
    {
        if (potionData == null)
        {
            Debug.LogError("PotionProjectile cannot launch without PotionData.", this);
            Destroy(gameObject);
            return;
        }

        _potionData = potionData;
        _startPosition = transform.position;
        _targetPosition = targetPosition;
        _flightDuration = Mathf.Max(0.01f, flightDuration);
        _rotationSpeed = rotationSpeed;
        _elapsedTime = 0f;

        float height = Mathf.Max(0f, arcHeight);
        _gravity = 8f * height / (_flightDuration * _flightDuration);
        _verticalVelocity = 0.5f * _gravity * _flightDuration;

        if (_visualRenderer != null && potionData.VisualData.PotionSprite != null)
        {
            _visualRenderer.sprite = potionData.VisualData.PotionSprite;
        }

        enabled = true;
    }

    private void Update()
    {
        _elapsedTime = Mathf.Min(_elapsedTime + Time.deltaTime, _flightDuration);
        float progress = _elapsedTime / _flightDuration;
        float virtualHeight = Mathf.Max(
            0f,
            _verticalVelocity * _elapsedTime - 0.5f * _gravity * _elapsedTime * _elapsedTime);

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

    private void Land()
    {
        enabled = false;
        transform.position = _targetPosition;

        PlayBreakEffect();

        if (_potionData.Element == PotionElement.Vaporization)
        {
            RemoveGroundAreas();
        }
        else if (_potionData.CreatesGroundArea)
        {
            CreateGroundArea();
        }
        else
        {
            ApplyInstantAreaEffect();
        }

        Destroy(gameObject);
    }

    private void PlayBreakEffect()
    {
        if (_breakEffectPrefab == null)
        {
            return;
        }

        PotionBreakEffect breakEffect = Instantiate(_breakEffectPrefab, _targetPosition, Quaternion.identity);
        breakEffect.Play();
    }

    private void CreateGroundArea()
    {
        if (_groundAreaPrefab == null)
        {
            Debug.LogError("PotionProjectile requires a ground area prefab.", this);
            return;
        }

        PotionGroundArea groundArea = Instantiate(_groundAreaPrefab, _targetPosition, Quaternion.identity);
        groundArea.Initialize(_potionData);
        groundArea.Activate();
    }

    private void ApplyInstantAreaEffect()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_targetPosition, _potionData.AreaRadius);
        HashSet<PotionEffectReceiver> affectedTargets = new();

        for (int i = 0; i < hits.Length; i++)
        {
            if (PotionEffectResolver.TryFindTarget(hits[i], out PotionEffectReceiver target) &&
                affectedTargets.Add(target))
            {
                PotionEffectResolver.ApplyAreaEffect(_potionData, target);
            }
        }
    }

    private void RemoveGroundAreas()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(_targetPosition, _potionData.AreaRadius);
        HashSet<PotionGroundArea> groundAreas = new();

        for (int i = 0; i < hits.Length; i++)
        {
            PotionGroundArea groundArea = hits[i].GetComponentInParent<PotionGroundArea>();
            if (groundArea != null && groundAreas.Add(groundArea))
            {
                Destroy(groundArea.gameObject);
            }
        }
    }
}
