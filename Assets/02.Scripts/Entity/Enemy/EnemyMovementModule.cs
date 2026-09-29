using UnityEngine;

public class EnemyMovementModule : MonoBehaviour, IKnockbackable
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D _rigid;

    [Header("Knockback")]
    [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.15f; // direction * force 만큼 밀려나는 데 걸리는 시간

    private float _knockbackEndTime;
    private bool _isKnockedBack;

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
        _knockbackEndTime = Time.time + _knockbackDuration;
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

        if (Time.time < _knockbackEndTime)
        {
            return true;
        }

        _isKnockedBack = false;
        _rigid.linearVelocity = Vector2.zero;
        return false;
    }
}
