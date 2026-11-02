using System;
using UnityEngine;

// Ordinary gameplay uses GameDefault (the pixel-verified mask optimizations). The demo selects its variant
// explicitly, including Original, so comparison measurements are unaffected.
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
    Improved = StatusQuery | Timeline | SharedMask | EnemySearch,
    // Applied to the real game: only the mask path, verified pixel-identical (284 masks, 0 differences).
    GameDefault = SharedMask | FastMask
}

public static class OptimizationRuntime
{
    public static OptimizationVariant Variant { get; private set; } = OptimizationVariant.GameDefault;
    public static bool Uses(OptimizationVariant feature) => (Variant & feature) != 0;

    // Called before demo Setup creates receivers, timelines and renderers.
    public static void Select(OptimizationVariant variant) => Variant = variant;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Variant = OptimizationVariant.GameDefault;
}
