#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// Run in an isolated Unity project with the Cliping sources copied into Assets.
public static class ClippingValidation
{
    private static int _checks;
    public static void Run()
    {
        try
        {
            CornerContinuityChecks();
            Vector2[] square = Box(-1f, -1f, 1f, 1f);
            var thinWall = new[] { Box(0.1f, -2f, 0.12f, 2f) };
            PotionAreaFootprint left = PotionAreaClipper.Subtract(square, new Vector2(-0.5f, 0f), thinWall);
            Check(left != null && left.Contains(new Vector2(-0.5f, 0f)), "Landing side retained");
            Check(!left.Contains(new Vector2(0.11f, 0f)), "Wall interior excluded");
            Check(!left.Contains(new Vector2(0.5f, 0f)), "Thin wall's far side discarded");
            CheckCollider(left, new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f));

            PotionAreaFootprint hole = PotionAreaClipper.Subtract(square, new Vector2(-0.7f, 0f),
                new[] { Box(-0.2f, -0.2f, 0.2f, 0.2f) });
            Check(hole != null && !hole.Contains(Vector2.zero), "Pillar hole retained");
            Check(hole.Contains(new Vector2(0.7f, 0f)), "Connected floor around pillar retained");
            CheckCollider(hole, new Vector2(0.7f, 0f), Vector2.zero);
            for (int i = 0; i < 1000; i++) Check(hole.Contains(hole.SamplePoint()), "Sample stays in valid floor");

            Check(PotionAreaClipper.Subtract(square, Vector2.zero,
                new[] { Box(-2f, -2f, 2f, 2f) }) == null, "Fully blocked floor rejected");
            Check(PotionAreaClipper.Subtract(square, Vector2.zero,
                new[] { Box(-0.2f, -0.2f, 0.2f, 0.2f) }) == null, "Blocked landing point rejected");
            PotionAreaFootprint corner = PotionAreaClipper.Subtract(square, new Vector2(-0.7f, -0.7f),
                new[] { Box(0f, -2f, 2f, 2f), Box(-2f, 0f, 2f, 2f) });
            Check(corner != null && corner.Contains(new Vector2(-0.7f, -0.7f)) &&
                !corner.Contains(new Vector2(-0.7f, 0.7f)), "Multiple walls form a clipped corner");

            Vector2[] shifted = Box(9f, 19f, 11f, 21f);
            PotionAreaFootprint offset = PotionAreaClipper.Subtract(shifted, new Vector2(9.5f, 20f),
                new[] { Box(10.1f, 18f, 10.2f, 22f) });
            Check(offset != null && offset.Contains(new Vector2(9.5f, 20f)) &&
                !offset.Contains(new Vector2(10.5f, 20f)), "World-space mesh origin retained");
            QueryChecks();
            TilemapCheck();
            ShaderChecks(hole);
            Debug.Log($"CLIPPING_VALIDATION_PASS: {_checks} checks");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void QueryChecks()
    {
        var wallObject = new GameObject("Tilemap_Wall");
        try
        {
            var box = wallObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.2f, 4f);
            wallObject.transform.position = new Vector2(0.5f, 0f);
            Check(PotionAreaClipper.Clip(Box(-1f, -1f, 1f, 1f), Vector2.zero, default)
                ?.Contains(new Vector2(0.8f, 0f)) == false, "Default named wall clipped");
            wallObject.name = "CustomWall";
            wallObject.layer = 8;
            Check(PotionAreaClipper.Clip(Box(-1f, -1f, 1f, 1f), Vector2.zero, 1 << 8)
                ?.Contains(new Vector2(0.8f, 0f)) == false, "Custom wall layer clipped");
            box.isTrigger = true;
            Check(PotionAreaClipper.Clip(Box(-1f, -1f, 1f, 1f), Vector2.zero, 1 << 8)
                ?.Contains(new Vector2(0.8f, 0f)) == true, "Trigger excluded");
            UnityEngine.Object.DestroyImmediate(box);
            var circle = wallObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.25f;
            var body = wallObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            wallObject.transform.rotation = Quaternion.Euler(0f, 0f, 35f);
            PotionAreaFootprint round = PotionAreaClipper.Clip(Box(-1f, -1f, 1f, 1f), Vector2.zero, 1 << 8);
            Check(round != null && !round.Contains(new Vector2(0.5f, 0f)) &&
                round.Contains(new Vector2(-0.7f, 0f)), "Round wall with body transform clipped");
        }
        finally { UnityEngine.Object.DestroyImmediate(wallObject); }
    }

    private static void CornerContinuityChecks()
    {
        var ellipse = new Vector2[128];
        for (int i = 0; i < ellipse.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / ellipse.Length;
            ellipse[i] = new Vector2(Mathf.Cos(angle) * 1.5f, Mathf.Sin(angle) * 0.75f);
        }
        for (int i = 0; i < 20; i++)
        {
            float left = 0.1f + i * 0.03f;
            float bottom = -0.4f + i * 0.03f;
            var wall = Box(left, bottom, 2f, 2f);
            PotionAreaFootprint floor = PotionAreaClipper.Subtract(ellipse,
                new Vector2(-0.5f, 0f), new[] { wall });
            Vector2 underWall = new(left + 0.1f, bottom - 0.1f);
            Check(floor != null && floor.Contains(underWall),
                $"Ellipse remains connected below wall corner {i}: {underWall}");
        }
        Debug.Log("CORNER_CONTINUITY_NATIVE_PASS");

        // Captured from RealEnemyTestScene_mg while reproducing the half-cut floor.
        Vector2 landing = new(7.6500001f, 0.708735168f);
        for (int i = 0; i < ellipse.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / ellipse.Length;
            ellipse[i] = landing + new Vector2(Mathf.Cos(angle) * 3f, Mathf.Sin(angle) * 1.5f);
        }
        var tiles = new List<Vector2[]>();
        for (int row = 0; row < 3; row++)
        for (int column = 0; column < 4; column++)
        {
            float bottom = row == 0 ? 0.3125f : row;
            float right = column == 3 ? 7.5f : 5f + column;
            tiles.Add(row == 0 && column == 3
                ? new[] { new Vector2(7.5f, 0.4375f), new Vector2(7.5f, 1f),
                    new Vector2(7f, 1f), new Vector2(7f, 0.3125f), new Vector2(7.375f, 0.3125f) }
                : Box(4f + column, bottom, right, row + 1f));
        }
        PotionAreaFootprint tileCorner = PotionAreaClipper.Subtract(ellipse, landing, tiles);
        Check(tileCorner != null && tileCorner.Contains(new Vector2(6f, 0f)),
            "Captured tile corner retains the floor below the wall, not just the right half");
        Check(!tileCorner.Contains(new Vector2(6f, 0.5f)), "Captured tile wall remains excluded");
        CheckCollider(tileCorner, new Vector2(6f, 0f), new Vector2(6f, 0.5f));
        for (int rotation = 1; rotation < 8; rotation++)
        {
            Quaternion turn = Quaternion.Euler(0f, 0f, rotation * 45f);
            Vector2 Rotate(Vector2 p) => landing + (Vector2)(turn * (Vector3)(p - landing));
            var rotatedOutline = new Vector2[ellipse.Length];
            for (int i = 0; i < ellipse.Length; i++) rotatedOutline[i] = Rotate(ellipse[i]);
            var rotatedTiles = new List<Vector2[]>();
            foreach (Vector2[] tile in tiles)
            {
                var rotatedTile = new Vector2[tile.Length];
                for (int i = 0; i < tile.Length; i++) rotatedTile[i] = Rotate(tile[i]);
                rotatedTiles.Add(rotatedTile);
            }
            PotionAreaFootprint rotatedFloor = PotionAreaClipper.Subtract(rotatedOutline, landing, rotatedTiles);
            Check(rotatedFloor != null && rotatedFloor.Contains(Rotate(new Vector2(6f, 0f))),
                $"Captured corner retains connected floor at {rotation * 45} degrees");
            Check(!rotatedFloor.Contains(Rotate(new Vector2(6f, 0.5f))),
                $"Captured corner excludes wall at {rotation * 45} degrees");
        }

        // A native collider mesh may triangulate adjacent convex pieces separately:
        // one piece has a long edge, and its neighbor splits that edge in two.
        Vector3[] vertices =
        {
            new(0f, 0f), new(0f, 2f), new(-2f, 1f),
            new(0f, 0f), new(1f, 0.5f), new(0f, 1f),
            new(0f, 1f), new(1f, 1.5f), new(0f, 2f)
        };
        PotionAreaFootprint joined = PotionAreaFootprint.ConnectedMesh(vertices,
            new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, new Vector2(-0.5f, 1f));
        Check(joined != null && joined.Contains(new Vector2(0.2f, 0.5f)) &&
            joined.Contains(new Vector2(0.2f, 1.5f)), "Partial shared edges preserve connected floor");
    }

    private static void ShaderChecks(PotionAreaFootprint footprint)
    {
        var mask = new PotionAreaMaskRenderer();
        var properties = new MaterialPropertyBlock();
        try
        {
            mask.SetFootprint(footprint);
            mask.Apply(properties);
            Check(properties.GetFloat("_AreaClipEnabled") == 1f &&
                properties.GetTexture("_AreaClipMask") != null, "Rendering mask bound");
            foreach (string resource in new[] { "PotionAreaVisual", "PotionAreaVfxClip" })
            {
                Shader shader = Resources.Load<Shader>(resource);
                Check(shader != null, "Shader resource exists: " + resource);
                var material = new Material(shader);
                try
                {
                    for (int pass = 0; pass < material.passCount; pass++)
                        Check(material.SetPass(pass), "Shader pass compiles: " + resource);
                    Check(!ShaderUtil.ShaderHasError(shader), "Shader has no compiler errors: " + resource);
                }
                finally { UnityEngine.Object.DestroyImmediate(material); }
            }
            mask.SetFootprint(null);
            mask.Apply(properties);
            Check(properties.GetFloat("_AreaClipEnabled") == 0f, "Mask resets on unclipped floor");
        }
        finally { mask.Release(); }
    }

    private static void TilemapCheck()
    {
        var gridObject = new GameObject("ValidationGrid", typeof(Grid));
        var wallObject = new GameObject("Tilemap_Wall", typeof(Tilemap), typeof(TilemapCollider2D));
        var tile = ScriptableObject.CreateInstance<Tile>();
        try
        {
            wallObject.transform.SetParent(gridObject.transform);
            tile.colliderType = Tile.ColliderType.Grid;
            Tilemap map = wallObject.GetComponent<Tilemap>();
            for (int y = -2; y <= 2; y++) map.SetTile(new Vector3Int(0, y, 0), tile);
            wallObject.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            PotionAreaFootprint area = PotionAreaClipper.Clip(Box(-2f, -1f, 2f, 1f),
                new Vector2(-0.5f, 0f), default);
            Check(area != null && !area.Contains(new Vector2(0.5f, 0f)) &&
                !area.Contains(new Vector2(1.5f, 0f)), "Tilemap wall and its far side excluded");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(gridObject);
            UnityEngine.Object.DestroyImmediate(tile);
        }
    }

    private static void CheckCollider(PotionAreaFootprint area, Vector2 inside, Vector2 outside)
    {
        var go = new GameObject("ValidationCollider");
        try
        {
            go.transform.position = new Vector3(3f, -2f, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, 27f);
            go.transform.localScale = new Vector3(2f, 0.5f, 1f);
            var collider = go.AddComponent<PolygonCollider2D>();
            collider.offset = new Vector2(0.2f, -0.1f);
            area.ApplyTo(collider);
            Physics2D.SyncTransforms();
            Check(collider.OverlapPoint(inside), "Collider contains valid point under transformed parent");
            Check(!collider.OverlapPoint(outside), "Collider excludes removed point under transformed parent");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    private static Vector2[] Box(float left, float bottom, float right, float top) => new[]
    {
        new Vector2(left, bottom), new Vector2(right, bottom),
        new Vector2(right, top), new Vector2(left, top)
    };
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("Clipping validation failed: " + description);
        _checks++;
    }
}
#endif
