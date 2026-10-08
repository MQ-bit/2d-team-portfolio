using System.Collections.Generic;
using UnityEngine;

// Shared clock and overlap seed for rendering, VFX and effect membership.
internal sealed class PotionCombinationSpread
{
    private readonly float _startedAt;
    private readonly float _duration;
    private readonly float _maximumReach;
    private readonly List<Collider2D> _hits = new();
    private int _sampledFrame = -1;
    public PotionPuddleShape Shape { get; }
    public PolygonCollider2D Collider { get; }
    public Color FirstColor { get; }
    public Color SecondColor { get; }
    public bool IsComplete => IsCompleteAt(Time.time);
    public float Reach => ReachAt(Time.time);

    public PotionCombinationSpread(PotionPuddleShape shape, PolygonCollider2D collider,
        float startedAt, float duration, Color firstColor, Color secondColor)
    {
        Shape = shape;
        Collider = collider;
        _startedAt = startedAt;
        _duration = Mathf.Max(0f, duration);
        FirstColor = firstColor;
        SecondColor = secondColor;
        foreach (Vector2 point in shape.Outline)
            _maximumReach = Mathf.Max(_maximumReach, shape.DistanceFromOverlap(point));
        _maximumReach = Mathf.Max(0.01f, _maximumReach);
    }

    public bool IsCompleteAt(float now) => _duration <= 0f || now >= _startedAt + _duration;
    public float ReachAt(float now) => _maximumReach * (_duration > 0f
        ? Mathf.Clamp01((now - _startedAt) / _duration) : 1f);
    public bool HasReached(Vector2 worldPoint) => HasReachedAt(worldPoint, Time.time);
    public bool HasReachedAt(Vector2 worldPoint, float now) =>
        IsCompleteAt(now) || Shape.DistanceFromOverlap(worldPoint) <= ReachAt(now);
    public bool HasReached(Collider2D target) => target != null &&
        HasReached(target.ClosestPoint(Shape.OverlapCenter));

    public IReadOnlyList<Collider2D> Hits
    {
        get
        {
            if (_sampledFrame == Time.frameCount) return _hits;
            // Query once per frame for both original profiles and the combination.
            if (_sampledFrame < 0) Physics2D.SyncTransforms();
            _sampledFrame = Time.frameCount;
            _hits.Clear();
            if (Collider != null) Collider.Overlap(new ContactFilter2D { useTriggers = true }, _hits);
            return _hits;
        }
    }
}
