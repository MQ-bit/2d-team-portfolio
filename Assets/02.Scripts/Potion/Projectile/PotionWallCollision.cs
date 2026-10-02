using System.Collections.Generic;
using UnityEngine;

public readonly struct PotionThrowPrediction
{
    public readonly bool HitWall;
    public readonly float FlightProgress;
    public readonly Vector3 ImpactPosition;
    public readonly Vector3 AreaPosition;
    public readonly bool CanCreateArea;

    public PotionThrowPrediction(bool hitWall, float progress, Vector3 impactPosition,
        Vector3 areaPosition, bool canCreateArea = true)
    {
        HitWall = hitWall;
        FlightProgress = progress;
        ImpactPosition = impactPosition;
        AreaPosition = areaPosition;
        CanCreateArea = canCreateArea;
    }
}

// Shared by the projectile and its aim preview. All casts follow the visible bottle.
internal sealed class PotionWallCollision
{
    private readonly List<RaycastHit2D> _hits = new();
    private readonly List<Collider2D> _overlaps = new();

    public bool TryTrace(Vector3 start, Vector3 target, float arcHeight, float fromProgress,
        float toProgress, LayerMask wallLayers, float groundClearance, out PotionThrowPrediction prediction)
    {
        prediction = new PotionThrowPrediction(false, 1f, target, target);
        ContactFilter2D filter = new() { useTriggers = false, useLayerMask = true };
        filter.layerMask = wallLayers.value != 0 ? wallLayers : (LayerMask)Physics2D.DefaultRaycastLayers;
        fromProgress = Mathf.Clamp01(fromProgress);
        toProgress = Mathf.Clamp(toProgress, fromProgress, 1f);
        arcHeight = Mathf.Max(0f, arcHeight);

        Vector3 current = ThrowTrajectory.EvaluateVisualPoint(start, target, arcHeight, fromProgress);
        _overlaps.Clear();
        Physics2D.OverlapPoint(current, filter, _overlaps);
        foreach (Collider2D overlap in _overlaps)
        {
            if (!IsWall(overlap, wallLayers)) continue;
            prediction = MakeImpact(overlap, current, Vector2.zero, fromProgress, groundClearance, filter, wallLayers);
            return true;
        }

        // Fixed trajectory subdivisions give preview and flight the same collision points.
        int segments = Mathf.Clamp(Mathf.CeilToInt((Vector2.Distance(start, target) + arcHeight * 4f) / 0.2f), 32, 512);
        int first = Mathf.FloorToInt(fromProgress * segments);
        int last = Mathf.Min(segments - 1, Mathf.CeilToInt(toProgress * segments) - 1);
        for (int i = first; i <= last; i++)
        {
            float a = i / (float)segments;
            float b = (i + 1) / (float)segments;
            Vector3 from = ThrowTrajectory.EvaluateVisualPoint(start, target, arcHeight, a);
            Vector3 to = ThrowTrajectory.EvaluateVisualPoint(start, target, arcHeight, b);
            Vector2 movement = (Vector2)(to - from);
            float distance = movement.magnitude;
            if (distance <= Mathf.Epsilon) continue;

            _hits.Clear();
            Physics2D.Raycast(from, movement / distance, filter, _hits, distance);
            RaycastHit2D nearest = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit2D hit in _hits)
            {
                float progress = Mathf.Lerp(a, b, hit.distance / distance);
                if (!IsWall(hit.collider, wallLayers) || progress < fromProgress - 0.00001f ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearest = hit;
                nearestDistance = hit.distance;
            }
            if (nearest.collider == null) continue;
            float hitProgress = Mathf.Lerp(a, b, nearestDistance / distance);
            if (hitProgress > toProgress + 0.00001f) return false;
            Vector3 impact = new(nearest.point.x, nearest.point.y, Mathf.Lerp(start.z, target.z, hitProgress));
            prediction = MakeImpact(nearest.collider, impact, nearest.normal, hitProgress,
                groundClearance, filter, wallLayers);
            return true;
        }
        return false;
    }

    private PotionThrowPrediction MakeImpact(Collider2D wall, Vector3 impact, Vector2 normal,
        float progress, float clearance, ContactFilter2D filter, LayerMask wallLayers)
    {
        // The ray hit is already on the wall boundary. Its outward normal points
        // toward the adjacent floor, whether this is an upper, lower or side wall.
        Vector3 ground = impact + (Vector3)normal.normalized * Mathf.Max(0.01f, clearance);
        // An overlap at the launch point has no surface normal. Do not invent a
        // distant floor position or create an area inside the wall in that case.
        bool foundGround = wall != null && normal.sqrMagnitude > Mathf.Epsilon &&
            !HasWallAt(ground, filter, wallLayers);
        return new PotionThrowPrediction(true, progress, impact, ground, foundGround);
    }

    private bool HasWallAt(Vector2 point, ContactFilter2D filter, LayerMask wallLayers)
    {
        _overlaps.Clear();
        Physics2D.OverlapPoint(point, filter, _overlaps);
        foreach (Collider2D collider in _overlaps)
            if (IsWall(collider, wallLayers)) return true;
        return false;
    }

    private static bool IsWall(Collider2D collider, LayerMask wallLayers) => collider != null &&
        (wallLayers.value != 0 ? (wallLayers.value & (1 << collider.gameObject.layer)) != 0
            : collider.gameObject.name == "Tilemap_Wall");
}
