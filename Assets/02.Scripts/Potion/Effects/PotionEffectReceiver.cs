using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PotionEffectReceiver : MonoBehaviour, IPotionEffectTarget
{
    [SerializeField] private PotionTargetKind _targetKind = PotionTargetKind.Enemy;

    private readonly PotionStatusEffectState _statusEffects = new();
    private readonly PotionAreaEffectTracker _areaEffects = new();

    public PotionTargetKind TargetKind => _targetKind;
    public IDamageable Damageable { get; private set; }
    public IThirstable Thirstable { get; private set; }
    public bool CanMove => _statusEffects.CanMove;
    public float MoveSpeedMultiplier => _statusEffects.MoveSpeedMultiplier;
    public int FrostStackCount => _statusEffects.FrostStackCount;
    public event Action<PotionDamageResult> StatusDamageApplied;
    public event Action<PotionDamageResult> DamageApplied;

    private void Awake()
    {
        Damageable = GetComponentInParent<IDamageable>();
        Thirstable = GetComponentInParent<IThirstable>();
    }

    public void ClearStatuses()
    {
        _statusEffects.Clear();
        RefreshDamageSuppression();
    }

    public void EnterFloor(IPotionEffectData profile, PotionEffectSource source)
    {
        if (!CanReceiveStatus()) return;
        TickStatuses();
        _areaEffects.Enter(profile, source);
        foreach (var effect in profile.GetEffects(PotionEffectPhase.FloorTick))
        {
            if (effect.Operation == PotionEffectOperation.AddFrostStacks)
                EnterFrostArea(source.SourceId);
        }

        RefreshDamageSuppression();
    }

    public void PulseFloor(int sourceId)
    {
        if (CanReceiveStatus()) _areaEffects.Pulse(sourceId, Time.timeAsDouble, ApplyEffect);
    }

    public void ExitFloor(int sourceId, bool applyResidual)
    {
        if (!_areaEffects.Exit(sourceId, out var profile, out var source)) return;
        // 이탈 시점 이전의 억제된 DoT를 먼저 소비하여 밀린 피해가 튀어나오지 않게 한다.
        TickStatuses();
        RemoveAreaEffectsFromSource(sourceId);
        RefreshDamageSuppression();
        if (!applyResidual || !CanReceiveStatus()) return;
        foreach (var effect in profile.GetEffects(PotionEffectPhase.FloorExit))
        {
            if (!_areaEffects.HasDamageElement(effect.Element))
                ApplyEffect(effect, profile, source);
        }
    }

    public void ApplyEffect(PotionEffectDefinition effect, IPotionEffectData profile, PotionEffectSource source)
    {
        if (!CanReceiveStatus()) return;
        switch (effect.Operation)
        {
            case PotionEffectOperation.Damage:
                ApplyDamage(effect.Amount, source, effect.Element, false);
                break;
            case PotionEffectOperation.Heal:
                PotionStatDispatcher.Heal(Damageable, effect.Amount);
                break;
            case PotionEffectOperation.ApplyStatus:
                ApplyStatus(effect, source);
                break;
            case PotionEffectOperation.AddFrostStacks:
                AddFrostStacks(profile.Frost, source, effect.FrostStackAmount, profile.TickInterval);
                break;
        }
    }

    private void RefreshDamageSuppression()
    {
        _statusEffects.SuppressDamage(PotionElement.Fire, _areaEffects.HasDamageElement(PotionElement.Fire));
        _statusEffects.SuppressDamage(PotionElement.Poison, _areaEffects.HasDamageElement(PotionElement.Poison));
    }

    public void RemoveStatusesFromSource(int sourceId)
    {
        _statusEffects.RemoveSource(sourceId);
    }

    public void ApplyStatus(PotionStatusEffectInfo effectInfo)
    {
        if (!PrepareStatusApplication()) return;
        _statusEffects.Apply(effectInfo);
        TickStatuses();
    }

    public void ApplyStatus(PotionEffectDefinition effect, PotionEffectSource source)
    {
        if (effect == null || effect.Operation != PotionEffectOperation.ApplyStatus) return;
        ApplyStatus(PotionStatusEffectInfo.FromDefinition(effect, source));
    }

    public void EnterFrostArea(int sourceId) => _statusEffects.EnterFrostArea(sourceId);
    public void RemoveAreaEffectsFromSource(int sourceId) => _statusEffects.RemoveAreaSource(sourceId);
    public void SetAreaSlow(int sourceId, float multiplier) => _statusEffects.SetAreaSlow(sourceId, multiplier);

    public void AddFrostStacks(PotionFrostSettings settings, PotionEffectSource source,
        int amount = 1, float tickInterval = 1f)
    {
        if (PrepareStatusApplication()) _statusEffects.AddFrostStacks(settings, source, amount, tickInterval);
    }

    private bool PrepareStatusApplication()
    {
        TickStatuses();
        return CanReceiveStatus();
    }

    private void Update() => TickStatuses();

    private void OnDisable()
    {
        _areaEffects.Clear();
        ClearStatuses();
    }

    private bool CanReceiveStatus() => isActiveAndEnabled && (Damageable == null || PotionStatDispatcher.IsAlive(Damageable));

    private void TickStatuses()
    {
        if (!CanReceiveStatus())
        {
            _areaEffects.Clear();
            ClearStatuses();
            return;
        }

        _statusEffects.Tick(ApplyStatusDamage);
    }

    private void ApplyStatusDamage(PotionStatusDamage tick)
    {
        if (!CanReceiveStatus())
        {
            ClearStatuses();
            return;
        }

        ApplyDamage(tick.Amount, tick.Source, tick.EffectInfo.Element, true);
        if (!CanReceiveStatus()) ClearStatuses();
    }

    private void ApplyDamage(float amount, PotionEffectSource source, PotionElement element, bool isStatusDamage)
    {
        PotionDamageResult result = PotionStatDispatcher.Damage(Damageable, amount, source, element);
        if (result.ActualDamage <= 0f) return;

        PotionStatDispatcher.Heal(source.Owner, result.LifeStealAmount);
        if (isStatusDamage) StatusDamageApplied?.Invoke(result);
        DamageApplied?.Invoke(result);
    }

    public int GetStatusStackCount(PotionStatusEffectTracker status)
    {
        return _statusEffects.GetStackCount(status);
    }
}
