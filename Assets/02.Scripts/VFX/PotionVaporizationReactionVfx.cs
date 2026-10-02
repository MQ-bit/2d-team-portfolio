using UnityEngine;

// Copies the footprint before the source puddle is removed.
internal static class PotionVaporizationReactionVfx
{
    internal static void Spawn(PotionVaporizationSettings settings, Collider2D area,
        SpriteRenderer sortingRenderer)
    {
        if (settings == null) return;
        Spawn(settings.ReactionVfxPrefab, settings.ReactionVfxDuration, area, sortingRenderer);
    }

    internal static void Spawn(GameObject prefab, float duration, Collider2D area,
        SpriteRenderer sortingRenderer)
    {
        if (prefab == null || duration <= 0f)
        {
            return;
        }

        if (prefab.GetComponent<SpriteSheetPlayer>() == null || prefab.GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogWarning("기화 반응 VFX에는 SpriteSheetPlayer와 SpriteRenderer가 필요합니다.", prefab);
            return;
        }

        Bounds bounds = area.bounds;
        var go = new GameObject("VaporizationReactionVfx");
        go.transform.position = bounds.center;
        // Retain sorting without retaining a reference to the removed puddle.
        var sorting = go.AddComponent<SpriteRenderer>();
        sorting.enabled = false;
        if (sortingRenderer != null)
        {
            sorting.sortingLayerID = sortingRenderer.sortingLayerID;
            sorting.sortingOrder = sortingRenderer.sortingOrder;
        }

        var field = go.AddComponent<VFXBurstField>();
        field.ConfigureArea(bounds.size.x, bounds.size.y, sorting);
        var vfxSettings = prefab.GetComponent<PotionAreaVfxSettings>();
        bool centeredExplosion = vfxSettings != null && vfxSettings.playOnce &&
            vfxSettings.fillArea && vfxSettings.burstCount == 1;
        // A single explosion fills the bounds; scattered bursts follow the copied footprint.
        if (!centeredExplosion && area is PolygonCollider2D polygon)
        {
            var paths = new Vector2[polygon.pathCount][];
            for (int path = 0; path < paths.Length; path++)
            {
                Vector2[] points = polygon.GetPath(path);
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = go.transform.InverseTransformPoint(
                        polygon.transform.TransformPoint(points[i] + polygon.offset));
                }

                paths[path] = points;
            }
            field.ConfigureFootprint(paths);
        }
        field.SetVfx(prefab);
        Object.Destroy(go, duration);
    }
}
