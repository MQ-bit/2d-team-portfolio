using System.Collections.Generic;
using UnityEngine;

// Geometry only: native Boolean subtraction, followed by connected-component selection.
internal static class PotionAreaClipper
{
    internal static PotionAreaFootprint Clip(Vector2[] outline, Vector2 seed, LayerMask wallLayers)
    {
        var walls = PotionWallQuery.FindPolygons(PotionAreaFootprint.BoundsOf(outline), wallLayers);
        if (walls.Count == 0) return PotionAreaFootprint.Unclipped(outline);
        return Subtract(outline, seed, walls);
    }

    internal static PotionAreaFootprint Subtract(Vector2[] outline, Vector2 seed,
        IReadOnlyList<Vector2[]> walls)
    {
        // Copies are calculation-only; original wall colliders and bodies are untouched.
        var scratch = new GameObject("PotionClippingGeometry") { hideFlags = HideFlags.HideAndDontSave };
        scratch.layer = 2; // Ignore Raycast
        Vector2 origin = PotionAreaFootprint.BoundsOf(outline).center;
        scratch.transform.position = origin;
        Mesh mesh = null;
        try
        {
            var body = scratch.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var composite = scratch.AddComponent<CompositeCollider2D>();
            composite.generationType = CompositeCollider2D.GenerationType.Manual;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            // Default convex decomposition can leave non-collinear seams at
            // concave tile corners. Use a conforming triangulation so the mesh
            // connectivity matches the actual connected floor outline.
            composite.useDelaunayMesh = true;
            composite.vertexDistance = 0.0001f;
            composite.offsetDistance = 0.0001f;
            composite.isTrigger = true;
            AddPolygon(scratch, outline, origin, Collider2D.CompositeOperation.Merge, 0);
            for (int i = 0; i < walls.Count; i++)
                AddPolygon(scratch, walls[i], origin, Collider2D.CompositeOperation.Difference, i + 1);
            composite.GenerateGeometry();
            mesh = composite.CreateMesh(false, false);
            if (mesh == null || mesh.vertexCount == 0) return null;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] += (Vector3)origin;
            return PotionAreaFootprint.ConnectedMesh(vertices, mesh.triangles, seed);
        }
        finally
        {
            scratch.SetActive(false);
            Release(mesh);
            Release(scratch);
        }
    }

    private static void AddPolygon(GameObject scratch, Vector2[] points, Vector2 origin,
        Collider2D.CompositeOperation operation, int order)
    {
        var local = new Vector2[points.Length];
        for (int i = 0; i < local.Length; i++) local[i] = points[i] - origin;
        var polygon = scratch.AddComponent<PolygonCollider2D>();
        polygon.points = local;
        polygon.compositeOrder = order;
        polygon.compositeOperation = operation;
    }

    private static void Release(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }
}
