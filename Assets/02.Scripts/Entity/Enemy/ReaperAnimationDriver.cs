using UnityEngine;

// 원본 프레임의 잘린 사각형 크기가 달라도 몸의 기준점이 흔들리지 않게 정렬한다.
public sealed class ReaperAnimationDriver : MonoBehaviour
{
    [SerializeField] private ReaperController _controller;
    [SerializeField] private EnemyStatus _status;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Vector2 _bodyAnchorPixels = new Vector2(105f, 18f);
    private int _playing;
    private float _hurtUntil;
    private Rigidbody2D _rigid;
    private EnemyMovementModule _movement;
    private Vector2 _lastPosition;
    private bool _wasKnockedBack;
    private bool _isMoving;
    private float _playbackSpeed = 1f;
    private bool IsFrozen => !_status.IsDead && !_status.CanMove;
    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Walk = Animator.StringToHash("Walk");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Cast = Animator.StringToHash("Cast");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Death = Animator.StringToHash("Death");

    private void Awake()
    {
        if (_controller == null) _controller = GetComponent<ReaperController>();
        if (_status == null) _status = GetComponent<EnemyStatus>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();
        _rigid = GetComponent<Rigidbody2D>();
        _movement = GetComponent<EnemyMovementModule>();
        _playbackSpeed = _animator.speed;
    }
    private void OnEnable()
    {
        _playing = 0;
        _hurtUntil = 0f;
        _isMoving = _wasKnockedBack = false;
        _lastPosition = _rigid != null ? _rigid.position : (Vector2)transform.position;
        _animator.speed = _playbackSpeed;
        _status.OnDamaged += OnDamaged;
    }
    private void OnDisable()
    {
        _status.OnDamaged -= OnDamaged;
        _animator.speed = _playbackSpeed;
    }
    private void OnDamaged()
    {
        if (IsFrozen) return;
        if (_controller.Action == ReaperController.ActionState.Idle || _controller.Action == ReaperController.ActionState.Walk)
            _hurtUntil = Time.time + 0.3f;
    }
    private void FixedUpdate()
    {
        Vector2 position = _rigid != null ? _rigid.position : (Vector2)transform.position;
        Vector2 delta = position - _lastPosition;
        _lastPosition = position;
        bool knockedBack = _movement != null && _movement.IsKnockedBack;
        _isMoving = !knockedBack && !_wasKnockedBack && delta.sqrMagnitude > 0.01f * Time.fixedDeltaTime * Time.fixedDeltaTime;
        _wasKnockedBack = knockedBack;
    }

    private void Update()
    {
        _animator.speed = IsFrozen ? 0f : _playbackSpeed;
        if (IsFrozen) return;
        int state = _controller.Action switch
        {
            ReaperController.ActionState.Death => Death,
            ReaperController.ActionState.Melee => Attack,
            ReaperController.ActionState.Cast => Cast,
            ReaperController.ActionState.Steal => Cast,
            ReaperController.ActionState.Dash => _isMoving ? Walk : Idle,
            ReaperController.ActionState.Walk => Time.time < _hurtUntil ? Hurt : _isMoving ? Walk : Idle,
            ReaperController.ActionState.Idle => Time.time < _hurtUntil ? Hurt : Idle,
            ReaperController.ActionState.Recovery => _controller.RecoveryFrom == ReaperController.ActionState.Melee ? Attack : Idle,
            _ => Idle
        };
        if (_playing != state) { _playing = state; _animator.Play(state, 0, 0f); }
        if (Mathf.Abs(_controller.Facing.x) > 0.001f) _sprite.flipX = _controller.Facing.x > 0f;
    }
    private void LateUpdate()
    {
        if (_sprite.sprite == null) return;
        Vector2 offset = (_sprite.sprite.rect.center - _bodyAnchorPixels) / _sprite.sprite.pixelsPerUnit;
        if (_sprite.flipX) offset.x = -offset.x;
        // Visual만 확대해도 몸 기준점은 루트 위치에 고정한다.
        _sprite.transform.localPosition = Vector3.Scale(new Vector3(offset.x, offset.y, 0f), _sprite.transform.localScale);
    }
}
