using Unity.Profiling;
using UnityEngine;

internal sealed class ComparisonAreaMaskRenderer
{
    private readonly PotionAreaMaskRenderer _original;
    private readonly OptimizedPotionAreaMaskRenderer _improved;
    private static readonly ProfilerMarker SetMarker = new("Optimization.Mask.SetFootprint");
    internal ComparisonAreaMaskRenderer()
    {
        if (OptimizationRuntime.Uses(OptimizationVariant.SharedMask)) _improved = new();
        else _original = new();
    }
    internal void SetFootprint(PotionAreaFootprint footprint)
    {
        using var marker = SetMarker.Auto();
        if (_improved != null) _improved.SetFootprint(footprint); else _original.SetFootprint(footprint);
    }
    internal void Apply(MaterialPropertyBlock properties)
    { if (_improved != null) _improved.Apply(properties); else _original.Apply(properties); }
    internal void Release()
    { if (_improved != null) _improved.Release(); else _original.Release(); }
}
