using System.Collections.Generic;
using UnityEngine;

public class EnemyMovementModule : MonoBehaviour, IKnockbackable
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D _rigid;

    [Header("Knockback")]
    [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.15f; // direction * force 만큼 밀려나는 데 걸리는 시간

    [Header("Pathfinding")]
    [Tooltip("경로상 다음 지점을 다시 고르는 간격(초). 지점에 도착하면 간격과 상관없이 바로 다시 고른다.")]
    [SerializeField, Min(0.02f)] private float _repathInterval = 0.15f;
    [SerializeField, Min(0.01f)] private float _waypointReachDistance = 0.1f;

    [Header("Separation")]
    [Tooltip("쫓아가는 동안 이 거리 안의 다른 몬스터에게서 멀어지려 한다. 0이면 끈다.")]
    [SerializeField, Min(0f)] private float _separationRadius = 1.2f;
    [Tooltip("밀어내는 힘. 1이면 가까이 붙었을 때 이동 속도만큼 밀려난다.")]
    [SerializeField, Min(0f)] private float _separationStrength = 0.8f;

    private readonly List<Collider2D> _neighborResults = new List<Collider2D>();

    private float _knockbackEndTime;
    private bool _isKnockedBack;

    private Vector2 _waypoint;
    private bool _hasWaypoint;
    private bool _isDirectChase;
    private float _nextRepathTime;

    private void Awake()
    {
        if (_rigid == null)
        {
            _rigid = GetComponent<Rigidbody2D>();
        }
    }

    // FixedUpdate에서 호출한다.
    public void MoveTowards(Vector2 targetPosition, float speed)
    {
        if (UpdateKnockback())
        {
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(_rigid.position, targetPosition, speed * Time.fixedDeltaTime);
        _rigid.MovePosition(nextPosition);
    }

    // FixedUpdate에서 호출한다. 벽/소품을 돌아서 targetPosition까지 쫓아간다(EnemyPathfinder).
    // 목표가 직선으로 보이면 그대로 직진하고, 길을 못 찾으면 예전처럼 직진한다.
    public void ChaseTarget(Vector2 targetPosition, float speed)
    {
        if (UpdateKnockback())
        {
            return;
        }

        float step = speed * Time.fixedDeltaTime;
        Vector2 destination = GetChaseDestination(targetPosition);
        Vector2 nextPosition = Vector2.MoveTowards(_rigid.position, destination, step);

        // 같은 길로 몰려와도 겹치지 않게 주변 몬스터 반대쪽으로 조금씩 비켜난다. 한 프레임 이동량을 넘지 않는다.
        nextPosition += Vector2.ClampMagnitude(GetSeparation() * _separationStrength, 1f) * step;
        _rigid.MovePosition(nextPosition);
    }

    // 가까운 몬스터일수록 강하게 밀어내는 방향의 합.
    private Vector2 GetSeparation()
    {
        if (_separationRadius <= 0f || _separationStrength <= 0f)
        {
            return Vector2.zero;
        }

        ContactFilter2D filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = 1 << gameObject.layer
        };
        Physics2D.OverlapCircle(_rigid.position, _separationRadius, filter, _neighborResults);

        Vector2 push = Vector2.zero;
        foreach (Collider2D hit in _neighborResults)
        {
            Rigidbody2D other = hit.attachedRigidbody;
            if (other == null || other == _rigid || !other.TryGetComponent(out EnemyMovementModule _))
            {
                continue;
            }

            Vector2 away = _rigid.position - other.position;
            float distance = away.magnitude;
            if (distance < 0.001f)
            {
                // 완전히 겹쳐 있으면 서로 반대쪽으로 갈라지게 인스턴스 ID로 방향을 정한다.
                away = _rigid.GetInstanceID() < other.GetInstanceID() ? Vector2.right : Vector2.left;
                distance = 0.001f;
            }

            push += away / distance * (1f - Mathf.Clamp01(distance / _separationRadius));
        }

        return push;
    }

    private Vector2 GetChaseDestination(Vector2 targetPosition)
    {
        bool hasReachedWaypoint = _hasWaypoint && !_isDirectChase
            && Vector2.Distance(_rigid.position, _waypoint) <= _waypointReachDistance;

        if (Time.fixedTime >= _nextRepathTime || hasReachedWaypoint)
        {
            _nextRepathTime = Time.fixedTime + _repathInterval;
            _hasWaypoint = EnemyPathfinder.Instance.TryGetNextWaypoint(_rigid.position, targetPosition,
                out _waypoint, out _isDirectChase);
        }

        // 직선 추적 중이면 고를 때의 위치가 아니라 목표의 지금 위치로 간다.
        if (!_hasWaypoint || _isDirectChase)
        {
            return targetPosition;
        }

        return _waypoint;
    }

    public void Stop()
    {
        if (UpdateKnockback())
        {
            return;
        }

        _rigid.linearVelocity = Vector2.zero;
    }

    // direction * force 만큼 밀려난다. 위치를 직접 옮기지 않고 속도로 밀어서 벽/다른 적과의 충돌이 적용된다.
    public void ApplyKnockback(Vector2 direction, float force)
    {
        _rigid.linearVelocity = direction * force / _knockbackDuration;
        _knockbackEndTime = Time.fixedTime + _knockbackDuration; // Update에서 호출돼도 물리 시간 기준으로 맞춘다.
        _isKnockedBack = true;
    }

    // 넉백 중이면 true를 돌려줘서 컨트롤러의 이동/정지 명령이 넉백 속도를 덮어쓰지 않게 한다.
    // 넉백이 막 끝났으면 남은 속도를 지운다 (감속이 없어서 그대로 두면 계속 미끄러진다).
    private bool UpdateKnockback()
    {
        if (!_isKnockedBack)
        {
            return false;
        }

        if (Time.fixedTime < _knockbackEndTime)
        {
            return true;
        }

        _isKnockedBack = false;
        _rigid.linearVelocity = Vector2.zero;
        return false;
    }
}
