using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IThirstable, IDamageable
{
    [Header("Component References")]
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;

    [Header("Player Health Settings")]
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [SerializeField, Min(0f)] private float _initHealth = 100f;
    [SerializeField] private EntityHealth _healthInstance;


    [Header("Player Thirst Settings")] //TODO: 하나의 ScriptableObject로 두는게..?
    [SerializeField, Min(1f)] private float _initMaxThirst = 100f;
    [SerializeField, Min(0f)] private float _initCurrentThirst = 100f;
    [SerializeField, Min(0f)] private float _itnitThirstIncreaseAmount = 1f; //초당 감소 정도
    [SerializeField] private float _initThirstIncreaseInterval = 10f; //감소 간격
    [SerializeField] private EntityThirst _thirstStatInstance;

    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 15f;

    [Header("Throw Settings")]
    [SerializeField] private float _maxThrowDistance = 6f;

    public bool CanMove => _potionEffectReceiver == null || _potionEffectReceiver.CanMove;
    public float Speed => _potionEffectReceiver != null
        ? _potionEffectReceiver.GetMovementSpeed(_speed) : _speed;
    public float DashSpeed => _potionEffectReceiver != null
        ? _potionEffectReceiver.GetDashSpeed(_dashSpeed) : _dashSpeed;
    public float MaxThrowDistance => _maxThrowDistance;

    public bool IsDead => _healthInstance.IsDead;
    public float CurrentHealth => _healthInstance.Current;

    public event Action<float, float> OnThirstChanged; //current, max
    public event Action<float, float> OnHealthChanged; //current, max
    public event Action OnDeath;

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
        _thirstStatInstance = new EntityThirst(_initCurrentThirst, _initMaxThirst, _itnitThirstIncreaseAmount, _initThirstIncreaseInterval);
        _healthInstance = new EntityHealth(_initHealth, _maxHealth);
    }

    private void Update()
    {
        if (_thirstStatInstance.IncreaseTick(Time.deltaTime))
        {
            OnThirstChanged?.Invoke(_thirstStatInstance.CurrentThirst, _thirstStatInstance.MaxThirst);
        }
    }

    public void RestoreHealth(float amount)
    {
        _healthInstance.RestoreHealth(amount);
        OnHealthChanged?.Invoke(_healthInstance.Current, _healthInstance.Max);
    }

    public void DecreaseHealth(float amount)
    {
        _healthInstance.DecreaseHealth(amount);
        OnHealthChanged?.Invoke(_healthInstance.Current, _healthInstance.Max);

        if (_healthInstance.IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    public void RestoreThirst(float amount)
    {
        _thirstStatInstance.RestoreThirst(amount);

        OnThirstChanged?.Invoke(_thirstStatInstance.CurrentThirst, _thirstStatInstance.MaxThirst);
    }

    public void DecreaseThirst(float amount)
    {
        _thirstStatInstance.DecreaseThirst(amount);

        OnThirstChanged?.Invoke(_thirstStatInstance.CurrentThirst, _thirstStatInstance.MaxThirst);
    }
}
