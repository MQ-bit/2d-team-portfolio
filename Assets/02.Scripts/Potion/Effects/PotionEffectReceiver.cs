using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PotionEffectReceiver : MonoBehaviour, IPotionEffectTarget
{
    [SerializeField] private PotionTargetKind _targetKind = PotionTargetKind.Enemy;

    private readonly PotionStatusEffectState _statusEffects = new();
    private readonly ComparisonAreaEffectTracker _areaEffects = new();

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
        // 기존 씬과 프리팹의 대상에도 상태 연출을 자동으로 연결한다.
        if (GetComponent<PotionStatusVisual>() == null)
        {
            gameObject.AddComponent<PotionStatusVisual>();
        }
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
                if (source.VaporizationBatch != null)
                    source.VaporizationBatch.AddHealing(Damageable, effect.Amount);
                else PotionStatApplier.Heal(Damageable, effect.Amount);
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
        if (effectInfo.Status == PotionStatusEffectTracker.Frozen
            || effectInfo.Status == PotionStatusEffectTracker.Paralyzed)
        {
            float multiplier = SelfPotionMultiplier(effectInfo.Source);
            effectInfo = new PotionStatusEffectInfo(effectInfo.Status, effectInfo.Duration * multiplier,
                effectInfo.Source, effectInfo.Element, effectInfo.DamagePerTick, effectInfo.TickInterval,
                effectInfo.ApplyImmediately, effectInfo.MoveSpeedMultiplier,
                effectInfo.ImmobilizeDuration * multiplier);
        }
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
        if (PrepareStatusApplication())
            _statusEffects.AddFrostStacks(settings, source, amount, tickInterval, SelfPotionMultiplier(source));
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

    private bool CanReceiveStatus() => isActiveAndEnabled && (Damageable == null || PotionStatApplier.IsAlive(Damageable));

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
        // Apply the player's own potion damage reduction after the reaction's raw cap.
        amount *= SelfPotionMultiplier(source);

        PotionDamageResult result = PotionStatApplier.Damage(Damageable, amount, source, element);
        if (result.ActualDamage <= 0f) return;

        if (TargetKind != PotionTargetKind.Enemy || ReferenceEquals(Damageable, source.Owner))
            result = new PotionDamageResult(source, element, result.ActualDamage, false);
        if (source.VaporizationBatch != null)
            source.VaporizationBatch.AddHealing(source.Owner, result.LifeStealAmount);
        else PotionStatApplier.Heal(source.Owner, result.LifeStealAmount);
        if (isStatusDamage) StatusDamageApplied?.Invoke(result);
        DamageApplied?.Invoke(result);
    }

    private float SelfPotionMultiplier(PotionEffectSource source) =>
        source.IsAreaDamage && TargetKind == PotionTargetKind.Player
        && ReferenceEquals(Damageable, source.Owner) ? 0.5f : 1f;

    public int GetStatusStackCount(PotionStatusEffectTracker status)
    {
        return _statusEffects.GetStackCount(status);
    }

    public float GetStatusRemainingTime(PotionStatusEffectTracker status)
    {
        return _statusEffects.GetRemainingTime(status);
    }

    // 상태 연출용 조회. 해당 상태를 거는 장판 위에 서 있는지 알려준다. (상태 자체는 아직 안 걸렸을 수 있다)
    public bool IsInStatusArea(PotionStatusEffectTracker status)
    {
        return _areaEffects.HasStatus(status);
    }

    // 장판과 남아 있는 상태의 조합 정보를 중복 없이 조회한다. 게임플레이 상태는 변경하지 않는다.
    public void GetVisualCombinations(List<PotionCombinationData> result)
    {
        result.Clear();
        _areaEffects.CollectVisualCombinations(result);
        _statusEffects.CollectVisualCombinations(result);
    }
}
