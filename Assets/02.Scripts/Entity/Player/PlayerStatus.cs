using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Potion Effect")]
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;

    [Header("Player Thirst")]
    [SerializeField, Min(1f)] private float _maxThirst = 100f;
    [SerializeField, Min(0f)] private float _currentThirst = 100f;

    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooltime = 1f;

    [Header("Throw Settings")]
    [SerializeField] private float _maxThrowDistance = 6f;

    public event Action<float, float> ThirstChanged;

    public float CurrentThirst => _currentThirst;
    public float MaxThirst => _maxThirst;
    public bool CanMove => _potionEffectReceiver == null || _potionEffectReceiver.CanAct;
    public float Speed => CanMove ? _speed : 0f;
    public float DashSpeed => CanMove ? _dashSpeed : 0f;
    public float DashDuration => _dashDuration;
    public float DashCooltime => _dashCooltime;
    public float MaxThrowDistance => _maxThrowDistance;

    private void Awake()
    {
        _currentThirst = Mathf.Clamp(_currentThirst, 0f, _maxThirst);

        if (_potionEffectReceiver == null)
        {
            _potionEffectReceiver = GetComponent<PotionEffectReceiver>();
        }
    }

    public void RestoreThirst(float amount)
    {
        _currentThirst = Mathf.Clamp(_currentThirst + Mathf.Max(0f, amount), 0f, _maxThirst);
        ThirstChanged?.Invoke(_currentThirst, _maxThirst);
    }
}
