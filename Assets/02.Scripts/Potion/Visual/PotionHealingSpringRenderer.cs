using UnityEngine;

// A single procedural quad supplies the healing spring's beam, orb and rising glints.
internal sealed class PotionHealingSpringRenderer
{
    private static readonly int ColorId = Shader.PropertyToID("_SpringColor");
    private static readonly int AccentId = Shader.PropertyToID("_SpringAccent");
    private static readonly int AnimationId = Shader.PropertyToID("_SpringAnimation");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int GroundMatrixId = Shader.PropertyToID("_SpringGroundFromWorld");
    private static readonly int ClipMaskId = Shader.PropertyToID("_AreaClipMask");
    private static readonly int ClipRectId = Shader.PropertyToID("_AreaClipRect");
    private static readonly int ClipEnabledId = Shader.PropertyToID("_AreaClipEnabled");
    private SpriteRenderer _renderer;
    private Sprite _sprite;
    private Material _material;
    private MaterialPropertyBlock _properties;
    private MaterialPropertyBlock _groundProperties;

    internal void Bind(SpriteRenderer ground, Object context)
    {
        if (_material == null)
        {
            Shader shader = Resources.Load<Shader>("PotionHealingSpring");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("Healing spring shader unavailable; keeping the ground surface.", context);
                return;
            }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        if (_renderer == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            _sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0f), texture.width, 0, SpriteMeshType.FullRect);
            _sprite.name = "HealingSpringQuad";
            _sprite.hideFlags = HideFlags.HideAndDontSave;
            var go = new GameObject("HealingSpringLight");
            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = _sprite;
            _renderer.sharedMaterial = _material;
            _properties = new MaterialPropertyBlock();
            _groundProperties = new MaterialPropertyBlock();
        }
        _renderer.transform.SetParent(ground.transform, false);
        _renderer.transform.localPosition = Vector3.zero;
        _renderer.transform.localRotation = Quaternion.identity;
        _renderer.enabled = true;
    }

    internal void Apply(SpriteRenderer ground, Color color, Color accent, float time, float visibility)
    {
        if (_renderer == null) return;
        _renderer.enabled = ground.enabled && visibility > 0f;
        if (!_renderer.enabled) return;
        ground.GetPropertyBlock(_groundProperties);
        var style = (PotionAreaSurfaceStyle)Mathf.RoundToInt(_groundProperties.GetVector(SurfaceId).w);
        bool vampiric = style == PotionAreaSurfaceStyle.VampiricFire
            || style == PotionAreaSurfaceStyle.VampiricPoison
            || style == PotionAreaSurfaceStyle.VampiricFrost
            || style == PotionAreaSurfaceStyle.VampiricLightning;
        PotionAreaSurfaceRenderer.CopyCombinationProperties(_groundProperties, _properties);
        _properties.SetFloat(ClipEnabledId, _groundProperties.GetFloat(ClipEnabledId));
        _properties.SetVector(ClipRectId, _groundProperties.GetVector(ClipRectId));
        _properties.SetTexture(ClipMaskId, _groundProperties.GetTexture(ClipMaskId) ?? Texture2D.whiteTexture);
        _properties.SetMatrix(GroundMatrixId, ground.transform.worldToLocalMatrix);
        // Cancel the ground ellipse's vertical squash so light rises above its center.
        Vector3 scale = ground.transform.lossyScale;
        float width = ground.bounds.size.x;
        _renderer.transform.localScale = new Vector3(
            width * (vampiric ? 0.55f : 0.8f) / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            width * (vampiric ? 0.55f : 0.85f) / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);
        _renderer.sortingLayerID = ground.sortingLayerID;
        _renderer.sortingOrder = ground.sortingOrder + (vampiric ? 2 : 1);
        _properties.SetColor(ColorId, color);
        _properties.SetColor(AccentId, accent);
        _properties.SetVector(AnimationId, new Vector4(time, visibility, vampiric ? 1f : 0f,
            style == PotionAreaSurfaceStyle.VampiricLightning ? 1f : 0f));
        _renderer.SetPropertyBlock(_properties);
    }

    internal void Hide()
    {
        if (_renderer != null) _renderer.enabled = false;
    }

    internal void Release()
    {
        if (_renderer != null) Object.Destroy(_renderer.gameObject);
        if (_sprite != null) Object.Destroy(_sprite);
        if (_material != null) Object.Destroy(_material);
    }
}
