using System;
using System.Collections.Generic;

// 한 대상이 겹친 장판에서 동일 효과를 중복 수신하지 않도록 공통 주기를 관리한다.
public sealed class PotionFloorEffectState
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
        private readonly PotionStatusEffect _status;
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

    public void Enter(IPotionEffectData profile, PotionEffectSource source) =>
        _sources[source.SourceId] = new Entry(profile, source);

    public bool HasDamageElement(PotionElement element)
    {
        foreach (Entry entry in _sources.Values)
        {
            foreach (var effect in entry.Profile.GetEffects(PotionEffectPhase.FloorTick))
                if (effect.Operation == PotionEffectOperation.Damage && effect.Element == element) return true;
        }

        return false;
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
