using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashDuration = 0.2f; //대쉬 시간
    [SerializeField] private float _dashCooltime = 1f;

    [Header("Throw Settings")]
    [SerializeField] private float _maxThrowDistance = 6f;

    public float Speed => _speed;
    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public float DashCooltime => _dashCooltime;
    public float MaxThrowDistance => _maxThrowDistance;
}
