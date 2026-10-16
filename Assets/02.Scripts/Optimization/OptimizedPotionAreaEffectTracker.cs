using System;
using System.Collections.Generic;
using UnityEngine;

// 한 대상이 겹친 장판에서 동일 효과를 중복 수신하지 않도록 공통 주기를 관리한다.
public sealed class OptimizedPotionAreaEffectTracker
{
    private readonly struct Entry
    {
        public readonly IPotionEffectData Profile;
        public readonly PotionEffectSource Source;
        public Entry(IPotionEffectData profile, PotionEffectSource source) { Profile = profile; Source = source; }
    }

    private readonly struct Key : IEquatable<Key>
    {
        private readonly PotionEffectOperation _operation;
        private readonly PotionElement _element;
        private readonly PotionStatusEffectTracker _status;
        public Key(PotionEffectDefinition effect)
        {
            _operation = effect.Operation;
            _element = _operation == PotionEffectOperation.Damage ? effect.Element : PotionElement.None;
            _status = _operation == PotionEffectOperation.ApplyStatus ? effect.Status : default;
        }
        public bool Equals(Key other) => _operation == other._operation && _element == other._element && _status == other._status;
        public override bool Equals(object obj) => obj is Key other && Equals(other);
        public override int GetHashCode() => ((int)_operation * 397 ^ (int)_element) * 397 ^ (int)_status;
    }

    private readonly Dictionary<int, Entry> _sources = new();
    private readonly Dictionary<Key, double> _nextTicks = new();

    // Convert the remaining periodic output to an instant amount. Entry/impact
    // damage has already happened; exit statuses are removed instead of applied.
    public static List<PotionEffectDefinition> GetRemainingEffects(IPotionEffectData profile,
        float remainingDuration, float multiplier)
        => ConvertRemainingEffects(profile, remainingDuration, _ => multiplier);

    public static List<PotionEffectDefinition> GetVaporizationEffects(IPotionEffectData profile,
        float remainingDuration, PotionVaporizationSettings settings)
        => ConvertRemainingEffects(profile, remainingDuration,
            effect => settings != null ? settings.GetOutputMultiplier(effect)
                : PotionVaporizationSettings.DefaultOutputMultiplier(effect));

    private static List<PotionEffectDefinition> ConvertRemainingEffects(IPotionEffectData profile,
        float remainingDuration, Func<PotionEffectDefinition, float> multiplier)
    {
        var result = new List<PotionEffectDefinition>();
        if (profile == null || profile.TickInterval <= 0f || remainingDuration <= 0f) return result;

        var selected = new Dictionary<Key, PotionEffectDefinition>();
        foreach (var effect in profile.GetEffects(PotionEffectPhase.FloorTick))
        {
            if (effect.Operation != PotionEffectOperation.Damage &&
                effect.Operation != PotionEffectOperation.Heal)
            {
                continue;
            }

            var key = new Key(effect);
            if (!selected.TryGetValue(key, out var current) || effect.Amount > current.Amount)
                selected[key] = effect;
        }

        float ticks = remainingDuration / Math.Max(0.01f, profile.TickInterval);
        foreach (var effect in selected.Values)
        {
            result.Add(PotionEffectDefinition.CreateInstant(effect.Operation, effect.Element,
                effect.Amount * ticks * PotionDataValidation.NonNegative(multiplier(effect))));
        }

        return result;
    }

    public void Enter(IPotionEffectData profile, PotionEffectSource source)
    {
        if (source.Combination != null)
        {
            // The reaction front can reach this target before the partner's old
            // floor exits. Its base-potion cooldown must not delay the new entry pulse.
            foreach (var effect in profile.GetEffects(PotionEffectPhase.FloorTick))
            {
                var key = new Key(effect);
                if (TrySelect(key, out Entry previous, out _) && previous.Source.Combination == null)
                    _nextTicks.Remove(key);
            }
        }
        _sources[source.SourceId] = new Entry(profile, source);
    }

    public void CollectVisualCombinations(List<PotionCombinationData> result)
    {
        foreach (Entry entry in _sources.Values)
        {
            PotionCombinationData combination = entry.Source.Combination;
            if (combination != null && !result.Contains(combination)) result.Add(combination);
        }
    }

    // Read the shared effect list by index without creating phase iterators.
    public bool HasDamageElement(PotionElement element)
    {
        foreach (Entry entry in _sources.Values)
        {
            var effects = entry.Profile?.Effects;
            if (effects == null) continue;
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect != null && effect.Phase == PotionEffectPhase.FloorTick &&
                    effect.Operation == PotionEffectOperation.Damage && effect.Element == element)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public bool HasStatus(PotionStatusEffectTracker status)
    {
        foreach (Entry entry in _sources.Values)
        {
            var effects = entry.Profile?.Effects;
            if (effects == null) continue;
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect != null && (effect.Phase == PotionEffectPhase.FloorTick ||
                    effect.Phase == PotionEffectPhase.FloorExit) && AppliesStatus(effect, status))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static bool AppliesStatus(PotionEffectDefinition effect, PotionStatusEffectTracker status)
    {
        if (effect.Operation == PotionEffectOperation.ApplyStatus) return effect.Status == status;
        return effect.Operation == PotionEffectOperation.AddFrostStacks && status == PotionStatusEffectTracker.Frozen;
    }

    public void Pulse(int sourceId, double now,
        Action<PotionEffectDefinition, IPotionEffectData, PotionEffectSource> apply)
    {
        if (!_sources.TryGetValue(sourceId, out Entry caller)) return;
        var handled = new HashSet<Key>();
        foreach (var effect in caller.Profile.GetEffects(PotionEffectPhase.FloorTick))
        {
            if (!_sources.ContainsKey(sourceId)) return;
            var key = new Key(effect);
            if (!handled.Add(key) || (_nextTicks.TryGetValue(key, out double next) && now < next)) continue;
            if (!TrySelect(key, out Entry selected, out PotionEffectDefinition definition)) continue;
            _nextTicks[key] = now + Math.Max(0.01, selected.Profile.TickInterval);
            apply(definition, selected.Profile, selected.Source);
        }
    }

    public bool Exit(int sourceId, out IPotionEffectData profile, out PotionEffectSource source)
    {
        profile = null;
        source = default;
        if (!_sources.TryGetValue(sourceId, out Entry removed)) return false;
        _sources.Remove(sourceId);
        profile = removed.Profile;
        source = removed.Source;
        foreach (var key in new List<Key>(_nextTicks.Keys))
            if (!TrySelect(key, out _, out _)) _nextTicks.Remove(key);
        return true;
    }

    public void Clear() { _sources.Clear(); _nextTicks.Clear(); }

    private bool TrySelect(Key key, out Entry selected, out PotionEffectDefinition definition)
    {
        selected = default;
        definition = null;
        double best = double.NegativeInfinity;
        foreach (Entry entry in _sources.Values)
        {
            foreach (var effect in entry.Profile.GetEffects(PotionEffectPhase.FloorTick))
            {
                if (!key.Equals(new Key(effect))) continue;
                double strength = effect.Amount / Math.Max(0.01, entry.Profile.TickInterval);
                if (definition != null && (strength < best ||
                    (strength == best && entry.Source.SourceId >= selected.Source.SourceId)))
                {
                    continue;
                }

                selected = entry;
                definition = effect;
                best = strength;
            }
        }
        return definition != null;
    }
}

