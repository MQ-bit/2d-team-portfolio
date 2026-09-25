using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public sealed class PotionFieldItem : MonoBehaviour, IItemCollectable
{
    [Header("Item")]
    [SerializeField] private int _quantity = 1;

    // 실제 PotionData의 직렬화 방식이 확정되면 활성화한다.
    // [SerializeField] private PotionData _potionData;

    [Header("Collect Movement Settings")]
    [SerializeField] private float _flightDuration = 0.45f;
    [SerializeField] private float _arcHeight = 1f;

    [SerializeField] private AnimationCurve _flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Failed Collection Setting")]
    [SerializeField] private float _failedDropPadding = 0.5f;

    [SerializeField] private float _failedDropDuration = 0.3f;

    [SerializeField] private float _failedDropArcHeight = 0.75f;

    /*
     * 테스트용 데이터
     *
     * EntityInventory가 PotionData 인스턴스를 Dictionary 키로 사용하므로
     * 모든 테스트 필드아이템이 동일한 인스턴스를 공유해야 한 슬롯에 쌓인다.
     */
    private static readonly PotionData TestPotionData =
        new PotionData("test_potion", "Test Potion", PotionElement.Fire, 0f, 0f, 0f, 0f, 0f);

    private Collider2D[] _colliders;
    private Coroutine _collectionRoutine;

    private Vector3 _originalPosition;
    private bool _isCollecting;
    private bool _isBeingDestroyed;

    private void Awake()
    {
        _colliders = GetComponentsInChildren<Collider2D>();
    }

    public bool TryCollect(IItemReceiver receiver, Transform collectTarget, float collectRadius)
    {
        if (!isActiveAndEnabled || _isCollecting || receiver == null || collectTarget == null)
        {
            return false;
        }

        // 실제 PotionData가 준비되면 아래 코드를 활성화하기.
        // PotionData potionData = _potionData;

        PotionData potionData = TestPotionData;

        _originalPosition = transform.position;
        _isCollecting = true;

        SetCollidersEnabled(false);

        _collectionRoutine = StartCoroutine(CollectCoroutine(receiver, collectTarget, Mathf.Max(0f, collectRadius), potionData));

        return true;
    }

    private IEnumerator CollectCoroutine(IItemReceiver receiver, Transform collectTarget, float collectRadius, PotionData potionData)
    {
        yield return FlyToPlayer(collectTarget);

        if (collectTarget == null)
        {
            ResetToOriginalState();
            yield break;
        }

        //플레이어에게 도착한 순간 실제 인벤토리 추가를 시도
        if (receiver.TryReceive(potionData, _quantity))
        {
            _collectionRoutine = null;
            _isBeingDestroyed = true;

            Destroy(gameObject); // TODO : 풀링 적용 시 SetActive(false)로 변경
        }
        else
        {
            //인벤토리가 가득 찼다면 수집 범위 바깥으로 떨어트리는 루틴 실행
            yield return DropNearPlayer(collectTarget, collectRadius);

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
            Vector3 control = CalculateSideControlPoint(start, end, _arcHeight);

            transform.position = EvaluateQuadraticBezier(start, control, end, t);

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

        /*
         * OnTriggerStay2D로 즉시 다시 빨려 들어가는 것을 막기 위해
         * 수집 범위보다 약간 바깥에 착지시킨다.
         */
        float dropDistance = collectRadius + _failedDropPadding;

        Vector3 dropEnd = collectTarget.position + (Vector3)(dropDirection * dropDistance);
        Vector3 dropControl = CalculateDropControlPoint(dropStart, dropEnd, _failedDropArcHeight);

        float elapsedTime = 0f;

        while (elapsedTime < _failedDropDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / _failedDropDuration);

            // 떨어지는 동작을 조금 부드럽게 만든다.
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = EvaluateQuadraticBezier(dropStart, dropControl, dropEnd, t);

            yield return null;
        }

        transform.position = dropEnd;
    }

    private static Vector3 CalculateSideControlPoint(Vector3 start, Vector3 end, float arcHeight)
    {
        Vector3 direction = end - start;

        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);

        if (perpendicular.sqrMagnitude > 0f)
        {
            perpendicular.Normalize();
        }

        return (start + end) * 0.5f + perpendicular * arcHeight;
    }

    private static Vector3 CalculateDropControlPoint(Vector3 start, Vector3 end, float arcHeight)
    {
        //2D 화면에서 위로 살짝 솟았다가 떨어지는 형태다.
        return (start + end) * 0.5f + Vector3.up * arcHeight;
    }

    private static Vector3 EvaluateQuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float inverseT = 1f - t;

        return inverseT * inverseT * start + 2f * inverseT * t * control + t * t * end;
    }

    private static float EvaluateCurve(AnimationCurve curve, float normalizedTime)
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
        if (_colliders == null)
        {
            return;
        }

        foreach (Collider2D itemCollider in _colliders)
        {
            if (itemCollider != null)
            {
                itemCollider.enabled = isEnabled;
            }
        }
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
}