using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IThirstable
{
    [Header("Component References")]
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;

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
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private float _dashCooltime = 1f;

    [Header("Throw Settings")]
    [SerializeField] private float _maxThrowDistance = 6f;

    public bool CanMove => _potionEffectReceiver.CanAct;
    public float Speed => CanMove ? _speed : 0f;
    public float DashSpeed => CanMove ? _dashSpeed : 0f;
    public float DashDuration => _dashDuration;
    public float DashCooltime => _dashCooltime;
    public float MaxThrowDistance => _maxThrowDistance;

    public event Action<float, float> OnThirstChanged; //current, max


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
        //_health = new EntityHealth(_initHealth, _maxHealth);
    }

    private void Update()
    {
        if (_thirstStatInstance.IncreaseTick(Time.deltaTime))
        {
            OnThirstChanged?.Invoke(_thirstStatInstance.CurrentThirst, _thirstStatInstance.MaxThirst);
        }
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
        _thirstStatInstance.RestoreThirst(amount);       

        OnThirstChanged?.Invoke(_thirstStatInstance.CurrentThirst, _thirstStatInstance.MaxThirst);
    }

    public void DecreaseThirst(float amount)
    {
        _thirstStatInstance.DecreaseThirst(amount);

        OnThirstChanged?.Invoke(_thirstStatInstance.CurrentThirst, _thirstStatInstance.MaxThirst);
    }
}
