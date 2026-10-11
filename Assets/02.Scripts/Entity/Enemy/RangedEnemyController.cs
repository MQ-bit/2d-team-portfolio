using System;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;
    [SerializeField] private EnemyStatus _status;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Attack")]
    [Tooltip("투사체가 나가는 위치. 이 오브젝트의 직계 자식으로, 오른쪽을 바라보는 기준으로 배치한다. " +
             "왼쪽을 보고 공격하면 좌우를 뒤집어서 쓴다. 비워두면 몸 중심에서 나간다.")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private EnemyProjectile _projectilePrefab;

    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();

    private State _state = State.Idle;
    private Transform _target;
    private float _attackTimer;
    private float _fireTimer;
    private bool _isFiring; // 공격을 시작해서 투사체가 나가기를 기다리는 중
    private bool _isAttackFacingLeft; // 공격을 시작할 때 플레이어가 왼쪽에 있었는지

    private bool CanMove => _potionEffectReceiver == null || _potionEffectReceiver.CanMove;

    // 공격 애니메이션 재생용. 인자는 공격하며 왼쪽을 바라보는지 여부. 투사체는 AttackFireDelay 뒤에 나간다.
    public event Action<bool> OnAttackStarted;
    // 투사체가 나가기 전에 공격이 끊긴 순간 (빙결/마비, 대상 소실)
    public event Action OnAttackCanceled;

    public bool IsAttacking => _isFiring;

    private void Awake()
    {
        GetModules();

        _status.Initialize(_data != null ? _data.MaxHealth : 100f, _data != null ? _data.MoveSpeed : 2.5f);

        if (_data != null)
        {
            _data.ApplyVisual(_spriteRenderer);
        }
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
        if (_potionEffectReceiver == null)
        {
            _potionEffectReceiver = GetComponent<PotionEffectReceiver>();
        }
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (_firePoint == null)
        {
            _firePoint = transform;
        }
        if (_status == null)
        {
            _status = GetComponent<EnemyStatus>();
            if (_status == null)
            {
                Debug.LogError($"{gameObject.name} : EnemyStatus 가 할당 안됨");
            }
        }
    }

    private void OnEnable()
    {
        _status.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        _status.OnDeath -= HandleDeath;
    }

    // 포션 등으로 죽으면 그냥 사라진다.
    private void HandleDeath()
    {
        if (_state == State.Dead)
        {
            return;
        }

        _state = State.Dead;
        Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (_state == State.Dead || _data == null)
        {
            return;
        }

        // 얼음/마비 중에는 이동과 공격 모두 멈춘다. 준비 중이던 공격은 취소한다.
        if (!CanMove)
        {
            CancelAttack();
            _movementModule.Stop();
            return;
        }

        _target = FindPlayerInRange(_data.DetectRange);
        if (_target == null)
        {
            CancelAttack();
            _state = State.Idle;
            _movementModule.Stop();
            return;
        }

        float distance = Vector2.Distance(transform.position, _target.position);

        // AttackRange(공격 진입)와 ChaseResumeRange(추적 복귀) 두 임계값을 분리한 히스테리시스 구간.
        // 두 값이 같으면 플레이어가 경계선에서 미세하게 움직이기만 해도 매 프레임
        // 추적<->공격 상태가 뒤바뀌며 몬스터가 제자리에서 버벅거리는(jittering) 문제가 생긴다.
        // 이미 시작한 공격은 사거리를 벗어나도 투사체를 쏠 때까지 유지한다.
        if (_state == State.Attack)
        {
            if (!_isFiring && distance > _data.ChaseResumeRange)
            {
                _state = State.Chase;
            }
        }
        else if (distance <= _data.AttackRange && HasLineOfSightToTarget())
        {
            EnterAttack();
        }

        // 벽 뒤로 숨으면 쏘지 않고 보이는 곳까지 돌아서 다가간다. 이미 시작한 공격은 그대로 쏜다.
        if (_state == State.Attack && !_isFiring && !HasLineOfSightToTarget())
        {
            _state = State.Chase;
        }

        switch (_state)
        {
            case State.Idle:
            case State.Chase:
                _state = State.Chase;
                _movementModule.ChaseTarget(_target.position, _status.Speed);
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

    // 쿨타임이 끝나면 공격을 시작하고(애니메이션), AttackFireDelay 뒤에 투사체를 쏜다.
    // 쿨타임은 공격 시작 시점부터 잰다.
    private void UpdateAttack()
    {
        _attackTimer -= Time.fixedDeltaTime;

        if (_isFiring)
        {
            _fireTimer -= Time.fixedDeltaTime;
            if (_fireTimer <= 0f)
            {
                _isFiring = false;
                FireProjectile();
            }
            return;
        }

        if (_attackTimer > 0f)
        {
            return;
        }

        StartAttack();
    }

    private void StartAttack()
    {
        _isFiring = true;
        _fireTimer = _data.AttackFireDelay;
        _attackTimer = _data.AttackCooldown;
        _isAttackFacingLeft = _target.position.x < transform.position.x;
        OnAttackStarted?.Invoke(_isAttackFacingLeft);
    }

    private void CancelAttack()
    {
        if (!_isFiring)
        {
            return;
        }

        _isFiring = false;
        OnAttackCanceled?.Invoke();
    }

    private void FireProjectile()
    {
        if (_projectilePrefab == null || _target == null)
        {
            return;
        }

        Vector2 origin = GetFireOrigin();
        Vector2 direction = ((Vector2)_target.position - origin).normalized;

        // 투사체가 여러 개면 플레이어 방향을 가운데로 두고 부채꼴로 고르게 퍼뜨린다.
        int count = _data.ProjectileCount;
        float angleStep = count > 1 ? _data.SpreadAngle / (count - 1) : 0f;
        float startAngle = count > 1 ? -_data.SpreadAngle * 0.5f : 0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, startAngle + angleStep * i) * direction;
            EnemyProjectile projectile = Instantiate(_projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(shotDirection, _data.ProjectileSpeed, _data.ProjectileDamage, _data.ProjectileLifetime);
        }

        SoundManager.Play(GameSoundId.EnemyAttack);
    }

    // FirePoint는 오른쪽을 바라보는 기준으로 놓여 있으므로, 왼쪽을 보고 공격 중이면 x를 뒤집는다.
    // TransformPoint를 써서 엘리트처럼 크기가 커진 적도 같은 비율의 위치에서 나간다.
    private Vector2 GetFireOrigin()
    {
        if (_firePoint == transform)
        {
            return transform.position;
        }

        Vector3 localPoint = _firePoint.localPosition;
        if (_isAttackFacingLeft)
        {
            localPoint.x = -localPoint.x;
        }

        return transform.TransformPoint(localPoint);
    }

    private bool HasLineOfSightToTarget()
    {
        return EnemyPathfinder.Instance.HasLineOfSight(GetFireOrigin(), _target.position);
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
