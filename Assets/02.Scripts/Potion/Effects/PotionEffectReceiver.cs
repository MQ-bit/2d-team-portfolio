using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PotionEffectReceiver : MonoBehaviour, IPotionEffectTarget
{
    [SerializeField] private PotionTargetKind _targetKind = PotionTargetKind.Enemy;
    [SerializeField] private PlayerStatus _playerStatus;

    private readonly PotionStatusEffectState _statusEffects = new();
    private readonly PotionFloorEffectState _floorEffects = new();

    public PotionTargetKind TargetKind => _targetKind;
    public IDamageable Damageable { get; private set; }
    public IThirstable Thirstable { get; private set; }
    public bool CanMove => _statusEffects.CanMove;
    public float MoveSpeedMultiplier => _statusEffects.MoveSpeedMultiplier;
    public int FrostStackCount => _statusEffects.FrostStackCount;
    public event Action<PotionDamageResult> StatusDamageApplied;
    public event Action<PotionDamageResult> DamageApplied;

    // 플레이어/적 공통 조회 API. 실제 이동은 각 이동 모듈에서 한다.
    public float GetMovementSpeed(float baseSpeed) => baseSpeed * MoveSpeedMultiplier;
    public float GetDashSpeed(float baseSpeed) => CanMove ? baseSpeed : 0f;

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
        RefreshDamageSuppression();
    }

    public void EnterFloor(PotionEffectProfile profile, PotionEffectSource source)
    {
        if (!CanReceiveStatus()) return;
        TickStatuses();
        _floorEffects.Enter(profile, source);
        foreach (var effect in profile.GetEffects(PotionEffectPhase.FloorTick))
            if (effect.Operation == PotionEffectOperation.AddFrostStacks) EnterFrostArea(source.SourceId);
        RefreshDamageSuppression();
    }

    public void PulseFloor(int sourceId)
    {
        if (CanReceiveStatus()) _floorEffects.Pulse(sourceId, Time.timeAsDouble, ApplyEffect);
    }

    public void ExitFloor(int sourceId, bool applyResidual)
    {
        if (!_floorEffects.Exit(sourceId, out var profile, out var source)) return;
        // 이탈 시점 이전의 억제된 DoT를 먼저 소비하여 밀린 피해가 튀어나오지 않게 한다.
        TickStatuses();
        RemoveAreaEffectsFromSource(sourceId);
        RefreshDamageSuppression();
        if (!applyResidual || !CanReceiveStatus()) return;
        foreach (var effect in profile.GetEffects(PotionEffectPhase.FloorExit))
            if (!_floorEffects.HasDamageElement(effect.Element)) ApplyEffect(effect, profile, source);
    }

    public void ApplyEffect(PotionEffectDefinition effect, PotionEffectProfile profile, PotionEffectSource source)
    {
        if (!CanReceiveStatus()) return;
        switch (effect.Operation)
        {
            case PotionEffectOperation.Damage:
                PotionDamageResult result = PotionStatDispatcher.Damage(Damageable, effect.Amount, source, effect.Element);
                if (result.ActualDamage > 0f) DamageApplied?.Invoke(result);
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
        _statusEffects.SuppressDamage(PotionElement.Fire, _floorEffects.HasDamageElement(PotionElement.Fire));
        _statusEffects.SuppressDamage(PotionElement.Poison, _floorEffects.HasDamageElement(PotionElement.Poison));
    }

    public void RemoveStatusesFromSource(int sourceId)
    {
        _statusEffects.RemoveSource(sourceId);
    }

    public void ApplyStatus(PotionStatusEffect status, float duration, int sourceId)
    {
        TickStatuses();
        if (!CanReceiveStatus()) return;
        _statusEffects.Apply(status, duration, sourceId);
    }

    public void ApplyStatus(PotionStatusApplication application)
    {
        TickStatuses();
        if (!CanReceiveStatus()) return;
        _statusEffects.Apply(application);
        TickStatuses();
    }

    public void ApplyStatus(PotionEffectDefinition effect, PotionEffectSource source)
    {
        if (effect == null || effect.Operation != PotionEffectOperation.ApplyStatus) return;
        ApplyStatus(PotionStatusApplication.FromDefinition(effect, source));
    }

    public void EnterFrostArea(int sourceId) => _statusEffects.EnterFrostArea(sourceId);
    public void ExitFrostArea(int sourceId) => _statusEffects.ExitFrostArea(sourceId);
    public void RemoveAreaEffectsFromSource(int sourceId) => _statusEffects.RemoveAreaSource(sourceId);
    public void SetAreaSlow(int sourceId, float multiplier) => _statusEffects.SetAreaSlow(sourceId, multiplier);

    public void AddFrostStacks(PotionFrostSettings settings, PotionEffectSource source,
        int amount = 1, float tickInterval = 1f)
    {
        TickStatuses();
        if (CanReceiveStatus()) _statusEffects.AddFrostStacks(settings, source, amount, tickInterval);
    }

    private void Update() => TickStatuses();
    private void OnDisable()
    {
        _floorEffects.Clear();
        ClearStatuses();
    }

    private bool CanReceiveStatus() => isActiveAndEnabled &&
        (Damageable == null || PotionStatDispatcher.IsAlive(Damageable));

    private void TickStatuses()
    {
        if (!CanReceiveStatus()) { _floorEffects.Clear(); ClearStatuses(); return; }
        _statusEffects.Tick(ApplyStatusDamage);
    }

    private void ApplyStatusDamage(PotionStatusDamage tick)
    {
        if (!CanReceiveStatus()) { ClearStatuses(); return; }
        PotionDamageResult result = PotionStatDispatcher.Damage(Damageable, tick.Amount,
            tick.Source, tick.Application.Element);
        if (result.ActualDamage > 0f)
        {
            StatusDamageApplied?.Invoke(result);
            DamageApplied?.Invoke(result);
        }
        if (!CanReceiveStatus()) ClearStatuses();
    }

    public int GetStatusStackCount(PotionStatusEffect status)
    {
        return _statusEffects.GetStackCount(status);
    }
}
