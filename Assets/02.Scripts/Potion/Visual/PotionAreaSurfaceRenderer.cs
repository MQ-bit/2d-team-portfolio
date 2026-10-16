using UnityEngine;

// 셰이더 리소스, 속성 전달, 렌더러 교체와 복원을 담당한다.
internal sealed class PotionAreaSurfaceRenderer
{
    private static readonly int AreaColorId = Shader.PropertyToID("_AreaColor");
    private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int AnimationId = Shader.PropertyToID("_Animation");
    private static readonly int BoundaryId = Shader.PropertyToID("_Boundary");
    private static readonly int PuddleShapeId = Shader.PropertyToID("_PuddleShape");
    private static readonly int PuddleFrameId = Shader.PropertyToID("_PuddleFrame");
    private static readonly int PuddleBoundsId = Shader.PropertyToID("_PuddleBounds");
    private static readonly int PuddleSpreadId = Shader.PropertyToID("_PuddleSpread");
    private static readonly int FirstColorId = Shader.PropertyToID("_PuddleFirstColor");
    private static readonly int SecondColorId = Shader.PropertyToID("_PuddleSecondColor");

    private SpriteRenderer _renderer;
    private Material _material;
    private Sprite _surfaceSprite;
    private MaterialPropertyBlock _properties;
    private MaterialPropertyBlock _originalProperties;
    private Material _originalMaterial;
    private Sprite _originalSprite;
    private Color _originalColor;
    private Vector3 _originalScale;
    private readonly ComparisonAreaMaskRenderer _mask = new();
    private Vector4 _boundarySettings = new(0.17f, 4.5f, 0.35f, 0.45f);

    internal void SetFootprint(PotionAreaFootprint footprint) => _mask.SetFootprint(footprint);
    internal void SetBoundarySettings(Vector4 settings) => _boundarySettings = settings;

    internal static void CopyCombinationProperties(MaterialPropertyBlock source, MaterialPropertyBlock target)
    {
        target.SetVector(PuddleShapeId, source.GetVector(PuddleShapeId));
        target.SetVector(PuddleFrameId, source.GetVector(PuddleFrameId));
        target.SetVector(PuddleBoundsId, source.GetVector(PuddleBoundsId));
        target.SetVector(PuddleSpreadId, source.GetVector(PuddleSpreadId));
    }

    internal bool IsBound { get; private set; }
    internal Material OriginalMaterial => _originalMaterial;

    internal bool Bind(SpriteRenderer renderer, Object context)
    {
        if (!EnsureResources(context)) return false;
        _renderer = renderer;
        _originalMaterial = renderer.sharedMaterial;
        _originalSprite = renderer.sprite;
        _originalColor = renderer.color;
        _originalScale = renderer.transform.localScale;
        _originalProperties ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_originalProperties);

        // 전체 사각형을 사용해 원본 스프라이트의 색상과 메시 잘림을 피한다.
        Vector2 size = _originalSprite.bounds.size;
        renderer.sprite = _surfaceSprite;
        renderer.transform.localScale = new Vector3(
            _originalScale.x * size.x, _originalScale.y * size.y, _originalScale.z);
        renderer.sharedMaterial = _material;
        renderer.color = Color.white;
        IsBound = true;
        return true;
    }

    private bool EnsureResources(Object context)
    {
        if (_material == null)
        {
            Shader shader = Resources.Load<Shader>("PotionAreaVisual");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("PotionAreaVisual shader unavailable; keeping the original sprite.", context);
                return false;
            }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        if (_surfaceSprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            _surfaceSprite = Sprite.Create(texture,
                new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                texture.width, 0, SpriteMeshType.FullRect);
            _surfaceSprite.name = "PotionAreaVisualQuad";
            _surfaceSprite.hideFlags = HideFlags.HideAndDontSave;
        }
        _properties ??= new MaterialPropertyBlock();
        return true;
    }

    internal void Apply(Color color, Color accentColor, Vector4 surface, Vector4 animation,
        PotionPuddleShape puddle, PotionCombinationSpread spread)
    {
        _renderer.GetPropertyBlock(_properties);
        _properties.SetColor(AreaColorId, color);
        _properties.SetColor(AccentColorId, accentColor);
        _properties.SetVector(SurfaceId, surface);
        _properties.SetVector(AnimationId, animation);
        _properties.SetVector(BoundaryId, _boundarySettings);
        _mask.Apply(_properties);
        if (puddle != null)
        {
            Vector2 offset = puddle.BoundsCenter - puddle.Center;
            _properties.SetVector(PuddleShapeId, new Vector4(puddle.HalfDistance,
                puddle.FirstRadius, puddle.SecondRadius, puddle.Blend));
            _properties.SetVector(PuddleFrameId, new Vector4(puddle.Direction.x,
                puddle.Direction.y, puddle.NeckRadius, 1f));
            _properties.SetVector(PuddleBoundsId, new Vector4(puddle.BoundsSize.x,
                puddle.BoundsSize.y, offset.x, offset.y));
        }
        else
        {
            _properties.SetVector(PuddleFrameId, Vector4.zero);
        }

        if (spread != null)
        {
            _properties.SetColor(FirstColorId, spread.FirstColor);
            _properties.SetColor(SecondColorId, spread.SecondColor);
            _properties.SetVector(PuddleSpreadId, new Vector4(spread.Reach,
                Mathf.Max(0.03f, Mathf.Min(puddle.FirstRadius, puddle.SecondRadius) * 0.04f),
                1f, spread.IsComplete ? 1f : 0f));
        }
        else
        {
            _properties.SetVector(PuddleSpreadId, Vector4.zero);
        }
        _renderer.SetPropertyBlock(_properties);
    }

    internal void Restore()
    {
        if (IsBound && _renderer != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
            _renderer.sprite = _originalSprite;
            _renderer.color = _originalColor;
            _renderer.transform.localScale = _originalScale;
            _renderer.SetPropertyBlock(_originalProperties);
        }
        IsBound = false;
    }

    internal void Release()
    {
        Restore();
        _mask.Release();
        if (_material != null) Object.Destroy(_material);
        if (_surfaceSprite != null) Object.Destroy(_surfaceSprite);
    }
}
