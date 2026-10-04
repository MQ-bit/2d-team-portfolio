using System.Collections.Generic;
using UnityEngine;

// Wall identification is shared by flight and floor clipping. No potion state here.
internal static class PotionWallQuery
{
    internal static ContactFilter2D Filter(LayerMask layers) => new()
    {
        useTriggers = false,
        useLayerMask = true,
        layerMask = layers.value != 0 ? layers : (LayerMask)Physics2D.DefaultRaycastLayers
    };

    internal static bool IsWall(Collider2D collider, LayerMask layers) => collider != null &&
        (layers.value != 0 ? (layers.value & (1 << collider.gameObject.layer)) != 0
            : collider.gameObject.name == "Tilemap_Wall");

    internal static List<Vector2[]> FindPolygons(Rect bounds, LayerMask layers)
    {
        Physics2D.SyncTransforms();
        var hits = new List<Collider2D>();
        Physics2D.OverlapBox(bounds.center, bounds.size, 0f, Filter(layers), hits);
        var result = new List<Vector2[]>();
        var shapes = new PhysicsShapeGroup2D();
        var vertices = new List<Vector2>();
        foreach (Collider2D wall in hits)
        {
            if (!IsWall(wall, layers)) continue;
            wall.GetShapes(shapes);
            for (int i = 0; i < shapes.shapeCount; i++)
            {
                PhysicsShape2D shape = shapes.GetShape(i);
                vertices.Clear();
                shapes.GetShapeVertices(i, vertices);
                for (int j = 0; j < vertices.Count; j++)
                    vertices[j] = shapes.localToWorldMatrix.MultiplyPoint3x4(vertices[j]);

                if (shape.shapeType == PhysicsShapeType2D.Polygon)
                {
                    AddIfNearby(result, ExpandPolygon(vertices, shape.radius), bounds);
                }
                else if (shape.shapeType == PhysicsShapeType2D.Circle)
                {
                    AddIfNearby(result, Capsule(vertices[0], vertices[0], shape.radius), bounds);
                }
                else if (shape.shapeType == PhysicsShapeType2D.Capsule)
                {
                    AddIfNearby(result, Capsule(vertices[0], vertices[1], shape.radius), bounds);
                }
                else
                {
                    // Edge/outline walls have no filled interior. Give their boundary a
                    // small thickness so even a zero-radius edge separates components.
                    for (int j = 1; j < vertices.Count; j++)
                    {
                        AddIfNearby(result, Capsule(vertices[j - 1], vertices[j],
                            Mathf.Max(0.005f, shape.radius)), bounds);
                    }
                }
            }
        }
        return result;
    }

    private static void AddIfNearby(List<Vector2[]> result, Vector2[] polygon, Rect bounds)
    {
        if (polygon.Length >= 3 && PotionAreaFootprint.BoundsOf(polygon).Overlaps(bounds))
            result.Add(polygon);
    }

    private static Vector2[] ExpandPolygon(List<Vector2> vertices, float radius)
    {
        Vector2[] polygon = vertices.ToArray();
        if (radius <= 0f) return polygon;
        float signedArea = 0f;
        for (int i = 0; i < polygon.Length; i++)
            signedArea += Cross(polygon[i], polygon[(i + 1) % polygon.Length]);
        float sign = signedArea >= 0f ? 1f : -1f;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 previous = (vertices[i] - vertices[(i + vertices.Count - 1) % vertices.Count]).normalized;
            Vector2 next = (vertices[(i + 1) % vertices.Count] - vertices[i]).normalized;
            Vector2 a = new Vector2(previous.y, -previous.x) * sign;
            Vector2 b = new Vector2(next.y, -next.x) * sign;
            Vector2 bisector = (a + b).normalized;
            polygon[i] += bisector * (radius / Mathf.Max(0.1f, Vector2.Dot(bisector, a)));
        }
        return polygon;
    }

    private static Vector2[] Capsule(Vector2 a, Vector2 b, float radius)
    {
        const int halfSegments = 16;
        // Circumscribed approximation never lets the floor enter a round wall.
        radius = Mathf.Max(0.005f, radius) / Mathf.Cos(Mathf.PI / (halfSegments * 2));
        float direction = Mathf.Atan2(b.y - a.y, b.x - a.x);
        var points = new Vector2[(halfSegments + 1) * 2];
        for (int i = 0; i <= halfSegments; i++)
        {
            float angle = direction - Mathf.PI * 0.5f + Mathf.PI * i / halfSegments;
            points[i] = b + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            angle += Mathf.PI;
            points[i + halfSegments + 1] = a + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
