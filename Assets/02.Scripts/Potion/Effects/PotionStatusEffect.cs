using System;
using System.Collections.Generic;
using UnityEngine;

public enum PotionStatusEffect
{
    Burning = 0,
    Poisoned = 1,
    Frozen = 2,
    Paralyzed = 3,
    Slowed = 4
}

// 시간과 상태만 계산한다. HP/갈증 및 실제 이동은 변경하지 않는다.
public sealed class PotionStatusEffectState
{
    private sealed class ActiveStatus
    {
        public PotionStatusApplication Application;
        public double ExpiresAt;
        public double NextTickAt;
        public double PulseStartedAt;
    }

    private readonly Func<double> _clock;
    private readonly Dictionary<PotionStatusEffect, ActiveStatus> _statuses = new();
    private readonly HashSet<int> _frostAreas = new();
    private readonly Dictionary<int, float> _areaSlows = new();
    private readonly HashSet<PotionElement> _suppressedDamageElements = new();
    private readonly List<KeyValuePair<PotionStatusEffect, ActiveStatus>> _tickSnapshot = new();
    private int _frostStacks;
    private float _frostMultiplier = 1f;
    private double _nextFrostStackAt = double.NegativeInfinity;
    private bool _isTicking;

    public PotionStatusEffectState() : this(() => Time.timeAsDouble) { }
    public PotionStatusEffectState(Func<double> clock) => _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    // 둔화는 대시를 차단하지 않는다. 빙결/마비만 이 bool을 변경한다.
    public bool CanMove => !IsImmobilized(_clock());
    public int FrostStackCount => _frostStacks;
    public float MoveSpeedMultiplier
    {
        get
        {
            double now = _clock();
            if (IsImmobilized(now)) return 0f;
            float multiplier = _frostMultiplier;
            if (TryGetActive(PotionStatusEffect.Slowed, now, out ActiveStatus slow))
                multiplier = Mathf.Min(multiplier, slow.Application.MoveSpeedMultiplier);
            foreach (float areaSlow in _areaSlows.Values) multiplier = Mathf.Min(multiplier, areaSlow);
            return multiplier;
        }
    }

    public bool IsActive(PotionStatusEffect status) => TryGetActive(status, _clock(), out _);
    public int GetStackCount(PotionStatusEffect status) => IsActive(status) ? 1 : 0;

    // 걸려 있지 않으면 0.
    public float GetRemainingTime(PotionStatusEffect status)
    {
        double now = _clock();
        return TryGetActive(status, now, out ActiveStatus active) ? (float)(active.ExpiresAt - now) : 0f;
    }

    public void Apply(PotionStatusApplication application)
    {
        if (application.Duration <= 0f) return;
        double now = _clock();
        bool refreshing = TryGetActive(application.Status, now, out ActiveStatus previous);
        PotionStatusApplication selected = application;
        if (refreshing && Strength(previous.Application) > Strength(application))
            selected = previous.Application;

        double nextTick = selected.TickInterval > 0f
            ? now + (selected.ApplyImmediately ? 0d : selected.TickInterval)
            : double.PositiveInfinity;
        double pulseStart = now + (selected.ApplyImmediately ? 0d : selected.TickInterval);
        if (refreshing)
        {
            // 즉시 Tick을 반복하거나 다음 Tick을 계속 미루지 않는다.
            nextTick = previous.NextTickAt;
            pulseStart = previous.PulseStartedAt;
            if (double.IsPositiveInfinity(nextTick) && selected.TickInterval > 0f)
                nextTick = now + selected.TickInterval;
        }
        _statuses[application.Status] = new ActiveStatus
        {
            Application = selected,
            ExpiresAt = now + application.Duration,
            NextTickAt = nextTick,
            PulseStartedAt = pulseStart
        };
        if (application.Status == PotionStatusEffect.Frozen) ResetFrostStacks();
    }

    // Receiver는 재적용 전에 Tick을 호출해 만료 경계의 마지막 정규 피해를 정산한다.
    public void Tick(Action<PotionStatusDamage> onDamage)
    {
        if (_isTicking) return;
        _isTicking = true;
        try
        {
            double now = _clock();
            _tickSnapshot.Clear();
            foreach (var pair in _statuses) _tickSnapshot.Add(pair);
            foreach (var pair in _tickSnapshot)
            {
                ActiveStatus active = pair.Value;
                while (IsCurrent(pair.Key, active) && active.Application.DamagePerTick > 0f &&
                       active.Application.TickInterval > 0f && active.NextTickAt <= now &&
                       active.NextTickAt <= active.ExpiresAt)
                {
                    active.NextTickAt += active.Application.TickInterval;
                    if (!_suppressedDamageElements.Contains(active.Application.Element))
                        onDamage?.Invoke(new PotionStatusDamage(active.Application));
                }
                if (IsCurrent(pair.Key, active) && now >= active.ExpiresAt) _statuses.Remove(pair.Key);
            }
        }
        finally { _isTicking = false; }
    }

    public void EnterFrostArea(int sourceId) => _frostAreas.Add(sourceId);
    public void ExitFrostArea(int sourceId)
    {
        if (_frostAreas.Remove(sourceId) && _frostAreas.Count == 0) ResetFrostStacks();
    }

    public void AddFrostStacks(PotionFrostSettings settings, PotionEffectSource source,
        int amount = 1, float tickInterval = 1f)
    {
        if (settings == null || amount <= 0) return;
        EnterFrostArea(source.SourceId);
        double now = _clock();
        if (IsActive(PotionStatusEffect.Frozen) || now < _nextFrostStackAt) return;
        _nextFrostStackAt = now + Math.Max(0.01d, PotionDataValidation.NonNegative(tickInterval));
        int threshold = Math.Max(1, settings.FreezeThreshold);
        _frostStacks = (int)Math.Min(threshold, (long)_frostStacks + amount);
        if (_frostStacks >= threshold)
        {
            Apply(new PotionStatusApplication(PotionStatusEffect.Frozen, settings.FreezeDuration, source));
            ResetFrostStacks();
            return;
        }
        var multipliers = settings.MoveSpeedMultipliers;
        _frostMultiplier = multipliers != null && _frostStacks <= multipliers.Count
            ? PotionDataValidation.Multiplier(multipliers[_frostStacks - 1]) : 1f;
    }

    public void SetAreaSlow(int sourceId, float multiplier) =>
        _areaSlows[sourceId] = PotionDataValidation.Multiplier(multiplier);

    public void SuppressDamage(PotionElement element, bool suppressed)
    {
        if (suppressed) _suppressedDamageElements.Add(element);
        else _suppressedDamageElements.Remove(element);
    }

    // 장판 이탈/삭제에서는 이미 적용한 잔류 상태를 지우지 않는다.
    public void RemoveAreaSource(int sourceId)
    {
        _areaSlows.Remove(sourceId);
        ExitFrostArea(sourceId);
    }

    // 명시적 해제 및 기존 조합 교체 호환용.
    public void RemoveSource(int sourceId)
    {
        var remove = new List<PotionStatusEffect>();
        foreach (var pair in _statuses)
            if (pair.Value.Application.Source.SourceId == sourceId) remove.Add(pair.Key);
        foreach (var status in remove) _statuses.Remove(status);
        RemoveAreaSource(sourceId);
    }

    public void Clear()
    {
        _statuses.Clear();
        _areaSlows.Clear();
        _suppressedDamageElements.Clear();
        _frostAreas.Clear();
        ResetFrostStacks();
    }

    private void ResetFrostStacks()
    {
        _frostStacks = 0;
        _frostMultiplier = 1f;
        _nextFrostStackAt = double.NegativeInfinity;
    }

    private bool TryGetActive(PotionStatusEffect status, double now, out ActiveStatus active) =>
        _statuses.TryGetValue(status, out active) && now < active.ExpiresAt;

    private bool IsCurrent(PotionStatusEffect status, ActiveStatus active) =>
        _statuses.TryGetValue(status, out ActiveStatus current) && ReferenceEquals(current, active);

    private bool IsImmobilized(double now)
    {
        if (TryGetActive(PotionStatusEffect.Frozen, now, out _)) return true;
        if (!TryGetActive(PotionStatusEffect.Paralyzed, now, out ActiveStatus paralysis)) return false;
        var effect = paralysis.Application;
        if (effect.TickInterval <= 0f) return true;
        if (now < paralysis.PulseStartedAt) return false;
        double phase = (now - paralysis.PulseStartedAt) % effect.TickInterval;
        return phase < effect.ImmobilizeDuration;
    }

    private static double Strength(PotionStatusApplication application)
    {
        if (application.Status == PotionStatusEffect.Slowed) return 1d - application.MoveSpeedMultiplier;
        if (application.Status == PotionStatusEffect.Paralyzed)
        {
            return application.TickInterval > 0f
                ? application.ImmobilizeDuration / application.TickInterval : 1d;
        }

        return application.TickInterval > 0f ? application.DamagePerTick / application.TickInterval : 0d;
    }
}
