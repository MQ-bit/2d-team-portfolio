using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IThirstable
{
    [Header("Potion Effect Receiver")]
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;

    [Header("Player Thirst Settings")] //인터페이스 분리?
    [SerializeField, Min(1f)] private float _maxThirst = 100f;
    [SerializeField, Min(0f)] private float _initThirst = 100f;
    [SerializeField] private EntityThirst _thirstInstance;

    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 15f;
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooltime = 1f;

    [Header("Throw Settings")]
    [SerializeField] private float _maxThrowDistance = 6f;

    public float CurrentThirst => _thirstInstance.CurrentThirst;
    public float MaxThirst => _thirstInstance.MaxThirst;

    public bool CanMove => _potionEffectReceiver == null || _potionEffectReceiver.CanAct;
    public float Speed => CanMove ? _speed : 0f;
    public float DashSpeed => CanMove ? _dashSpeed : 0f;
    public float DashDuration => _dashDuration;
    public float DashCooltime => _dashCooltime;
    public float MaxThrowDistance => _maxThrowDistance;

    public event Action<float, float, float> OnThirstChanged; //previous, current, max


    private void Awake()
    {
        InitiateStatus();

        if (_potionEffectReceiver == null)
        {
            _potionEffectReceiver = GetComponent<PotionEffectReceiver>();
        }
    }

    private void InitiateStatus()
    {
        _thirstInstance = new EntityThirst(_initThirst, _maxThirst);
        //_health = new EntityHealth(_initHealth, _maxHealth);
    }


    // public void RestoreHealth(float amount)
    // {
    //     _health.RestoreHealth(amount);
    // }

    // public void DecreaseHealth(float amount)
    // {
    //     _health.DecreaseHealth(amount);
    // }

    public void RestoreThirst(float amount)
    {
        float previousThirst = _thirstInstance.CurrentThirst;
        _thirstInstance.RestoreThirst(amount);       

        OnThirstChanged?.Invoke(previousThirst, _thirstInstance.CurrentThirst, _thirstInstance.MaxThirst);
    }

    public void DecreaseThirst(float amount)
    {
        float previousThirst = _thirstInstance.CurrentThirst;
        _thirstInstance.DecreaseThirst(amount);

        OnThirstChanged?.Invoke(previousThirst, _thirstInstance.CurrentThirst, _thirstInstance.MaxThirst);
    }
}
