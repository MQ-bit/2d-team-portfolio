using UnityEngine;

// One immutable shape drives the surface shader, collider and VFX placement.
// Work in unflattened space, then compress world Y to match the single-area preview.
internal sealed class PotionPuddleShape
{
    private const float HeightRatio = 0.5f;
    private const int Sections = 64;
    private const int RootIterations = 18;

    public Vector2 Center { get; }
    public Vector2 Direction { get; }
    public float HalfDistance { get; }
    public float FirstRadius { get; }
    public float SecondRadius { get; }
    public float NeckRadius { get; }
    public float Blend { get; }
    public Vector2[] Outline { get; }
    public Vector2 BoundsCenter { get; }
    public Vector2 BoundsSize { get; }
    public Vector2 FirstCenter => ToWorld(-Direction * HalfDistance);
    public Vector2 SecondCenter => ToWorld(Direction * HalfDistance);
    public bool HasLobeOverlap => HalfDistance * 2f <= FirstRadius + SecondRadius;
    public Vector2 OverlapCenter
    {
        get
        {
            float left = Mathf.Max(-HalfDistance - FirstRadius, HalfDistance - SecondRadius);
            float right = Mathf.Min(-HalfDistance + FirstRadius, HalfDistance + SecondRadius);
            return ToWorld(Direction * ((left + right) * 0.5f));
        }
    }

    public PotionPuddleShape(Vector2 first, float firstRadius, Vector2 second,
        float secondRadius, float connectionRatio, float blendRatio)
    {
        Center = (first + second) * 0.5f;
        Vector2 delta = second - first;
        delta.y /= HeightRatio;
        HalfDistance = delta.magnitude * 0.5f;
        Direction = delta.sqrMagnitude > 0.000001f ? delta.normalized : Vector2.right;
        FirstRadius = Mathf.Max(0.01f, firstRadius);
        SecondRadius = Mathf.Max(0.01f, secondRadius);
        float smallerRadius = Mathf.Min(FirstRadius, SecondRadius);
        NeckRadius = smallerRadius * Mathf.Clamp(connectionRatio, 0.2f, 0.9f);
        Blend = smallerRadius * Mathf.Clamp(blendRatio, 0.01f, 0.5f);
        Outline = BuildOutline();

        Vector2 minimum = Outline[0];
        Vector2 maximum = minimum;
        foreach (Vector2 point in Outline)
        {
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }
        BoundsCenter = (minimum + maximum) * 0.5f;
        // Leave a small margin for sampling/rounding at the silhouette's extrema.
        BoundsSize = maximum - minimum + Vector2.one * 0.02f;
    }

    public bool Contains(Vector2 worldPoint)
    {
        Vector2 flat = worldPoint - Center;
        flat.y /= HeightRatio;
        Vector2 normal = new(-Direction.y, Direction.x);
        return Distance(new Vector2(Vector2.Dot(flat, Direction), Vector2.Dot(flat, normal))) <= 0f;
    }

    public float DistanceFromOverlap(Vector2 worldPoint)
    {
        Vector2 flat = worldPoint - Center;
        flat.y /= HeightRatio;
        float first = (flat + Direction * HalfDistance).magnitude - FirstRadius;
        float second = (flat - Direction * HalfDistance).magnitude - SecondRadius;
        return Mathf.Max(first, second);
    }

    public bool ContainsLobe(Vector2 worldPoint, bool firstLobe)
    {
        Vector2 flat = worldPoint - (firstLobe ? FirstCenter : SecondCenter);
        flat.y /= HeightRatio;
        float radius = firstLobe ? FirstRadius : SecondRadius;
        return flat.sqrMagnitude <= radius * radius;
    }

    public Vector2 SampleLocalPoint(float spawnRatio)
    {
        Vector2 halfSize = BoundsSize * (0.5f * Mathf.Clamp01(spawnRatio));
        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector2 local = new(Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y));
            if (Contains(BoundsCenter + local)) return local;
        }
        // The midpoint always belongs to the bridge, even for a long diagonal shape.
        return Center - BoundsCenter;
    }

    private float Distance(Vector2 point)
    {
        float first = (point + Vector2.right * HalfDistance).magnitude - FirstRadius;
        float second = (point - Vector2.right * HalfDistance).magnitude - SecondRadius;
        Vector2 bridge = new(point.x - Mathf.Clamp(point.x, -HalfDistance, HalfDistance), point.y);
        return SmoothMinimum(SmoothMinimum(first, second), bridge.magnitude - NeckRadius);
    }

    private float SmoothMinimum(float a, float b)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / Blend);
        return Mathf.Lerp(b, a, h) - Blend * h * (1f - h);
    }

    private Vector2[] BuildOutline()
    {
        float extent = Mathf.Max(FirstRadius, SecondRadius) + Blend;
        float left = FindEnd(-HalfDistance - extent, 0f);
        float right = FindEnd(HalfDistance + extent, 0f);
        Vector2 normal = new(-Direction.y, Direction.x);
        var points = new Vector2[Sections * 2];
        for (int i = 0; i <= Sections; i++)
        {
            // Cosine spacing adds resolution at the rounded tips.
            float fraction = (1f - Mathf.Cos(i * Mathf.PI / Sections)) * 0.5f;
            float x = Mathf.Lerp(left, right, fraction);
            float low = 0f;
            float high = extent;
            if (i != 0 && i != Sections)
            {
                for (int refine = 0; refine < RootIterations; refine++)
                {
                    float middle = (low + high) * 0.5f;
                    if (Distance(new Vector2(x, middle)) <= 0f) low = middle;
                    else high = middle;
                }
            }

            float y = i == 0 || i == Sections ? 0f : (low + high) * 0.5f;
            points[i] = ToWorld(Direction * x + normal * y);
            if (i > 0 && i < Sections)
                points[Sections * 2 - i] = ToWorld(Direction * x - normal * y);
        }
        return points;
    }

    private float FindEnd(float outside, float inside)
    {
        for (int refine = 0; refine < RootIterations; refine++)
        {
            float middle = (outside + inside) * 0.5f;
            if (Distance(new Vector2(middle, 0f)) <= 0f) inside = middle;
            else outside = middle;
        }
        return (outside + inside) * 0.5f;
    }

    private Vector2 ToWorld(Vector2 flat) => Center + new Vector2(flat.x, flat.y * HeightRatio);
}
