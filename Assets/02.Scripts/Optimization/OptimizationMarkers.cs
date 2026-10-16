using Unity.Profiling;

public static class OptimizationMarkers
{
    public static readonly ProfilerMarker Shape = new("Optimization.Combine.Shape");
    public static readonly ProfilerMarker Clip = new("Optimization.Combine.WallQueryAndClip");
    public static readonly ProfilerMarker Collider = new("Optimization.Combine.Collider");
    public static readonly ProfilerMarker Visual = new("Optimization.Combine.Visual");
}
