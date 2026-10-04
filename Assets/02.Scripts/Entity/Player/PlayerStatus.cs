using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IThirstable, IDamageable, IDirectionalDamageable
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
    [SerializeField, Min(0f)] private float _initThirstIncreaseAmount = 1f; //초당 감소 정도
    [SerializeField] private float _initThirstIncreaseInterval = 10f; //감소 간격
    [SerializeField] float _thirstWarningThreshold = 0.7f;
    [SerializeField] float _thirstCriticalThreshold = 1f;
    [SerializeField] private EntityThirst _thirstInstance;

    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashSpeed = 15f;

    [Header("Throw Settings")]
    [SerializeField] private float _maxThrowDistance = 6f;

    // Receiver는 상태값을 제공하고, 플레이어의 최종 이동 속도는 여기서 결정한다.
    private bool HasPotionMovementEffect => _potionEffectReceiver != null && _potionEffectReceiver.isActiveAndEnabled;
    public bool CanMove => !IsDead && (!HasPotionMovementEffect || _potionEffectReceiver.CanMove);
    public float Speed => !CanMove ? 0f : _speed * (HasPotionMovementEffect ? _potionEffectReceiver.MoveSpeedMultiplier : 1f);
    // 둔화는 대시에 적용하지 않고, 빙결/마비에 의한 정지만 반영
    public float DashSpeed => CanMove ? _dashSpeed : 0f;
    public float MaxThrowDistance => _maxThrowDistance;

    public float CurrentHealth => _healthInstance.Current;
    public float MaxHealth => _healthInstance.Max;
    public float CurrentThirst => _thirstInstance.CurrentThirst;
    public float MaxThirst => _thirstInstance.MaxThirst;
    public bool IsDead => _healthInstance.IsDead;
    public ThirstWarningLevel CurrentThirstLevel { get; private set; } = ThirstWarningLevel.Normal;

    public event Action<float, float> OnThirstChanged; //current, max
    public event Action<float, float> OnHealthChanged; //current, max
    public event Action OnDeath;
    public event Action<float, Vector2> OnDamaged;
    public event Action<ThirstWarningLevel, ThirstWarningLevel> OnThirstLevelChanged; //prevLevel, nextLevel

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
        _thirstInstance = new EntityThirst(_initCurrentThirst, _initMaxThirst, _initThirstIncreaseAmount, _initThirstIncreaseInterval);
        _healthInstance = new EntityHealth(_initHealth, _maxHealth);

        CurrentThirstLevel = EvaluateThirstLevel(_thirstInstance.CurrentThirst, _thirstInstance.MaxThirst);
    }

    private void Update()
    {
        if (_thirstInstance.IncreaseTick(Time.deltaTime))
        {
            NotifyThirstChanged();
        }
    }

    public void RestoreHealth(float amount)
    {
        _healthInstance.RestoreHealth(amount);
        OnHealthChanged?.Invoke(_healthInstance.Current, _healthInstance.Max);
    }

    public void DecreaseHealth(float amount)
    {
        DecreaseHealth(amount, Vector2.zero);
    }

    public void DecreaseHealth(float amount, Vector2 hitDirection)
    {
        if (IsDead) return;

        _healthInstance.DecreaseHealth(amount);
        OnHealthChanged?.Invoke(_healthInstance.Current, _healthInstance.Max);

        if (amount > 0f)
        {
            OnDamaged?.Invoke(amount, hitDirection);
        }

        if (_healthInstance.IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    public void RestoreThirst(float amount)
    {
        _thirstInstance.RestoreThirst(amount);
        NotifyThirstChanged();
    }

    public void DecreaseThirst(float amount)
    {
        if (IsDead) return;

        _thirstInstance.DecreaseThirst(amount);

        NotifyThirstChanged();
    }

    private void NotifyThirstChanged()
    {
        float current = _thirstInstance.CurrentThirst;
        float maximum = _thirstInstance.MaxThirst;

        ThirstWarningLevel prevLevel = CurrentThirstLevel;
        ThirstWarningLevel nextLevel = EvaluateThirstLevel(current, maximum);

        CurrentThirstLevel = nextLevel;
        OnThirstChanged?.Invoke(current, maximum);

        if(nextLevel != prevLevel)
        {
            OnThirstLevelChanged?.Invoke(prevLevel, nextLevel);
        }
    }

    private ThirstWarningLevel EvaluateThirstLevel(float current, float maximum)
    {
        if (maximum <= Mathf.Epsilon)
        {
            return ThirstWarningLevel.Normal;
        }

        float normalizedThirst = Mathf.Clamp01(current / maximum);

        if (normalizedThirst >= _thirstCriticalThreshold)
        {
            return ThirstWarningLevel.Critical;
        }

        if (normalizedThirst >= _thirstWarningThreshold)
        {
            return ThirstWarningLevel.Warning;
        }

        return ThirstWarningLevel.Normal;
    }
}
