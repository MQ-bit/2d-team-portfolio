using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PotionProjectile : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private PotionBreakEffect _breakEffectPrefab;
    [SerializeField] private PotionAreaController _areaControllerPrefab;
    [SerializeField] private SpriteRenderer _renderer;

    [Header("Wall Collision")]
    [Tooltip("비워두면 Tilemap_Wall의 Collider만 검사한다. 지정하면 해당 레이어의 일반 Collider도 벽으로 처리한다.")]
    [SerializeField] private LayerMask _wallCollisionLayers;
    [Tooltip("벽 충돌면에서 바깥 방향으로 장판 중심을 띄우는 간격")]
    [SerializeField, Min(0.01f)] private float _wallGroundClearance = 0.15f;

    [Header("Impact Feedback")]
    [Tooltip("포션이 깨질 때마다 던진 방향으로 주는 화면 흔들림")]
    [SerializeField, Min(0f)] private float _impactShakeForce = 0.1f;
    [Tooltip("효과 범위 안에 적이 있을 때 대신 주는 화면 흔들림")]
    [SerializeField, Min(0f)] private float _enemyHitShakeForce = 0.25f;
    [Tooltip("효과 범위 안에 적이 있을 때 주는 히트스톱(실제 시간, 초)")]
    [SerializeField, Min(0f)] private float _enemyHitStopDuration = 0.06f;

    private static readonly List<Collider2D> OverlapResults = new();

    private readonly PotionWallCollision _wallCollision = new();
    private float _arcHeight;

    private PotionItem _itemData;
    private PotionRunCatalog _catalog;
    private IDamageable _owner;

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _flightDuration;
    private float _rotationSpeed;
    private float _elapsedTime;
    private float _verticalVelocity;
    private float _gravity;
    private PotionData _potionData => _itemData.PotionData;

    private void Awake()
    {
        // The root also contains a shadow renderer; tint/change the actual bottle.
        _renderer = _visual != null ? _visual.GetComponentInChildren<SpriteRenderer>()
            : GetComponentInChildren<SpriteRenderer>();
        enabled = false;
    }

    private void Update()
    {
        float previousProgress = _elapsedTime / _flightDuration;
        _elapsedTime = Mathf.Min(_elapsedTime + Time.deltaTime, _flightDuration);
        float progress = _elapsedTime / _flightDuration;
        float virtualHeight = Mathf.Max(0f, _verticalVelocity * _elapsedTime - 0.5f * _gravity * _elapsedTime * _elapsedTime);

        Vector3 nextPosition = Vector3.Lerp(_startPosition, _targetPosition, progress);
        if (_wallCollision.TryTrace(_startPosition, _targetPosition, _arcHeight, previousProgress,
            progress, _wallCollisionLayers, _wallGroundClearance, out PotionThrowPrediction wallImpact))
        {
            Land(wallImpact.ImpactPosition, wallImpact.AreaPosition, wallImpact.CanCreateArea);
            return;
        }

        transform.position = nextPosition;
        if (_visual != null)
        {
            _visual.position = nextPosition + Vector3.up * virtualHeight;
            _visual.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
        }

        if (_elapsedTime >= _flightDuration)
        {
            Land();
        }
    }

    // The aim preview uses this prefab's wall settings and the same query as flight.
    public float PredictFlightProgress(Vector2 start, Vector2 target)
    {
        return PredictThrow(start, target, 0f).FlightProgress;
    }

    public PotionThrowPrediction PredictThrow(Vector3 start, Vector3 target, float arcHeight)
    {
        _wallCollision.TryTrace(start, target, arcHeight, 0f, 1f, _wallCollisionLayers,
            _wallGroundClearance, out PotionThrowPrediction prediction);
        return prediction;
    }

    public void Initiate(PotionItem potionItem, PotionRunCatalog catalog, IDamageable owner)
    {
        _itemData = potionItem;
        _catalog = catalog;
        _owner = owner;
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
        _arcHeight = height;
        _gravity = 8f * height / (_flightDuration * _flightDuration);
        _verticalVelocity = 0.5f * _gravity * _flightDuration;

        enabled = true;
    }

    private void Land()
    {
        Land(_targetPosition, _targetPosition, true);
    }

    private void Land(Vector3 impactPosition, Vector3 areaPosition, bool canCreateArea)
    {
        enabled = false;
        transform.position = impactPosition;
        // Root movement alone retains the previous frame's arc offset on the child.
        // Set the visible bottle explicitly so it ends at the predicted collision.
        if (_visual != null) _visual.position = impactPosition;

        if (_breakEffectPrefab != null)
        {
            PotionBreakEffect breakEffect = Instantiate(_breakEffectPrefab, impactPosition, Quaternion.identity);
            breakEffect.Play();
        }

        if (_areaControllerPrefab != null && canCreateArea)
        {
            PotionAreaController areaController = Instantiate(_areaControllerPrefab, areaPosition, Quaternion.identity);
            areaController.Initialize(_potionData);
            areaController.SetOwner(_owner);
            areaController.Activate();
        }

        PlayImpactFeedback(areaPosition, canCreateArea);

        if (_catalog != null)
        {
            _catalog.TryIdentify(_itemData);
        }
        Destroy(gameObject);
    }

    private void PlayImpactFeedback(Vector3 areaPosition, bool canCreateArea)
    {
        bool hasEnemyInArea = canCreateArea && HasEnemyInArea(areaPosition);

        if (hasEnemyInArea)
        {
            HitStop.Request(_enemyHitStopDuration);
        }

        if (CameraController.Instance != null)
        {
            Vector2 throwDirection = _targetPosition - _startPosition;
            CameraController.Instance.ShakeCamera(throwDirection, hasEnemyInArea ? _enemyHitShakeForce : _impactShakeForce);
        }
    }

    // 장판이 깔릴 범위 안에 살아있는 적이 있는지. 실제 피해 판정은 장판이 하므로 연출용 근사치다.
    private bool HasEnemyInArea(Vector2 center)
    {
        if (_potionData == null || _potionData.TargetType == PotionTargetType.Player)
        {
            return false;
        }

        Physics2D.OverlapCircle(center, _potionData.EffectRadius, ContactFilter2D.noFilter, OverlapResults);
        foreach (Collider2D hit in OverlapResults)
        {
            EnemyStatus enemy = hit.GetComponentInParent<EnemyStatus>();
            if (enemy != null && !enemy.IsDead)
            {
                return true;
            }
        }

        return false;
    }
}
