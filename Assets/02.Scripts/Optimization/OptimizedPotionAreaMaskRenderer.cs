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
    internal static void FillBoundingBox(Vector2[] path, Rect bounds, int width, int height, Color32[] pixels)
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

    // Same predicate as the reference, but only for pixels near the ends of the triangle's span on each row;
    // the interior is filled directly. The span is widened by one pixel and a small y tolerance, so the result
    // stays identical to the reference (checked by --optimization-verify-mask).
    // Written with plain floats on purpose: Development Mono builds do not inline Mathf/Vector2 helpers,
    // and those calls, repeated for every row of every triangle, were the real cost.
    internal static void FillTriangleSpans(Vector2[] triangle, Rect bounds, int width, int height, Color32[] pixels)
    {
        const float YTolerance = 0.0001f;
        float x0 = triangle[0].x, y0 = triangle[0].y, x1 = triangle[1].x, y1 = triangle[1].y;
        float x2 = triangle[2].x, y2 = triangle[2].y;
        Rect pathBounds = PotionAreaFootprint.BoundsOf(triangle);
        int xMin = Mathf.Clamp(Mathf.FloorToInt((pathBounds.xMin - bounds.xMin) / bounds.width * width), 0, width - 1);
        int xMax = Mathf.Clamp(Mathf.CeilToInt((pathBounds.xMax - bounds.xMin) / bounds.width * width), 0, width - 1);
        int yMin = Mathf.Clamp(Mathf.FloorToInt((pathBounds.yMin - bounds.yMin) / bounds.height * height), 0, height - 1);
        int yMax = Mathf.Clamp(Mathf.CeilToInt((pathBounds.yMax - bounds.yMin) / bounds.height * height), 0, height - 1);
        float boundsX = bounds.xMin, boundsY = bounds.yMin;
        float boundsWidth = bounds.width, boundsHeight = bounds.height;
        float pixelsPerUnit = width / boundsWidth;
        var white = new Color32(255, 255, 255, 255);
        for (int y = yMin; y <= yMax; y++)
        {
            float rowY = boundsY + (y + 0.5f) / height * boundsHeight;
            float low = float.MaxValue, high = float.MinValue;
            AddCrossing(x0, y0, x1, y1, rowY, YTolerance, ref low, ref high);
            AddCrossing(x1, y1, x2, y2, rowY, YTolerance, ref low, ref high);
            AddCrossing(x2, y2, x0, y0, rowY, YTolerance, ref low, ref high);
            if (low > high) continue;
            float lowPixel = (low - boundsX) * pixelsPerUnit - 0.5f;
            float highPixel = (high - boundsX) * pixelsPerUnit - 0.5f;
            int from = FloorInt(lowPixel) - 1;
            int to = CeilInt(highPixel) + 1;
            if (from < xMin) from = xMin;
            if (to > xMax) to = xMax;
            // A triangle is convex, so its row is exactly [low, high]. Pixels one pixel inside that span are
            // inside without asking; only the pixels near either end use the reference predicate.
            int solidFrom = CeilInt(lowPixel) + 1;
            int solidTo = FloorInt(highPixel) - 1;
            if (solidFrom < from) solidFrom = from;
            if (solidTo > to) solidTo = to;
            int row = y * width;
            for (int x = from; x <= to; x++)
            {
                if (x == solidFrom && solidFrom <= solidTo)
                {
                    for (; x <= solidTo; x++) pixels[row + x] = white;
                    x = solidTo;
                    continue;
                }

                Vector2 point = bounds.min + new Vector2((x + 0.5f) / width * boundsWidth,
                    (y + 0.5f) / height * boundsHeight);
                if (PotionAreaFootprint.Inside(triangle, point)) pixels[row + x] = white;
            }
        }
    }

    private static void AddCrossing(float ax, float ay, float bx, float by, float rowY, float tolerance,
        ref float low, ref float high)
    {
        float minY = ay < by ? ay : by, maxY = ay < by ? by : ay;
        if (rowY < minY - tolerance || rowY > maxY + tolerance) return;
        float dy = by - ay;
        if (dy < 1e-9f && dy > -1e-9f)
        {
            float minX = ax < bx ? ax : bx, maxX = ax < bx ? bx : ax;
            if (minX < low) low = minX;
            if (maxX > high) high = maxX;
            return;
        }

        float t = (rowY - ay) / dy;
        t = t < 0f ? 0f : (t > 1f ? 1f : t);
        float crossing = ax + (bx - ax) * t;
        if (crossing < low) low = crossing;
        if (crossing > high) high = crossing;
    }

    private static int FloorInt(float value)
    {
        int whole = (int)value;
        return value < whole ? whole - 1 : whole;
    }

    private static int CeilInt(float value)
    {
        int whole = (int)value;
        return value > whole ? whole + 1 : whole;
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
