using UnityEngine;

public class PlayerMovementModule : MonoBehaviour
{
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private Rigidbody2D _rigid;
    [SerializeField] private Collider2D _collider;

    private void Awake()
    {
        if(_playerStatus == null)
        {
            _playerStatus = GetComponent<PlayerStatus>();
        }
        if(_rigid == null)
        {
            _rigid = GetComponent<Rigidbody2D>();
        }
        if(_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }
    }
    public void MovePosition(Vector2 moveInput)
    {
        _rigid.MovePosition(_rigid.position + moveInput * _playerStatus.Speed * Time.fixedDeltaTime);
    }
}
