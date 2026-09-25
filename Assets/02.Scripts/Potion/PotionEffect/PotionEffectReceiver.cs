using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public sealed class PotionEffectReceiver : MonoBehaviour, IPotionEffectTarget
{
    [SerializeField] private PotionTargetKind _targetKind = PotionTargetKind.Enemy;
    [SerializeField] private Health _health;
    [SerializeField] private PlayerStatus _playerStatus;

    private float _frozenUntil;
    private float _paralyzedUntil;
    private float _blindedUntil;
    private float _revealedUntil;
    private float _burningUntil;
    private float _poisonedUntil;

    public PotionTargetKind TargetKind => _targetKind;
    public Health Health => _health;
    public bool IsBurning => Time.time < _burningUntil;
    public bool IsPoisoned => Time.time < _poisonedUntil;
    public bool IsFrozen => Time.time < _frozenUntil;
    public bool IsParalyzed => Time.time < _paralyzedUntil;
    public bool IsBlinded => Time.time < _blindedUntil;
    public bool IsRevealed => Time.time < _revealedUntil;
    public bool CanAct => !IsFrozen && !IsParalyzed;

    private void Awake()
    {
        if (_health == null)
        {
            _health = GetComponent<Health>();
        }

        if (_targetKind == PotionTargetKind.Player && _playerStatus == null)
        {
            _playerStatus = GetComponent<PlayerStatus>();
        }
    }

    public void ModifyHealth(float amount)
    {
        _health?.Modify(amount);
    }

    public void RestoreThirst(float amount)
    {
        _playerStatus?.RestoreThirst(amount);
    }

    public void ApplyStatus(PotionStatusEffect status, float duration)
    {
        float expiresAt = Time.time + Mathf.Max(0f, duration);
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
            case PotionStatusEffect.Blinded:
                _blindedUntil = Mathf.Max(_blindedUntil, expiresAt);
                break;
            case PotionStatusEffect.Revealed:
                _revealedUntil = Mathf.Max(_revealedUntil, expiresAt);
                break;
        }
    }
}
