using UnityEngine;

public class EnemyMovementModule : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D _rigid;

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
        Vector2 nextPosition = Vector2.MoveTowards(_rigid.position, targetPosition, speed * Time.fixedDeltaTime);
        _rigid.MovePosition(nextPosition);
    }

    public void Stop()
    {
        _rigid.linearVelocity = Vector2.zero;
    }
}
