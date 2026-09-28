using UnityEngine;

[DisallowMultipleComponent]
public sealed class PotionEffectReceiver : MonoBehaviour, IPotionEffectTarget
{
    [SerializeField] private PotionTargetKind _targetKind = PotionTargetKind.Enemy;
    [SerializeField] private PlayerStatus _playerStatus;

    private readonly PotionStatusEffectState _statusEffects = new();

    public PotionTargetKind TargetKind => _targetKind;
    public IDamageable Damageable { get; private set; }
    public IThirstable Thirstable { get; private set; }
    public bool CanAct => _statusEffects.CanAct;

    private void Awake()
    {
        Damageable = GetComponentInParent<IDamageable>();
        Thirstable = GetComponentInParent<IThirstable>();

        if (_targetKind == PotionTargetKind.Player && _playerStatus == null)
        {
            _playerStatus = GetComponent<PlayerStatus>();
        }
    }

    public void ClearStatuses()
    {
        _statusEffects.Clear();
    }

    public void ApplyStatus(PotionStatusEffect status, float duration, int sourceId)
    {
        _statusEffects.Apply(status, duration, sourceId);
    }

    public int GetStatusStackCount(PotionStatusEffect status)
    {
        return _statusEffects.GetStackCount(status);
    }
}
