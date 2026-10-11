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
    }
    private void OnEnable() { _playing = 0; _hurtUntil = 0f; _status.OnDamaged += OnDamaged; }
    private void OnDisable() => _status.OnDamaged -= OnDamaged;
    private void OnDamaged()
    {
        if (_controller.Action == ReaperController.ActionState.Idle || _controller.Action == ReaperController.ActionState.Walk)
            _hurtUntil = Time.time + 0.3f;
    }
    private void Update()
    {
        int state = _controller.Action switch
        {
            ReaperController.ActionState.Death => Death,
            ReaperController.ActionState.Melee => Attack,
            ReaperController.ActionState.Cast => Cast,
            ReaperController.ActionState.Steal => Cast,
            ReaperController.ActionState.Dash => Walk,
            ReaperController.ActionState.Walk => Time.time < _hurtUntil ? Hurt : Walk,
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
        _sprite.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
    }
}
