using System;
using System.Collections.Generic;
using Unity.Profiling;

public sealed class ComparisonAreaTimeline<T>
{
    private readonly PotionAreaTimeline<T> _original;
    private readonly OptimizedPotionAreaTimeline<T> _improved;
    private static readonly ProfilerMarker SyncMarker = new("Optimization.Timeline.Synchronize");
    public ComparisonAreaTimeline(double expiresAt, double interval, bool applyOnEnter,
        Func<T, bool> isValid, Action<T> enter, Action<T> tick, Action<T, bool> exit)
    {
        if (OptimizationRuntime.Uses(OptimizationVariant.Timeline))
            _improved = new(expiresAt, interval, applyOnEnter, isValid, enter, tick, exit);
        else _original = new(expiresAt, interval, applyOnEnter, isValid, enter, tick, exit);
    }
    // Existing spread validation can still pass an original timeline explicitly.
    private ComparisonAreaTimeline(PotionAreaTimeline<T> original) => _original = original;
    public static implicit operator ComparisonAreaTimeline<T>(PotionAreaTimeline<T> original) =>
        original == null ? null : new ComparisonAreaTimeline<T>(original);
    public void Synchronize(IEnumerable<T> overlaps, double now)
    {
        using var marker = SyncMarker.Auto();
        if (_improved != null) _improved.Synchronize(overlaps, now); else _original.Synchronize(overlaps, now);
    }
    public void End(bool applyResidual)
    { if (_improved != null) _improved.End(applyResidual); else _original.End(applyResidual); }
}
