using Unity.Profiling;

public static class OptimizationMarkers
{
    public static readonly ProfilerMarker Shape = new("Optimization.Combine.Shape");
    public static readonly ProfilerMarker Clip = new("Optimization.Combine.WallQueryAndClip");
    public static readonly ProfilerMarker Collider = new("Optimization.Combine.Collider");
    public static readonly ProfilerMarker Visual = new("Optimization.Combine.Visual");

    // Landing path: one potion's flight end to a live area. Nested; inclusive times overlap.
    public static readonly ProfilerMarker WallTrace = new("Optimization.Land.WallTrace");
    public static readonly ProfilerMarker Land = new("Optimization.Land.Total");
    public static readonly ProfilerMarker LandBreakEffect = new("Optimization.Land.BreakEffect");
    public static readonly ProfilerMarker LandInstantiate = new("Optimization.Land.AreaInstantiate");
    public static readonly ProfilerMarker LandInitialize = new("Optimization.Land.AreaInitialize");
    public static readonly ProfilerMarker LandClip = new("Optimization.Land.FootprintClip");
    public static readonly ProfilerMarker LandCollider = new("Optimization.Land.FootprintCollider");
    public static readonly ProfilerMarker LandEffectInit = new("Optimization.Land.AreaEffectInit");
    public static readonly ProfilerMarker LandVisualBind = new("Optimization.Land.VisualBind");
    public static readonly ProfilerMarker LandAreaVfx = new("Optimization.Land.AreaVfx");
    public static readonly ProfilerMarker LandActivate = new("Optimization.Land.AreaActivate");
    public static readonly ProfilerMarker LandRegister = new("Optimization.Land.RegisterArea");
    public static readonly ProfilerMarker LandSingleImpact = new("Optimization.Land.SingleImpact");
    public static readonly ProfilerMarker LandFeedback = new("Optimization.Land.Feedback");
    public static readonly ProfilerMarker LandIdentify = new("Optimization.Land.Identify");
}
