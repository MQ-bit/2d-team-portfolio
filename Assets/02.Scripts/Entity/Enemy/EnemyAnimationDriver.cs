using UnityEngine;

// 적의 이동/피격 상태를 Animator(자식 Visual)에 전달한다. 컨트롤러 상태에 직접 접근하지 않고 관찰만 한다.
// - 이동: FixedUpdate마다 Rigidbody2D 위치 변화로 판단 (MovePosition은 velocity를 남기지 않으므로)
// - 방향: 가로 이동 방향에 따라 스프라이트를 좌우 반전 (에셋은 오른쪽을 바라봄)
// - 피격: EnemyStatus.OnHealthChanged에서 체력이 줄었을 때 Hurt 트리거
public class EnemyAnimationDriver : MonoBehaviour
{
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    [Header("References")]
    [SerializeField] private EnemyStatus _status;
    [SerializeField] private Rigidbody2D _rigid;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveThreshold = 0.1f; // 초당 이동량이 이 값보다 커야 걷는 것으로 본다.

    private Vector2 _lastPosition;
    private float _lastHealth = float.MaxValue;

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

        if (_rigid != null)
        {
            _lastPosition = _rigid.position;
        }
    }

    private void OnEnable()
    {
        if (_status != null)
        {
            _status.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (_status != null)
        {
            _status.OnHealthChanged -= HandleHealthChanged;
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

        float speed = delta.magnitude / Time.fixedDeltaTime;
        _animator.SetBool(IsMovingHash, speed > _moveThreshold);

        if (_spriteRenderer != null && Mathf.Abs(delta.x) > 0.0001f)
        {
            _spriteRenderer.flipX = delta.x < 0f;
        }
    }

    // 회복할 때도 OnHealthChanged가 불리므로 체력이 줄었을 때만 재생한다.
    private void HandleHealthChanged(float current, float max)
    {
        bool isDamaged = current < _lastHealth;
        _lastHealth = current;

        if (!isDamaged || current <= 0f || _animator == null)
        {
            return;
        }

        _animator.SetTrigger(HurtHash);
    }
}
