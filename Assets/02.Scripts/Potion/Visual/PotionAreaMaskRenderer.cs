using UnityEngine;

// Owns GPU resources only. Geometry and effect membership remain in the footprint.
internal sealed class PotionAreaMaskRenderer
{
    private static readonly int MaskId = Shader.PropertyToID("_AreaClipMask");
    private static readonly int RectId = Shader.PropertyToID("_AreaClipRect");
    private static readonly int EnabledId = Shader.PropertyToID("_AreaClipEnabled");
    private Texture2D _texture;
    private Rect _bounds;

    internal void SetFootprint(PotionAreaFootprint footprint)
    {
        Release();
        if (footprint == null || !footprint.IsClipped) return;
        _bounds = footprint.Bounds;
        const int resolution = 256;
        int width = Mathf.Max(1, Mathf.RoundToInt(resolution * _bounds.width /
            Mathf.Max(_bounds.width, _bounds.height)));
        int height = Mathf.Max(1, Mathf.RoundToInt(resolution * _bounds.height /
            Mathf.Max(_bounds.width, _bounds.height)));
        var pixels = new Color32[width * height];
        foreach (Vector2[] path in footprint.Paths)
        {
            Rect pathBounds = PotionAreaFootprint.BoundsOf(path);
            int xMin = Mathf.Clamp(Mathf.FloorToInt((pathBounds.xMin - _bounds.xMin) / _bounds.width * width), 0, width - 1);
            int xMax = Mathf.Clamp(Mathf.CeilToInt((pathBounds.xMax - _bounds.xMin) / _bounds.width * width), 0, width - 1);
            int yMin = Mathf.Clamp(Mathf.FloorToInt((pathBounds.yMin - _bounds.yMin) / _bounds.height * height), 0, height - 1);
            int yMax = Mathf.Clamp(Mathf.CeilToInt((pathBounds.yMax - _bounds.yMin) / _bounds.height * height), 0, height - 1);
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    Vector2 point = _bounds.min + new Vector2((x + 0.5f) / width * _bounds.width,
                        (y + 0.5f) / height * _bounds.height);
                    if (PotionAreaFootprint.Inside(path, point)) pixels[y * width + x] = new Color32(255, 255, 255, 255);
                }
            }
        }
        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            name = "PotionAreaClipMask",
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        _texture.SetPixels32(pixels);
        _texture.Apply(false, true);
    }

    internal void Apply(MaterialPropertyBlock properties)
    {
        properties.SetFloat(EnabledId, _texture != null ? 1f : 0f);
        properties.SetTexture(MaskId, _texture != null ? _texture : Texture2D.whiteTexture);
        properties.SetVector(RectId, new Vector4(_bounds.xMin, _bounds.yMin,
            Mathf.Max(0.0001f, _bounds.width), Mathf.Max(0.0001f, _bounds.height)));
    }

    internal void Release()
    {
        if (_texture == null) return;
        if (Application.isPlaying) Object.Destroy(_texture);
        else Object.DestroyImmediate(_texture);
        _texture = null;
    }
}
