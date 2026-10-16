using System.Collections.Generic;
using Unity.Profiling;

// Routing only; both implementations retain their own state for their lifetime.
public sealed class ComparisonAreaEffectTracker
{
    private readonly PotionAreaEffectTracker _original;
    private readonly OptimizedPotionAreaEffectTracker _improved;
    private static readonly ProfilerMarker StatusMarker = new("Optimization.Status.HasStatus");
    public ComparisonAreaEffectTracker()
    {
        if (OptimizationRuntime.Uses(OptimizationVariant.StatusQuery)) _improved = new();
        else _original = new();
    }
    public void Enter(IPotionEffectData profile, PotionEffectSource source)
    { if (_improved != null) _improved.Enter(profile, source); else _original.Enter(profile, source); }
    public void Pulse(int sourceId, double now, System.Action<PotionEffectDefinition, IPotionEffectData, PotionEffectSource> apply)
    { if (_improved != null) _improved.Pulse(sourceId, now, apply); else _original.Pulse(sourceId, now, apply); }
    public bool Exit(int id, out IPotionEffectData profile, out PotionEffectSource source)
    { return _improved != null ? _improved.Exit(id, out profile, out source) : _original.Exit(id, out profile, out source); }
    public bool HasDamageElement(PotionElement element) => _improved != null ? _improved.HasDamageElement(element) : _original.HasDamageElement(element);
    public bool HasStatus(PotionStatusEffectTracker status)
    {
        using var marker = StatusMarker.Auto();
        return _improved != null ? _improved.HasStatus(status) : _original.HasStatus(status);
    }
    public void CollectVisualCombinations(List<PotionCombinationData> result)
    { if (_improved != null) _improved.CollectVisualCombinations(result); else _original.CollectVisualCombinations(result); }
    public void Clear()
    { if (_improved != null) _improved.Clear(); else _original.Clear(); }
}
