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

    private PotionItem _nowItemData;

    private WaitForSeconds _waitForReactivate;
    private Collider2D _collider;
    private Coroutine _collectionRoutine;

    private Vector3 _originalPosition;
    private bool _isCollecting;
    private bool _isBeingDestroyed;

    private void Awake()
    {
        _collider = GetComponentInChildren<Collider2D>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _waitForReactivate = new WaitForSeconds(_reactivateDelay);
    }
    private void OnDisable()
    {
        if (_isBeingDestroyed)
        {
            return;
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

        _isCollecting = false;
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
        _nowItemData = _potionRunCatalog.CreatePotionItem(_potionData);
        _renderer.sprite = _nowItemData.Sprite;
    }

    public bool TryCollect(IItemReceiver receiver, Transform collectTarget, float collectRadius)
    {
        if (!isActiveAndEnabled || _isCollecting || receiver == null || collectTarget == null)
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

            Destroy(gameObject); // TODO : 풀링 적용 시 SetActive(false)로 변경
        }
        else
        {
            yield return DropNearPlayer(collectTarget, collectRadius); //인벤토리가 가득 찼다면 수집 범위 바깥으로 떨어트리는 루틴 실행

            _collectionRoutine = null;
            _isCollecting = false;

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

        SetCollidersEnabled(true);
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        _collider.enabled = isEnabled;
    }
}
