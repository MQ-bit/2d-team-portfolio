using System;
using UnityEngine;
using UnityEngine.Serialization;

// 공유하는 기본 설정만 저장한다. 현재 HP/금단, 행동 상태와 타이머는 플레이어 인스턴스가 관리한다.
[CreateAssetMenu(fileName = "PlayerData", menuName = "Player/Player Data")]
public sealed class PlayerData : ScriptableObject
{
    [Serializable]
    public sealed class HealthSettings
    {
        [SerializeField, Min(1f)] private float _maxHealth = 400f;
        [SerializeField, Min(0f)] private float _initialHealth = 400f;
        [SerializeField, Min(0.01f)] private float _contactDamageInterval = 0.5f;
        public float MaxHealth => Mathf.Max(1f, _maxHealth);
        public float InitialHealth => Mathf.Clamp(_initialHealth, 0f, MaxHealth);
        public float ContactDamageInterval => Mathf.Max(0.01f, _contactDamageInterval);
    }

    [Serializable]
    public sealed class ThirstSettings
    {
        [SerializeField, Min(1f)] private float _maxValue = 100f;
        [SerializeField, Min(0f)] private float _initialValue = 15f;
        [SerializeField, Min(0f)] private float _increaseAmount = 1f;
        [SerializeField, Min(0.01f)] private float _increaseInterval = 1.25f;
        [SerializeField, Range(0f, 1f)] private float _warningRatio = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _criticalRatio = 1f;
        public float MaxValue => Mathf.Max(1f, _maxValue);
        public float InitialValue => Mathf.Clamp(_initialValue, 0f, MaxValue);
        public float IncreaseAmount => Mathf.Max(0f, _increaseAmount);
        public float IncreaseInterval => Mathf.Max(0.01f, _increaseInterval);
        public float WarningRatio => Mathf.Clamp01(_warningRatio);
        public float CriticalRatio => Mathf.Clamp(_criticalRatio, WarningRatio, 1f);
    }

    [Serializable]
    public sealed class MovementSettings
    {
        [SerializeField, Min(0f)] private float _speed = 5f;
        public float Speed => Mathf.Max(0f, _speed);
    }

    [Serializable]
    public sealed class DashSettings
    {
        [SerializeField, Min(0f)] private float _speed = 30f;
        [SerializeField, Min(0.01f)] private float _duration = 0.2f;
        [SerializeField, Min(0f)] private float _cooldown = 1f;
        public float Speed => Mathf.Max(0f, _speed);
        public float Duration => Mathf.Max(0.01f, _duration);
        public float Cooldown => Mathf.Max(0f, _cooldown);
    }

    [Serializable]
    public sealed class MeleeSettings
    {
        [SerializeField, Min(0f)] private float _damage = 30f;
        [SerializeField, Min(0f)] private float _range = 3f;
        [SerializeField, Range(0f, 360f)] private float _angle = 90f;
        [SerializeField, Min(0f)] private float _cooldown = 0.5f;
        [SerializeField, Min(0.01f)] private float _swingDuration = 0.25f;
        [Tooltip("수락한 근접 공격 시작1회당 금단 증가량. 헛스윙도 포함합니다.")]
        [SerializeField, Min(0f)] private float _withdrawalCost = 3f;
        [SerializeField, Min(0f)] private float _knockbackForce = 1f;
        public float Damage => Mathf.Max(0f, _damage);
        public float Range => Mathf.Max(0f, _range);
        public float Angle => Mathf.Clamp(_angle, 0f, 360f);
        public float Cooldown => Mathf.Max(0f, _cooldown);
        public float SwingDuration => Mathf.Max(0.01f, _swingDuration);
        public float WithdrawalCost => Mathf.Max(0f, _withdrawalCost);
        public float KnockbackForce => Mathf.Max(0f, _knockbackForce);
    }

    [Serializable]
    public sealed class ThrowSettings
    {
        [SerializeField, Min(0f)] private float _maxDistance = 6f;
        [SerializeField, Min(0.01f)] private float _flightDuration = 1f;
        [SerializeField, Min(0f)] private float _arcHeight = 1.5f;
        [SerializeField] private float _rotationSpeed = 360f;
        public float MaxDistance => Mathf.Max(0f, _maxDistance);
        public float FlightDuration => Mathf.Max(0.01f, _flightDuration);
        public float ArcHeight => Mathf.Max(0f, _arcHeight);
        public float RotationSpeed => _rotationSpeed;
    }

    [Serializable]
    public sealed class DrinkSettings
    {
        [SerializeField, Min(0.01f)] private float _duration = 0.5f;
        [Tooltip("기존 포션별 금단 변화에 추가되는 공통 감소량입니다.")]
        [SerializeField, Min(0f)] private float _baseWithdrawalDecrease = 10f;
        public float Duration => Mathf.Max(0.01f, _duration);
        public float BaseWithdrawalDecrease => Mathf.Max(0f, _baseWithdrawalDecrease);
    }

    [Serializable]
    public sealed class CollectionSettings
    {
        [Tooltip("수집 센서의 로컬 반경. 월드 반경에는 센서 Transform 스케일이 한 번 적용됩니다.")]
        [SerializeField, Min(0f)] private float _localRadius = 3f;
        public float LocalRadius => Mathf.Max(0f, _localRadius);
    }

    [Serializable]
    public sealed class InteractionSettings
    {
        [SerializeField, Min(0.01f)] private float _holdDuration = 1f;
        public float HoldDuration => Mathf.Max(0.01f, _holdDuration);
    }

    [SerializeField] private HealthSettings _health = new();
    [FormerlySerializedAs("_withdrawal")]
    [FormerlySerializedAs("thirst")]
    [SerializeField] private ThirstSettings _thirst = new();
    [SerializeField] private MovementSettings _movement = new();
    [SerializeField] private DashSettings _dash = new();
    [SerializeField] private MeleeSettings _melee = new();
    [SerializeField] private ThrowSettings _throw = new();
    [SerializeField] private DrinkSettings _drink = new();
    [SerializeField] private CollectionSettings _collection = new();
    [SerializeField] private InteractionSettings _interaction = new();

    public HealthSettings Health => _health;
    public ThirstSettings Thirst => _thirst;
    public MovementSettings Movement => _movement;
    public DashSettings Dash => _dash;
    public MeleeSettings Melee => _melee;
    public ThrowSettings Throw => _throw;
    public DrinkSettings Drink => _drink;
    public CollectionSettings Collection => _collection;
    public InteractionSettings Interaction => _interaction;
}
