using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PotionEffectReceiver : MonoBehaviour, IPotionEffectTarget
{
    [SerializeField] private bool _enableDebugLogs = true;
    [SerializeField] private Health _health;
    [SerializeField] private PlayerStatus _playerStatus;

    private float _frozenUntil;
    private float _paralyzedUntil;
    private float _burningUntil;
    private float _poisonedUntil;

    public bool IsBurning => Time.time < _burningUntil;
    public bool IsPoisoned => Time.time < _poisonedUntil;
    public bool IsFrozen => Time.time < _frozenUntil;
    public bool IsParalyzed => Time.time < _paralyzedUntil;
    public bool CanAct => !IsFrozen && !IsParalyzed;

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

    public void RestoreThirst(float amount)
    {
        _playerStatus?.RestoreThirst(amount);
    }

    public void ApplyStatus(PotionStatusEffect status, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        float expiresAt = Time.time + duration;
        switch (status)
        {
            case PotionStatusEffect.Burning:
                _burningUntil = Mathf.Max(_burningUntil, expiresAt);
                break;
            case PotionStatusEffect.Poisoned:
                _poisonedUntil = Mathf.Max(_poisonedUntil, expiresAt);
                break;
            case PotionStatusEffect.Frozen:
                _frozenUntil = Mathf.Max(_frozenUntil, expiresAt);
                break;
            case PotionStatusEffect.Paralyzed:
                _paralyzedUntil = Mathf.Max(_paralyzedUntil, expiresAt);
                break;
        }
    }
}
