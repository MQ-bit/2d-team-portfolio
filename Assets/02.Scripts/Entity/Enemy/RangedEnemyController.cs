using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class RangedEnemyController : MonoBehaviour
{
    private enum State
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [Header("Data")]
    [SerializeField] private RangedEnemyData _data;

    [Header("Module References")]
    [SerializeField] private EnemyMovementModule _movementModule;
    [SerializeField] private Health _health;
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;

    [Header("Attack")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private EnemyProjectile _projectilePrefab;

    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();

    private State _state = State.Idle;
    private Transform _target;
    private float _attackTimer;

    private bool CanAct => _potionEffectReceiver == null || _potionEffectReceiver.CanAct;

    private void Awake()
    {
        GetModules();
    }

    private void GetModules()
    {
        if (_data == null)
        {
            Debug.LogError($"{gameObject.name} : RangedEnemyData가 할당 안됨");
        }
        if (_movementModule == null)
        {
            _movementModule = GetComponent<EnemyMovementModule>();
            if (_movementModule == null)
            {
                Debug.LogError($"{gameObject.name} : EnemyMovementModule 이 할당 안됨");
            }
        }
        if (_health == null)
        {
            _health = GetComponent<Health>();
        }
        if (_potionEffectReceiver == null)
        {
            _potionEffectReceiver = GetComponent<PotionEffectReceiver>();
        }
        if (_firePoint == null)
        {
            _firePoint = transform;
        }
    }

    private void OnEnable()
    {
        _health.Changed += HandleHealthChanged;
    }

    private void OnDisable()
    {
        _health.Changed -= HandleHealthChanged;
    }

    private void FixedUpdate()
    {
        if (_state == State.Dead || _data == null)
        {
            return;
        }

        // 얼음/마비 중에는 이동과 공격 모두 멈춘다.
        if (!CanAct)
        {
            _movementModule.Stop();
            return;
        }

        _target = FindPlayerInRange(_data.DetectRange);
        if (_target == null)
        {
            _state = State.Idle;
            _movementModule.Stop();
            return;
        }

        float distance = Vector2.Distance(transform.position, _target.position);

        // AttackRange(공격 진입)와 ChaseResumeRange(추적 복귀) 두 임계값을 분리한 히스테리시스 구간.
        // 두 값이 같으면 플레이어가 경계선에서 미세하게 움직이기만 해도 매 프레임
        // 추적<->공격 상태가 뒤바뀌며 몬스터가 제자리에서 버벅거리는(jittering) 문제가 생긴다.
        if (_state == State.Attack)
        {
            if (distance > _data.ChaseResumeRange)
            {
                _state = State.Chase;
            }
        }
        else if (distance <= _data.AttackRange)
        {
            EnterAttack();
        }

        switch (_state)
        {
            case State.Idle:
            case State.Chase:
                _state = State.Chase;
                _movementModule.MoveTowards(_target.position, _data.MoveSpeed);
                break;
            case State.Attack:
                _movementModule.Stop();
                UpdateAttack();
                break;
        }
    }

    private void EnterAttack()
    {
        _state = State.Attack;
        _attackTimer = 0f;
    }

    private void UpdateAttack()
    {
        _attackTimer -= Time.fixedDeltaTime;
        if (_attackTimer > 0f)
        {
            return;
        }

        FireProjectile();
        _attackTimer = _data.AttackCooldown;
    }

    private void FireProjectile()
    {
        if (_projectilePrefab == null || _target == null)
        {
            return;
        }

        Vector2 origin = _firePoint.position;
        Vector2 direction = ((Vector2)_target.position - origin).normalized;

        EnemyProjectile projectile = Instantiate(_projectilePrefab, origin, Quaternion.identity);
        projectile.Launch(direction, _data.ProjectileSpeed, _data.ProjectileDamage, _data.ProjectileLifetime);
    }

    // 플레이어는 태그/레이어가 없어서 TargetKind가 Player인 PotionEffectReceiver로 찾는다.
    private Transform FindPlayerInRange(float range)
    {
        Physics2D.OverlapCircle(transform.position, range, ContactFilter2D.noFilter, _overlapResults);
        foreach (Collider2D hit in _overlapResults)
        {
            PotionEffectReceiver receiver = hit.GetComponentInParent<PotionEffectReceiver>();
            if (receiver != null && receiver.TargetKind == PotionTargetKind.Player)
            {
                return receiver.transform;
            }
        }

        return null;
    }

    // 포션 등으로 죽으면 그냥 사라진다.
    private void HandleHealthChanged(float current, float max)
    {
        if (_state == State.Dead || !_health.IsDead)
        {
            return;
        }

        _state = State.Dead;
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (_data == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _data.DetectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _data.AttackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _data.ChaseResumeRange);
    }
}
