using UnityEngine;

// 적의 이동/피격 상태를 Animator(자식 Visual)에 전달한다. 컨트롤러 상태에 직접 접근하지 않고 관찰만 한다.
// - 이동: FixedUpdate마다 Rigidbody2D 위치 변화로 판단 (MovePosition은 velocity를 남기지 않으므로)
// - 방향: 가로 이동 방향에 따라 스프라이트를 좌우 반전 (에셋은 오른쪽을 바라봄). 공격 중에는 공격 방향을 유지한다.
// - 피격: EnemyStatus.OnDamaged에서 Hurt 트리거. 공격 중에는 공격 애니메이션을 끊지 않도록 무시한다.
// - 공격(원거리): RangedEnemyController.OnAttackStarted에서 플레이어 쪽을 보고 Attack 트리거, 취소되면 Idle로 되돌린다.
public class EnemyAnimationDriver : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int AttackStateHash = Animator.StringToHash("Attack");

    [Header("References")]
    [SerializeField] private EnemyStatus _status;
    [SerializeField] private RangedEnemyController _rangedController; // 원거리 적만. 없으면 공격 애니메이션을 쓰지 않는다.
    [SerializeField] private Rigidbody2D _rigid;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveThreshold = 0.1f; // 초당 이동량이 이 값보다 커야 걷는 것으로 본다.

    private Vector2 _lastPosition;
    private EnemyMovementModule _movement;
    private bool _wasKnockedBack;
    private bool _attackCanceledWhileFrozen;
    private float _playbackSpeed = 1f;
    private bool IsFrozen => _status != null && !_status.IsDead && !_status.CanMove;

    private void Awake()
    {
        if (_status == null)
        {
            _status = GetComponent<EnemyStatus>();
            if (_status == null)
            {
                Debug.LogError($"{gameObject.name} : EnemyStatus 가 할당 안됨");
            }
        }
        if (_rigid == null)
        {
            _rigid = GetComponent<Rigidbody2D>();
            if (_rigid == null)
            {
                Debug.LogError($"{gameObject.name} : Rigidbody2D 가 할당 안됨");
            }
        }
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogError($"{gameObject.name} : Animator 가 할당 안됨");
            }
        }
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (_rangedController == null)
        {
            _rangedController = GetComponent<RangedEnemyController>();
        }
        _movement = GetComponent<EnemyMovementModule>();
        if (_animator != null) _playbackSpeed = _animator.speed;
    }

    private void OnEnable()
    {
        _wasKnockedBack = false;
        _attackCanceledWhileFrozen = false;
        if (_animator != null)
        {
            _animator.speed = _playbackSpeed;
            _animator.SetBool(IsMovingHash, false);
            _animator.ResetTrigger(HurtHash);
            _animator.ResetTrigger(AttackHash);
            _animator.Play(IdleStateHash, 0, 0f);
        }
        // 재활성화(풀링, 순간이동 등) 직후 첫 프레임에 이전 위치와의 차이를 이동으로 오인하지 않도록 기준점을 다시 잡는다.
        if (_rigid != null)
        {
            _lastPosition = _rigid.position;
        }

        if (_status != null)
        {
            _status.OnDamaged += HandleDamaged;
        }
        if (_rangedController != null)
        {
            _rangedController.OnAttackStarted += HandleAttackStarted;
            _rangedController.OnAttackCanceled += HandleAttackCanceled;
        }
    }

    private void OnDisable()
    {
        if (_animator != null) _animator.speed = _playbackSpeed;
        if (_status != null)
        {
            _status.OnDamaged -= HandleDamaged;
        }
        if (_rangedController != null)
        {
            _rangedController.OnAttackStarted -= HandleAttackStarted;
            _rangedController.OnAttackCanceled -= HandleAttackCanceled;
        }
    }

    private void Update()
    {
        if (_animator == null) return;
        bool frozen = IsFrozen;
        _animator.speed = frozen ? 0f : _playbackSpeed;
        if (!frozen && _attackCanceledWhileFrozen)
        {
            _attackCanceledWhileFrozen = false;
            _animator.Play(IdleStateHash, 0, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (_rigid == null || _animator == null)
        {
            return;
        }

        Vector2 position = _rigid.position;
        Vector2 delta = position - _lastPosition;
        _lastPosition = position;
        bool knockedBack = _movement != null && _movement.IsKnockedBack;
        bool excludeKnockback = knockedBack || _wasKnockedBack;
        _wasKnockedBack = knockedBack;
        // 정지된 프레임의 상태/방향도 그대로 둔다. 위치 샘플은 계속 갱신한다.
        if (IsFrozen) return;

        float speed = delta.magnitude / Time.fixedDeltaTime;
        bool moving = (_status == null || !_status.IsDead) && !excludeKnockback && speed > _moveThreshold;
        _animator.SetBool(IsMovingHash, moving);

        // 공격 중 넉백으로 밀려도 공격하던 방향을 유지한다.
        if (_spriteRenderer != null && moving && Mathf.Abs(delta.x) > 0.0001f && !IsPlayingAttack())
        {
            _spriteRenderer.flipX = delta.x < 0f;
        }
    }

    private void HandleDamaged()
    {
        if (_animator == null || IsFrozen || IsPlayingAttack())
        {
            return;
        }

        _animator.SetTrigger(HurtHash);
    }

    private void HandleAttackStarted(bool isFacingLeft)
    {
        if (_animator == null || IsFrozen)
        {
            return;
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.flipX = isFacingLeft;
        }

        _animator.SetTrigger(AttackHash);
    }

    // 투사체를 쏘기 전(컨트롤러 기준)이거나 공격 애니메이션을 재생/진입 중이면 공격 중으로 본다.
    private bool IsPlayingAttack()
    {
        if (_rangedController == null)
        {
            return false;
        }

        if (_rangedController.IsAttacking)
        {
            return true;
        }

        if (_animator == null)
        {
            return false;
        }

        if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackStateHash)
        {
            return true;
        }

        return _animator.IsInTransition(0)
            && _animator.GetNextAnimatorStateInfo(0).shortNameHash == AttackStateHash;
    }

    private void HandleAttackCanceled()
    {
        if (_animator == null)
        {
            return;
        }

        _animator.ResetTrigger(AttackHash);
        if (IsFrozen)
        {
            _attackCanceledWhileFrozen = true;
            return;
        }
        _attackCanceledWhileFrozen = false;
        _animator.Play(IdleStateHash, 0, 0f);
    }
}
