using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// Share only the exact immutable footprint object. No quantization or resolution change.
internal sealed class OptimizedPotionAreaMaskRenderer
{
    private sealed class Entry
    {
        internal Texture2D Texture;
        internal Rect Bounds;
        internal int References;
    }
    private static readonly Dictionary<PotionAreaFootprint, Entry> Cache = new();
    private static readonly int MaskId = Shader.PropertyToID("_AreaClipMask");
    private static readonly int RectId = Shader.PropertyToID("_AreaClipRect");
    private static readonly int EnabledId = Shader.PropertyToID("_AreaClipEnabled");
    private static readonly ProfilerMarker RasterMarker = new("Optimization.Mask.RasterizeAndUpload");
    private static readonly ProfilerMarker PixelMarker = new("Optimization.Mask.PixelTest");
    private static readonly ProfilerMarker UploadMarker = new("Optimization.Mask.TextureUpload");
    private PotionAreaFootprint _footprint;
    private Entry _entry;
    private Rect _bounds;
    internal static int CachedFootprints => Cache.Count;

    internal void SetFootprint(PotionAreaFootprint footprint)
    {
        if (ReferenceEquals(_footprint, footprint)) return;
        Release();
        if (footprint == null || !footprint.IsClipped) return;
        if (!Cache.TryGetValue(footprint, out Entry entry))
        {
            entry = Create(footprint);
            Cache.Add(footprint, entry);
        }
        entry.References++;
        _footprint = footprint;
        _entry = entry;
        _bounds = entry.Bounds;
    }
    private static Entry Create(PotionAreaFootprint footprint)
    {
        using var marker = RasterMarker.Auto();
        Rect bounds = footprint.Bounds;
        const int resolution = 256;
        int width = Mathf.Max(1, Mathf.RoundToInt(resolution * bounds.width /
            Mathf.Max(bounds.width, bounds.height)));
        int height = Mathf.Max(1, Mathf.RoundToInt(resolution * bounds.height /
            Mathf.Max(bounds.width, bounds.height)));
        var pixels = new Color32[width * height];
        bool fast = OptimizationRuntime.Uses(OptimizationVariant.FastMask);
        using (var rasterMarker = PixelMarker.Auto())
        {
            foreach (Vector2[] path in footprint.Paths)
            {
                if (fast && path.Length == 3) FillTriangleSpans(path, bounds, width, height, pixels);
                else FillBoundingBox(path, bounds, width, height, pixels);
            }
        }

        if (fast && VerifyAgainstReference) Verify(footprint, bounds, width, height, pixels);
        Texture2D texture;
        using (var uploadMarker = UploadMarker.Auto())
        {
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "PotionAreaClipMask",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
        }

        return new Entry { Texture = texture, Bounds = bounds };
    }

    // Reference rasterization: tests every pixel of the path's bounding box.
    private static void FillBoundingBox(Vector2[] path, Rect bounds, int width, int height, Color32[] pixels)
    {
        Rect pathBounds = PotionAreaFootprint.BoundsOf(path);
        int xMin = Mathf.Clamp(Mathf.FloorToInt((pathBounds.xMin - bounds.xMin) / bounds.width * width), 0, width - 1);
        int xMax = Mathf.Clamp(Mathf.CeilToInt((pathBounds.xMax - bounds.xMin) / bounds.width * width), 0, width - 1);
        int yMin = Mathf.Clamp(Mathf.FloorToInt((pathBounds.yMin - bounds.yMin) / bounds.height * height), 0, height - 1);
        int yMax = Mathf.Clamp(Mathf.CeilToInt((pathBounds.yMax - bounds.yMin) / bounds.height * height), 0, height - 1);
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                Vector2 point = bounds.min + new Vector2((x + 0.5f) / width * bounds.width,
                    (y + 0.5f) / height * bounds.height);
                if (PotionAreaFootprint.Inside(path, point)) pixels[y * width + x] = new Color32(255, 255, 255, 255);
            }
        }
    }

    // Same per-pixel predicate as the reference, but only for pixels near the triangle's span on each row.
    // The span is widened by one pixel and a small y tolerance, so the result stays identical to the reference.
    private static void FillTriangleSpans(Vector2[] triangle, Rect bounds, int width, int height, Color32[] pixels)
    {
        const float YTolerance = 0.0001f;
        Rect pathBounds = PotionAreaFootprint.BoundsOf(triangle);
        int xMin = Mathf.Clamp(Mathf.FloorToInt((pathBounds.xMin - bounds.xMin) / bounds.width * width), 0, width - 1);
        int xMax = Mathf.Clamp(Mathf.CeilToInt((pathBounds.xMax - bounds.xMin) / bounds.width * width), 0, width - 1);
        int yMin = Mathf.Clamp(Mathf.FloorToInt((pathBounds.yMin - bounds.yMin) / bounds.height * height), 0, height - 1);
        int yMax = Mathf.Clamp(Mathf.CeilToInt((pathBounds.yMax - bounds.yMin) / bounds.height * height), 0, height - 1);
        for (int y = yMin; y <= yMax; y++)
        {
            float rowY = bounds.min.y + (y + 0.5f) / height * bounds.height;
            float low = float.MaxValue, high = float.MinValue;
            for (int edge = 0; edge < 3; edge++)
            {
                Vector2 a = triangle[edge], b = triangle[(edge + 1) % 3];
                float minY = Mathf.Min(a.y, b.y) - YTolerance, maxY = Mathf.Max(a.y, b.y) + YTolerance;
                if (rowY < minY || rowY > maxY) continue;
                float dy = b.y - a.y;
                if (Mathf.Abs(dy) < 1e-9f)
                {
                    low = Mathf.Min(low, Mathf.Min(a.x, b.x));
                    high = Mathf.Max(high, Mathf.Max(a.x, b.x));
                    continue;
                }

                float t = Mathf.Clamp01((rowY - a.y) / dy);
                float crossing = a.x + (b.x - a.x) * t;
                low = Mathf.Min(low, crossing);
                high = Mathf.Max(high, crossing);
            }

            if (low > high) continue;
            int from = Mathf.Max(xMin, Mathf.FloorToInt((low - bounds.xMin) / bounds.width * width - 0.5f) - 1);
            int to = Mathf.Min(xMax, Mathf.CeilToInt((high - bounds.xMin) / bounds.width * width - 0.5f) + 1);
            for (int x = from; x <= to; x++)
            {
                Vector2 point = bounds.min + new Vector2((x + 0.5f) / width * bounds.width,
                    (y + 0.5f) / height * bounds.height);
                if (PotionAreaFootprint.Inside(triangle, point)) pixels[y * width + x] = new Color32(255, 255, 255, 255);
            }
        }
    }

    // Verification mode (--optimization-verify-mask): compare against the reference and report any difference.
    internal static bool VerifyAgainstReference;
    internal static int VerifiedMasks;
    internal static int MismatchedMasks;

    private static void Verify(PotionAreaFootprint footprint, Rect bounds, int width, int height, Color32[] fast)
    {
        var reference = new Color32[width * height];
        foreach (Vector2[] path in footprint.Paths) FillBoundingBox(path, bounds, width, height, reference);
        int different = 0;
        for (int i = 0; i < reference.Length; i++)
        {
            if (reference[i].a != fast[i].a) different++;
        }

        VerifiedMasks++;
        if (different > 0) MismatchedMasks++;
        Debug.Log("MaskVerify size=" + width + "x" + height + " triangles=" + footprint.Paths.Count + " differentPixels=" + different);
    }

    internal void Apply(MaterialPropertyBlock properties)
    {
        properties.SetFloat(EnabledId, _entry != null ? 1f : 0f);
        properties.SetTexture(MaskId, _entry != null ? _entry.Texture : Texture2D.whiteTexture);
        properties.SetVector(RectId, new Vector4(_bounds.xMin, _bounds.yMin,
            Mathf.Max(0.0001f, _bounds.width), Mathf.Max(0.0001f, _bounds.height)));
    }
    internal void Release()
    {
        if (_entry == null) return;
        if (--_entry.References == 0)
        {
            Cache.Remove(_footprint);
            DestroyTexture(_entry.Texture);
        }
        _entry = null;
        _footprint = null;
    }
    private static void DestroyTexture(Texture2D texture)
    {
        if (Application.isPlaying) Object.Destroy(texture);
        else Object.DestroyImmediate(texture);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        foreach (Entry entry in Cache.Values) DestroyTexture(entry.Texture);
        Cache.Clear();
    }
}
