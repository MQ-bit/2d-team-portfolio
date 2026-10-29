using System;
using UnityEngine;

// Original is the default, including ordinary gameplay. Only the demo opts in.
[Flags]
public enum OptimizationVariant
{
    Original = 0,
    StatusQuery = 1,
    Timeline = 2,
    SharedMask = 4,
    EnemySearch = 8,
    // Row-span rasterization of the clip mask; only meaningful together with SharedMask.
    FastMask = 16,
    Improved = StatusQuery | Timeline | SharedMask | EnemySearch
}

public static class OptimizationRuntime
{
    public static OptimizationVariant Variant { get; private set; }
    public static bool Uses(OptimizationVariant feature) => (Variant & feature) != 0;

    // Called before demo Setup creates receivers, timelines and renderers.
    public static void Select(OptimizationVariant variant) => Variant = variant;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Variant = OptimizationVariant.Original;
}
