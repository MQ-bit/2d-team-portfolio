using System.Collections;
using UnityEngine;

public sealed class PotionFieldItem : MonoBehaviour, IItemCollectable
{
    [SerializeField] private PotionRunCatalog _potionRunCatalog;
    [Header("Component References")]
    [SerializeField] private SpriteRenderer _renderer;

    [Header("Item Setting")]
    [SerializeField] private PotionData _potionData;
    [SerializeField] private int _quantity = 1;

    [Header("Collect Movement Settings")]
    [SerializeField] private float _flightDuration = 0.45f;
    [SerializeField] private float _arcHeight = 1f;
    [SerializeField] private AnimationCurve _flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Failed Collection Setting")]
    [SerializeField] private float _failedDropPadding = 0.5f;

    [SerializeField] private float _failedDropDuration = 0.3f;

    [SerializeField] private float _failedDropArcHeight = 0.75f;
    [SerializeField] private float _reactivateDelay = 1f;

    [Header("Drop Wall Collision")]
    [Tooltip("비워두면 Wall 레이어를 검사하고, Wall이 없으면 Tilemap_Wall을 검사한다.")]
    [SerializeField] private LayerMask _dropWallLayers;
    [SerializeField, Min(0.01f)] private float _dropWallPadding = 0.05f;
    [SerializeField, Min(0.01f)] private float _wallLandingDuration = 0.2f;

    [Header("Visual Effects")]
    [SerializeField] private TrailRenderer _trailRenderer;
    [SerializeField] private ParticleSystem _touchedParticlePrefab;

    private readonly Entity.Inventory.PotionWallCollision _wallCollision = new();

    private PotionItem _nowItemData;

    private WaitForSeconds _waitForReactivate;
    private Collider2D _collider;
    private Coroutine _collectionRoutine;
    private Coroutine _dropRoutine;
    private Vector3 _dropTarget;
    private bool _isDropping;

    private Vector3 _originalPosition;
    private bool _isCollecting;
    private bool _isBeingDestroyed;

    private void Awake()
    {
        _collider = GetComponentInChildren<Collider2D>();
        // 기존 씬에 직접 배치된 포션도 획득만 감지하고 이동을 막지 않는다.
        if (_collider != null) _collider.isTrigger = true;
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _trailRenderer = GetComponentInChildren<TrailRenderer>();
        SetAirborneSorting(false);
        _waitForReactivate = new WaitForSeconds(_reactivateDelay);
    }
    private void OnDisable()
    {
        if (_isBeingDestroyed)
        {
            return;
        }

        if (_dropRoutine != null)
        {
            StopCoroutine(_dropRoutine);
            _dropRoutine = null;
        }

        if (_isDropping)
        {
            transform.position = _dropTarget;
            _isDropping = false;
        }

        if (_collectionRoutine != null)
        {
            StopCoroutine(_collectionRoutine);
            _collectionRoutine = null;
        }

        if (_isCollecting)
        {
            transform.position = _originalPosition;
        }
        if (_trailRenderer != null)
        {
            _trailRenderer.enabled = false;
        }

        _isCollecting = false;
        SetAirborneSorting(false);
        SetCollidersEnabled(true);
    }

    private void Start()
    {
        if (_potionRunCatalog == null)
        {
            Debug.LogError($"{gameObject.name} : PotionRunCatalog is not assgined!");
            enabled = false;
            return;
        }

        if (_potionData == null)
        {
            Debug.LogError($"{name} : PotionData isn't assgined");
            enabled = false;
            return;
        }
        _nowItemData = _potionRunCatalog.GetPotionItemByData(_potionData);
        _renderer.sprite = _nowItemData.BottleSprite;
    }

    public bool CanSpawnAt(Vector3 position)
    {
        Collider2D itemCollider = _collider != null ? _collider : GetComponentInChildren<Collider2D>();
        _wallCollision.Configure(itemCollider, transform, _dropWallLayers);
        return _wallCollision.IsPositionClear(position);
    }

    public bool TryGetSpawnPosition(Vector3 origin, out Vector3 position)
    {
        Collider2D itemCollider = _collider != null ? _collider : GetComponentInChildren<Collider2D>();
        _wallCollision.Configure(itemCollider, transform, _dropWallLayers);
        return _wallCollision.TryGetSpawnPosition(origin, _dropWallPadding, out position);
    }

    public void PlayDropFlight(Vector3 target, float duration, float arcHeight)
    {
        if (!isActiveAndEnabled || _isCollecting || _isDropping) return;

        _dropTarget = target;
        _isDropping = true;
        SetAirborneSorting(true);
        _wallCollision.Configure(_collider, transform, _dropWallLayers);
        SetCollidersEnabled(false);
        _dropRoutine = StartCoroutine(DropFlightCoroutine(Mathf.Max(0.01f, duration), Mathf.Max(0f, arcHeight)));
    }

    private IEnumerator DropFlightCoroutine(float duration, float arcHeight)
    {
        Vector3 start = transform.position;
        Vector3 target = _dropTarget;
        Vector3 lastSafeGround = start;
        float elapsed = 0f;
        float previousT = 0f;

        while (elapsed < duration)
        {
            // 첫 프레임은 상자 위치에서 표시하고 Start의 포션 초기화를 기다린다.
            yield return null;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (_wallCollision.TryTrace(start, target, arcHeight, previousT, t,
                    _dropWallPadding, out PotionThrowPrediction hit))
            {
                // 투척용 검사 결과만 사용한다. 아이템은 깨뜨리지 않고 착지시킨다.
                _dropTarget = _wallCollision.GetLandingPosition(hit, lastSafeGround);
                transform.position = hit.ImpactPosition;
                yield return LandAfterWallHit(hit.ImpactPosition, _dropTarget);
                break;
            }

            Vector3 ground = ThrowTrajectory.EvaluateGroundPoint(start, target, t);
            if (_wallCollision.IsPositionClear(ground) && _wallCollision.IsPathClear(lastSafeGround, ground))
            {
                lastSafeGround = ground;
            }

            transform.position = ThrowTrajectory.EvaluateVisualPoint(start, target, arcHeight, t);
            previousT = t;
        }

        // 충돌이 없더라도 포션 크기를 포함해 최종 착지점을 확인한다.
        if (!_wallCollision.IsPositionClear(_dropTarget)) _dropTarget = lastSafeGround;
        transform.position = _dropTarget;
        SetAirborneSorting(false);
        // 착지 후에도 1초 동안 수집을 막는다.
        yield return new WaitForSeconds(1f);

        _dropRoutine = null;
        _isDropping = false;
        SetCollidersEnabled(true);
    }

    private IEnumerator LandAfterWallHit(Vector3 impact, Vector3 ground)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _wallLandingDuration);
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(impact, ground, t);
        }
        transform.position = ground;
    }

    public bool TryCollect(IItemReceiver receiver, Transform collectTarget, float collectRadius)
    {
        if (!isActiveAndEnabled || _isCollecting || _isDropping || receiver == null || collectTarget == null)
        {
            return false;
        }

        if (_potionData == null)
        {
            Debug.LogError($"{name}: PotionData is not assigned.", this);
            return false;
        }


        _originalPosition = transform.position;
        _isCollecting = true;

        SetCollidersEnabled(false);

        if (_nowItemData.IsEmpty)
        {
            return false;
        }
        _collectionRoutine = StartCoroutine(CollectCoroutine(receiver, collectTarget, Mathf.Max(0f, collectRadius), _nowItemData));

        return true;
    }


    // 플레이어에게 날아가는 동작을 구현하는 코루틴. 여러 코루틴이 연속적으로 수행됨.
    // CollectCoroutine, FlyToPlayer, DropNearPlayer 순서로 호출됨.
    private IEnumerator CollectCoroutine(IItemReceiver receiver, Transform collectTarget, float collectRadius, PotionItem potionItem)
    {
        SetAirborneSorting(true);
        if (_trailRenderer != null)
        {
            _trailRenderer.enabled = true;
        }

        yield return FlyToPlayer(collectTarget);

        if (collectTarget == null)
        {
            ResetToOriginalState();
            yield break;
        }

        if (receiver.TryTakeItem(potionItem, _quantity))
        {
            _collectionRoutine = null;
            _isBeingDestroyed = true;

            if (_touchedParticlePrefab != null)
            {
                Instantiate(_touchedParticlePrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject); // TODO : 풀링 적용 시 SetActive(false)로 변경
        }
        else
        {
            yield return DropNearPlayer(collectTarget, collectRadius); //인벤토리가 가득 찼다면 수집 범위 바깥으로 떨어트리는 루틴 실행

            _collectionRoutine = null;
            _isCollecting = false;
            _trailRenderer.enabled = false;

            SetCollidersEnabled(true);
        }

    }

    private IEnumerator FlyToPlayer(Transform collectTarget)
    {
        Vector3 start = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < _flightDuration)
        {
            if (collectTarget == null)
            {
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / _flightDuration);

            float t = EvaluateCurve(_flightCurve, normalizedTime);

            Vector3 end = collectTarget.position;
            Vector3 control = BezierCalculator.CalculateSideControlPoint(start, end, _arcHeight);

            transform.position = BezierCalculator.CalculateQuadraticBezier(start, control, end, t);

            yield return null;
        }

        if (collectTarget != null)
        {
            transform.position = collectTarget.position;
        }
    }

    private IEnumerator DropNearPlayer(Transform collectTarget, float collectRadius)
    {
        Vector3 dropStart = transform.position;

        //아이템이 처음 놓여 있던 방향으로 다시 튕겨 나가게 한다.
        Vector2 dropDirection = _originalPosition - collectTarget.position;

        if (dropDirection.sqrMagnitude <= 0.001f)
        {
            dropDirection = Random.insideUnitCircle;

            if (dropDirection.sqrMagnitude <= 0.001f)
            {
                dropDirection = Vector2.right;
            }
        }

        dropDirection.Normalize();

        float dropDistance = _failedDropPadding; // 수집 범위 바깥으로 떨어지는 거리를 계산.

        Vector3 dropEnd = collectTarget.position + (Vector3)(dropDirection * dropDistance);
        Vector3 dropControl = BezierCalculator.CalculateSideControlPoint(dropStart, dropEnd, _failedDropArcHeight, Vector3.up);

        float elapsedTime = 0f;

        while (elapsedTime < _failedDropDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / _failedDropDuration);

            t = Mathf.SmoothStep(0f, 1f, t); // 떨어지는 동작을 부드럽게 만듦.

            transform.position = BezierCalculator.CalculateQuadraticBezier(dropStart, dropControl, dropEnd, t);

            yield return null;
        }

        transform.position = dropEnd;
        SetAirborneSorting(false);
        yield return _waitForReactivate; // 일정 시간 후에 다시 수집 가능하도록 대기
    }

    private float EvaluateCurve(AnimationCurve curve, float normalizedTime)
    {
        if (curve == null)
        {
            return normalizedTime;
        }

        return Mathf.Clamp01(curve.Evaluate(normalizedTime));
    }

    private void ResetToOriginalState()
    {
        transform.position = _originalPosition;

        _collectionRoutine = null;
        _isCollecting = false;
        _trailRenderer.enabled = false;

        SetAirborneSorting(false);
        SetCollidersEnabled(true);
    }

    private void SetAirborneSorting(bool isAirborne)
    {
        // 바닥에서는 적(Enitty, 0) 뒤에, 비행 중에는 플레이어(FieldObject, 0) 앞에 표시한다.
        int layerID = SortingLayer.NameToID(isAirborne ? "FieldObject" : "Enitty");
        int order = isAirborne ? 1 : 0;
        if (_renderer != null)
        {
            _renderer.sortingLayerID = layerID;
        }
        if (_trailRenderer != null)
        {
            _trailRenderer.sortingLayerID = layerID;
            _trailRenderer.sortingOrder = order - 1;
        }
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        _collider.enabled = isEnabled;
    }
}
