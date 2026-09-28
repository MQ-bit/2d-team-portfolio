using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PotionEffectReceiver : MonoBehaviour // player Health와 상태 관리자에 효과 전달
{
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private Health _health;
    [SerializeField] private PlayerStatus _playerStatus;

    private readonly PotionStatusEffectState _statusEffects = new();

    public bool IsBurning => _statusEffects.IsActive(PotionStatusEffect.Burning);
    public bool IsPoisoned => _statusEffects.IsActive(PotionStatusEffect.Poisoned);
    public bool IsFrozen => _statusEffects.IsActive(PotionStatusEffect.Frozen);
    public bool IsParalyzed => _statusEffects.IsActive(PotionStatusEffect.Paralyzed);
    public bool CanAct => _statusEffects.CanAct;

    private void Awake()
    {
        if (_health == null)
        {
            _health = GetComponent<Health>();
        }

        if (_playerStatus == null)
        {
            _playerStatus = GetComponent<PlayerStatus>();
        }
    }

    public void ModifyHealth(float amount)
    {
        if (_health == null)
        {
            Debug.LogWarning($"[PotionEffectReceiver] Health is missing: target={name}", this);
            return;
        }

        float previousHealth = _health.Current;
        _health.Modify(amount);
        if (_enableDebugLogs)
        {
            Debug.Log(
                $"[PotionEffectReceiver] Health changed: target={name}, " +
                $"amount={amount}, {previousHealth} -> {_health.Current}",
                this);
        }
    }


    public void ApplyStatus(PotionStatusEffect status, float duration)
    {
        _statusEffects.Apply(status, duration);
    }
}
