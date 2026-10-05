using System;
using System.Collections.Generic;
using UnityEngine;

// Immutable world-space filled paths. Clipped paths are triangles, so holes cannot
// accidentally become filled PolygonCollider2D paths. Rendering uses the same paths.
internal sealed class PotionAreaFootprint
{
    private readonly Vector2[][] _paths;
    private readonly float[] _areas;
    private readonly float _totalArea;
    internal Rect Bounds { get; }
    internal bool IsClipped { get; }
    internal IReadOnlyList<Vector2[]> Paths => _paths;

    private PotionAreaFootprint(Vector2[][] paths, bool clipped)
    {
        _paths = paths;
        IsClipped = clipped;
        Vector2 minimum = paths[0][0], maximum = minimum;
        _areas = new float[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            float area = 0f;
            Vector2[] path = paths[i];
            for (int j = 0; j < path.Length; j++)
            {
                minimum = Vector2.Min(minimum, path[j]);
                maximum = Vector2.Max(maximum, path[j]);
                area += Cross(path[j], path[(j + 1) % path.Length]);
            }
            _totalArea += Mathf.Abs(area) * 0.5f;
            _areas[i] = _totalArea;
        }
        Bounds = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    internal static PotionAreaFootprint Unclipped(Vector2[] outline) =>
        new(new[] { (Vector2[])outline.Clone() }, false);

    internal static Rect BoundsOf(Vector2[] points)
    {
        Vector2 minimum = points[0], maximum = minimum;
        foreach (Vector2 point in points)
        {
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }
        return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
    }

    internal bool Contains(Vector2 worldPoint)
    {
        if (worldPoint.x < Bounds.xMin || worldPoint.x > Bounds.xMax ||
            worldPoint.y < Bounds.yMin || worldPoint.y > Bounds.yMax)
        {
            return false;
        }

        foreach (Vector2[] path in _paths)
            if (Inside(path, worldPoint)) return true;
        return false;
    }

    internal Vector2 SamplePoint()
    {
        if (IsClipped)
        {
            float area = UnityEngine.Random.value * _totalArea;
            int index = Array.BinarySearch(_areas, area);
            if (index < 0) index = ~index;
            Vector2[] triangle = _paths[Mathf.Min(index, _paths.Length - 1)];
            float u = Mathf.Sqrt(UnityEngine.Random.value), v = UnityEngine.Random.value;
            return triangle[0] * (1f - u) + triangle[1] * (u * (1f - v)) + triangle[2] * (u * v);
        }
        for (int attempt = 0; attempt < 256; attempt++)
        {
            Vector2 point = new(UnityEngine.Random.Range(Bounds.xMin, Bounds.xMax),
                UnityEngine.Random.Range(Bounds.yMin, Bounds.yMax));
            if (Contains(point)) return point;
        }
        // Unclipped potion outlines contain their bounds center.
        return Bounds.center;
    }

    internal void ApplyTo(PolygonCollider2D collider)
    {
        collider.enabled = false;
        collider.pathCount = _paths.Length;
        for (int i = 0; i < _paths.Length; i++)
        {
            var local = new Vector2[_paths[i].Length];
            for (int j = 0; j < local.Length; j++)
                local[j] = (Vector2)collider.transform.InverseTransformPoint(_paths[i][j]) - collider.offset;
            collider.SetPath(i, local);
        }
        collider.enabled = true;
    }

    // Native meshes can triangulate neighboring pieces independently. Besides
    // duplicate vertices, a long boundary can meet several shorter boundaries.
    // Join overlapping edge segments, but never connect by a lone touching vertex.
    internal static PotionAreaFootprint ConnectedMesh(Vector3[] vertices, int[] indices, Vector2 seed)
    {
        int count = indices.Length / 3;
        var ids = new int[vertices.Length];
        var welded = new Dictionary<Vector2Int, int>();
        var positions = new List<Vector2>();
        for (int i = 0; i < vertices.Length; i++)
        {
            var key = new Vector2Int(Mathf.RoundToInt(vertices[i].x * 100000f),
                Mathf.RoundToInt(vertices[i].y * 100000f));
            if (!welded.TryGetValue(key, out int id))
            {
                welded.Add(key, id = welded.Count);
                positions.Add(vertices[i]);
            }
            ids[i] = id;
        }
        var edges = new Dictionary<ulong, List<int>>();
        var triangles = new Vector2[count][];
        int start = -1;
        for (int i = 0; i < count; i++)
        {
            triangles[i] = new[] { (Vector2)vertices[indices[i * 3]],
                (Vector2)vertices[indices[i * 3 + 1]], (Vector2)vertices[indices[i * 3 + 2]] };
            if (Inside(triangles[i], seed)) start = i;
            for (int e = 0; e < 3; e++)
            {
                ulong key = Edge(ids[indices[i * 3 + e]], ids[indices[i * 3 + (e + 1) % 3]]);
                if (!edges.TryGetValue(key, out List<int> owners)) edges.Add(key, owners = new List<int>(2));
                owners.Add(i);
            }
        }
        if (start < 0) return null; // The actual landing point is blocked: don't relocate the floor.
        var neighbors = new List<int>[count];
        for (int i = 0; i < count; i++) neighbors[i] = new List<int>(3);
        var boundaryEdges = new List<(int Triangle, Vector2 A, Vector2 B)>();
        foreach (var pair in edges)
        {
            List<int> owners = pair.Value;
            if (owners.Count == 1)
            {
                boundaryEdges.Add((owners[0], positions[(int)(pair.Key >> 32)],
                    positions[(int)(pair.Key & uint.MaxValue)]));
                continue;
            }
            for (int a = 0; a < owners.Count; a++)
            {
                for (int b = a + 1; b < owners.Count; b++)
                    Connect(neighbors, owners[a], owners[b]);
            }
        }
        // Only unmatched edges need the more expensive partial-overlap test.
        for (int i = 0; i < boundaryEdges.Count; i++)
        {
            for (int j = i + 1; j < boundaryEdges.Count; j++)
            {
                var a = boundaryEdges[i];
                var b = boundaryEdges[j];
                if (a.Triangle != b.Triangle && ShareSegment(a.A, a.B, b.A, b.B))
                    Connect(neighbors, a.Triangle, b.Triangle);
            }
        }

        var visited = new bool[count];
        var queue = new Queue<int>();
        var selected = new List<Vector2[]>();
        visited[start] = true;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            selected.Add(triangles[i]);
            foreach (int neighbor in neighbors[i])
            {
                if (visited[neighbor]) continue;
                visited[neighbor] = true;
                queue.Enqueue(neighbor);
            }
        }
        return new PotionAreaFootprint(selected.ToArray(), true);
    }

    private static ulong Edge(int a, int b) => ((ulong)(uint)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);

    private static void Connect(List<int>[] neighbors, int a, int b)
    {
        neighbors[a].Add(b);
        neighbors[b].Add(a);
    }

    private static bool ShareSegment(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        const float tolerance = 0.00001f;
        Vector2 delta = b - a;
        float length = delta.magnitude;
        if (length <= tolerance || (d - c).sqrMagnitude <= tolerance * tolerance) return false;
        Vector2 direction = delta / length;
        if (Mathf.Abs(Cross(direction, c - a)) > tolerance ||
            Mathf.Abs(Cross(direction, d - a)) > tolerance)
        {
            return false;
        }

        float cPosition = Vector2.Dot(c - a, direction);
        float dPosition = Vector2.Dot(d - a, direction);
        float overlap = Mathf.Min(length, Mathf.Max(cPosition, dPosition)) -
            Mathf.Max(0f, Mathf.Min(cPosition, dPosition));
        return overlap > tolerance;
    }

    internal static bool Inside(Vector2[] polygon, Vector2 point)
    {
        if (polygon.Length == 3)
        {
            float a = Cross(polygon[1] - polygon[0], point - polygon[0]);
            float b = Cross(polygon[2] - polygon[1], point - polygon[1]);
            float c = Cross(polygon[0] - polygon[2], point - polygon[2]);
            return (a >= -0.0000001f && b >= -0.0000001f && c >= -0.0000001f) ||
                (a <= 0.0000001f && b <= 0.0000001f && c <= 0.0000001f);
        }
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.y > point.y) != (b.y > point.y) &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
