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
        foreach (Vector2[] path in footprint.Paths)
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
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            name = "PotionAreaClipMask",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return new Entry { Texture = texture, Bounds = bounds };
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
