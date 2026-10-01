using UnityEngine;

// 플레이어 모듈들의 상태를 관찰해서 Animator(자식 Visual)에 전달한다. 모듈 동작에는 관여하지 않는다.
// - 상태 우선순위: 사망 > 구르기(대시) > 근접 공격 > 투척 > 마시기 > 걷기 > 대기
// - 방향: 동/서/남/북 4방향. 공격·조준·투척 중에는 그 방향을, 그 외에는 이동 입력(없으면 실제 이동) 방향을 바라본다.
// - 각 상태는 Animator에서 DirX/DirY로 방향을 고르는 블렌드 트리이고, 상태 전환은 여기서 Play로 직접 한다.
public class PlayerAnimationDriver : MonoBehaviour
{
    private static readonly int DirXHash = Animator.StringToHash("DirX");
    private static readonly int DirYHash = Animator.StringToHash("DirY");

    private static readonly int IdleState = Animator.StringToHash("Idle");
    private static readonly int WalkState = Animator.StringToHash("Walk");
    private static readonly int RollState = Animator.StringToHash("Roll");
    private static readonly int AttackState = Animator.StringToHash("Attack");
    private static readonly int ThrowState = Animator.StringToHash("Throw");
    private static readonly int DrinkState = Animator.StringToHash("Drink");
    private static readonly int DieState = Animator.StringToHash("Die");

    [Header("References")]
    [SerializeField] private InputReader _input; // 이동 입력 방향을 바로 바라보기 위함. 없으면 실제 이동 방향만 쓴다.
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody2D _rigid;
    [SerializeField] private PlayerStatus _status;
    [SerializeField] private PlayerMovementModule _movementModule;
    [SerializeField] private PlayerMeleeModule _meleeModule;
    [SerializeField] private PlayerThrowingModule _throwingModule;
    [SerializeField] private PlayerDrinkingModule _drinkingModule;

    [Header("Clip Lengths")]
    [Tooltip("Roll 클립 길이(초). 실제 대시 시간에 맞춰 재생 속도를 조절한다.")]
    [SerializeField, Min(0.01f)] private float _rollClipLength = 0.4f;
    [Tooltip("Attack 클립 길이(초). 실제 휘두르기 시간에 맞춰 재생 속도를 조절한다.")]
    [SerializeField, Min(0.01f)] private float _attackClipLength = 0.3f;
    [Tooltip("Throw 클립 길이(초). 던진 뒤 이 시간 동안 투척 모션을 보여준다.")]
    [SerializeField, Min(0.01f)] private float _throwClipLength = 0.33f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveThreshold = 0.1f; // 초당 이동량이 이 값보다 커야 걷는 것으로 본다.

    private Vector2 _facing = Vector2.down;
    private Vector2 _lastPosition;
    private bool _isMoving;
    private int _currentState;
    private float _throwEndTime;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogError($"{gameObject.name} : Animator 가 할당 안됨");
            }
        }
        if (_rigid == null)
        {
            _rigid = GetComponent<Rigidbody2D>();
        }
        if (_status == null)
        {
            _status = GetComponent<PlayerStatus>();
        }
        if (_movementModule == null)
        {
            _movementModule = GetComponent<PlayerMovementModule>();
        }
        if (_meleeModule == null)
        {
            _meleeModule = GetComponent<PlayerMeleeModule>();
        }
        if (_throwingModule == null)
        {
            _throwingModule = GetComponent<PlayerThrowingModule>();
        }
        if (_drinkingModule == null)
        {
            _drinkingModule = GetComponent<PlayerDrinkingModule>();
        }

        CopySortingFromRootRenderer();
    }

    // 씬마다 루트 SpriteRenderer의 정렬 레이어를 바꿔 쓰는 경우가 있어서(예: IngameScene), Visual도 같은 정렬값을 따르게 한다.
    // 루트 렌더러는 꺼져 있지만 ThrowAimPreview가 정렬값을 읽으므로 남겨둔다.
    private void CopySortingFromRootRenderer()
    {
        SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
        SpriteRenderer visualRenderer = _animator != null ? _animator.GetComponent<SpriteRenderer>() : null;
        if (rootRenderer == null || visualRenderer == null)
        {
            return;
        }

        visualRenderer.sortingLayerID = rootRenderer.sortingLayerID;
        visualRenderer.sortingOrder = rootRenderer.sortingOrder;
    }

    private void OnEnable()
    {
        // 재활성화 직후 첫 프레임에 이전 위치와의 차이를 이동으로 오인하지 않도록 기준점을 다시 잡는다.
        if (_rigid != null)
        {
            _lastPosition = _rigid.position;
        }
        if (_throwingModule != null)
        {
            _throwingModule.OnPotionThrown += HandlePotionThrown;
        }

        _currentState = 0;
    }

    private void OnDisable()
    {
        if (_throwingModule != null)
        {
            _throwingModule.OnPotionThrown -= HandlePotionThrown;
        }
    }

    private void FixedUpdate()
    {
        if (_rigid == null)
        {
            return;
        }

        // MovePosition은 velocity를 남기지 않으므로 위치 변화로 이동 여부와 방향을 판단한다.
        Vector2 position = _rigid.position;
        Vector2 delta = position - _lastPosition;
        _lastPosition = position;

        _isMoving = delta.magnitude / Time.fixedDeltaTime > _moveThreshold;
        if (_isMoving)
        {
            _facing = delta;
        }
    }

    private void Update()
    {
        if (_animator == null)
        {
            return;
        }

        // 구르기 첫 프레임처럼 아직 위치가 바뀌기 전에도 누른 방향을 바로 바라보게 한다.
        if (_input != null && _input.MoveInput.sqrMagnitude > Mathf.Epsilon)
        {
            _facing = _input.MoveInput;
        }

        int nextState = SelectState();
        UpdateFacing(nextState);

        Vector2 direction = SnapToFourDirections(_facing);
        _animator.SetFloat(DirXHash, direction.x);
        _animator.SetFloat(DirYHash, direction.y);

        PlayState(nextState);
    }

    private int SelectState()
    {
        if (_status != null && _status.IsDead)
        {
            return DieState;
        }
        if (_movementModule != null && _movementModule.IsDashing)
        {
            return RollState;
        }
        if (_meleeModule != null && _meleeModule.IsSwinging)
        {
            return AttackState;
        }
        if (Time.time < _throwEndTime)
        {
            return ThrowState;
        }
        if (_drinkingModule != null && _drinkingModule.IsDrinking)
        {
            return DrinkState;
        }

        return _isMoving ? WalkState : IdleState;
    }

    // 공격·조준·투척 중에는 그 방향을 바라보고, 그 외에는 FixedUpdate에서 구한 이동 방향을 유지한다.
    private void UpdateFacing(int state)
    {
        if (state == AttackState)
        {
            _facing = _meleeModule.LastAttackDirection;
        }
        else if (_throwingModule != null && _throwingModule.IsAiming && state != RollState && state != DieState)
        {
            _facing = _throwingModule.AimWorldPosition - (Vector2)transform.position;
        }
    }

    private void PlayState(int state)
    {
        if (state == DrinkState)
        {
            // 마시는 시간이 바뀌어도 애니메이션이 진행도와 맞도록 직접 위치를 지정한다.
            _animator.speed = 0f;
            _animator.Play(DrinkState, 0, _drinkingModule.DrinkProgress);
            _currentState = state;
            return;
        }

        _animator.speed = GetPlaybackSpeed(state);

        if (state == _currentState)
        {
            return;
        }

        _currentState = state;
        _animator.Play(state, 0, 0f);
    }

    private float GetPlaybackSpeed(int state)
    {
        if (state == RollState && _movementModule.DashDuration > 0f)
        {
            return _rollClipLength / _movementModule.DashDuration;
        }
        if (state == AttackState && _meleeModule.SwingDuration > 0f)
        {
            return _attackClipLength / _meleeModule.SwingDuration;
        }

        return 1f;
    }

    private void HandlePotionThrown(Vector2 targetPosition)
    {
        _facing = targetPosition - (Vector2)transform.position;
        _throwEndTime = Time.time + _throwClipLength;

        // 연속으로 던지면 처음부터 다시 재생한다.
        if (_currentState == ThrowState)
        {
            _currentState = 0;
        }
    }

    // 가로/세로 중 더 큰 축으로 동서남북 중 하나를 고른다. 같으면 가로를 우선한다.
    private static Vector2 SnapToFourDirections(Vector2 direction)
    {
        if (direction.sqrMagnitude < Mathf.Epsilon)
        {
            return Vector2.down;
        }
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? Vector2.right : Vector2.left;
        }

        return direction.y >= 0f ? Vector2.up : Vector2.down;
    }
}
