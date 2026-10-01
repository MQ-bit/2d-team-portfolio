using UnityEngine;

public class PotionProjectile : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private PotionBreakEffect _breakEffectPrefab;
    [SerializeField] private PotionGroundArea _groundAreaPrefab;
    [SerializeField] private SpriteRenderer _renderer;

    [Header("Wall Collision")]
    [Tooltip("비워두면 Tilemap_Wall의 Collider만 검사한다. 지정하면 해당 레이어의 일반 Collider도 벽으로 처리한다.")]
    [SerializeField] private LayerMask _wallCollisionLayers;
    [Tooltip("벽 충돌면에서 바깥 방향으로 장판 중심을 띄우는 간격")]
    [SerializeField, Min(0.01f)] private float _wallGroundClearance = 0.15f;

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

        if (_groundAreaPrefab != null && canCreateArea)
        {
            PotionGroundArea groundArea = Instantiate(_groundAreaPrefab, areaPosition, Quaternion.identity);
            groundArea.Initialize(_potionData);
            groundArea.SetOwner(_owner);
            groundArea.Activate();
        }

        if (_catalog != null)
        {
            _catalog.TryIdentify(_itemData);
        }
        Destroy(gameObject);
    }
}
