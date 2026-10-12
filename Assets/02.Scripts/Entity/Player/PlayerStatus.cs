using System;
using UnityEngine;

public class PlayerStatus : MonoBehaviour, IThirstable, IDamageable, IDirectionalDamageable
{
    [Header("Component References")]
    [SerializeField] private PotionEffectReceiver _potionEffectReceiver;

    [Header("Player Data")]
    [SerializeField] private PlayerData _data;
    public PlayerData Data => _data;
    private EntityHealth _healthInstance;
    private float _nextContactDamageTime = float.NegativeInfinity;
    private EntityThirst _thirstInstance;

    // Receiver는 상태값을 제공하고, 플레이어의 최종 이동 속도는 여기서 결정한다.
    private bool HasPotionMovementEffect => _potionEffectReceiver != null && _potionEffectReceiver.isActiveAndEnabled;
    public bool CanMove => !IsDead && (!HasPotionMovementEffect || _potionEffectReceiver.CanMove);
    public float Speed => !CanMove ? 0f : Data.Movement.Speed * (HasPotionMovementEffect ? _potionEffectReceiver.MoveSpeedMultiplier : 1f);
    // 둔화는 대시에 적용하지 않고, 빙결/마비에 의한 정지만 반영
    public float DashSpeed => CanMove ? Data.Dash.Speed : 0f;
    public float MaxThrowDistance => Data.Throw.MaxDistance;

    public float CurrentHealth => _healthInstance.Current;
    public float MaxHealth => _healthInstance.Max;
    public float CurrentThirst => _thirstInstance.CurrentThirst;
    public float MaxThirst => _thirstInstance.MaxThirst;
    public bool IsDead => _healthInstance == null || _healthInstance.IsDead;
    public ThirstWarningLevel CurrentThirstLevel { get; private set; } = ThirstWarningLevel.Normal;

    public event Action<float, float> OnThirstChanged; //current, max
    public event Action<float, float> OnHealthChanged; //current, max
    public event Action OnDeath;
    public event Action<float, Vector2> OnDamaged;
    public event Action<ThirstWarningLevel, ThirstWarningLevel> OnThirstLevelChanged; //prevLevel, nextLevel

    private void Awake()
    {
        if (_data == null)
        {
            Debug.LogError($"{name}: PlayerData SO가 필요합니다.", this);
            enabled = false;
            return;
        }
        InitiateStatus();

        if (_potionEffectReceiver == null)
        {
            _potionEffectReceiver = GetComponent<PotionEffectReceiver>();
        }
    }

    private void InitiateStatus()
    {
        PlayerData.ThirstSettings thirst = Data.Thirst;
        _thirstInstance = new EntityThirst(thirst.InitialValue, thirst.MaxValue,
            thirst.IncreaseAmount, thirst.IncreaseInterval);
        _healthInstance = new EntityHealth(Data.Health.InitialHealth, Data.Health.MaxHealth);
        _nextContactDamageTime = float.NegativeInfinity;

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

    public bool TryTakeContactDamage(float amount, Vector2 hitDirection)
    {
        if (amount <= 0f || IsDead || Time.time < _nextContactDamageTime) return false;
        // 적별 타이머가 아닌 플레이어 공통 타이머로 다중 접촉 피해를 제한한다.
        _nextContactDamageTime = Time.time + Data.Health.ContactDamageInterval;
        DecreaseHealth(amount, hitDirection);
        return true;
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

        if (nextLevel != prevLevel)
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

        if (normalizedThirst >= Data.Thirst.CriticalRatio)
        {
            return ThirstWarningLevel.Critical;
        }

        if (normalizedThirst >= Data.Thirst.WarningRatio)
        {
            return ThirstWarningLevel.Warning;
        }

        return ThirstWarningLevel.Normal;
    }
}
