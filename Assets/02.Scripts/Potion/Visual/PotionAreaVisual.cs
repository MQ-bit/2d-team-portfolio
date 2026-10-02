using UnityEngine;

// Procedural surface only: colliders and potion effects remain owned by the area.
[DisallowMultipleComponent]
public sealed class PotionAreaVisual : MonoBehaviour
{
    [Header("Surface")]
    [SerializeField, Min(0f)] private float _flowSpeed = 0.7f;
    [SerializeField, Range(0f, 0.025f)] private float _edgeWobble = 0.018f;
    [SerializeField, Range(0f, 1f)] private float _surfaceDetail = 0.65f;
    [SerializeField, Range(0f, 1f)] private float _opacity = 0.85f;

    [Header("Transitions")]
    [SerializeField, Min(0f)] private float _appearDuration = 0.25f;
    [SerializeField, Min(0f)] private float _colorTransitionDuration = 0.35f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.4f;

    private static readonly int AreaColorId = Shader.PropertyToID("_AreaColor");
    private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int AnimationId = Shader.PropertyToID("_Animation");
    private static readonly int PuddleShapeId = Shader.PropertyToID("_PuddleShape");
    private static readonly int PuddleFrameId = Shader.PropertyToID("_PuddleFrame");
    private static readonly int PuddleBoundsId = Shader.PropertyToID("_PuddleBounds");
    private PotionPuddleShape _puddle;
    private PotionCombinationSpread _spread;
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
    private Color _fromColor;
    private Color _targetColor;
    private Color _currentColor;
    private Color _accentColor = Color.white;
    private PotionAreaSurfaceStyle _surfaceStyle;
    // Multipliers keep the Inspector controls useful for every VFX profile.
    private Vector4 _styleSettings = Vector4.one;
    private float _startedAt;
    private float _colorChangedAt;
    private float _expiresAt = float.PositiveInfinity;
    private float _flowTime;
    private bool _bound;

    public float Visibility { get; private set; } = 1f;

    // Use this when a replacement renderer is created for a combination.
    public Material OriginalMaterial => _bound ? _originalMaterial : _renderer != null
        ? _renderer.sharedMaterial : null;

    public void Bind(SpriteRenderer renderer, PotionVisualData visualData, bool transition = false)
    {
        _accentColor = visualData.AreaAccentColor;
        _surfaceStyle = visualData.AreaSurfaceStyle;
        _styleSettings = _surfaceStyle switch
        {
            PotionAreaSurfaceStyle.Embers => new Vector4(1.8f, 1f, 1f, 0.8f),
            PotionAreaSurfaceStyle.Bubbles => new Vector4(0.65f, 0.7f, 1f, 0.7f),
            PotionAreaSurfaceStyle.Frost => new Vector4(0.2f, 0.12f, 1f, 0.75f),
            PotionAreaSurfaceStyle.Life => new Vector4(0.55f, 0.25f, 0.8f, 0.65f),
            PotionAreaSurfaceStyle.Electricity => new Vector4(2.2f, 0.4f, 1f, 0.65f),
            PotionAreaSurfaceStyle.Mist => new Vector4(0.8f, 0.8f, 0.8f, 0.6f),
            PotionAreaSurfaceStyle.Cracks => new Vector4(0.3f, 0.2f, 1f, 0.8f),
            _ => Vector4.one
        };
        Bind(renderer, visualData.AreaColor, transition);
    }

    public void Bind(SpriteRenderer renderer, Color color, bool transition = false)
    {
        Color previousColor = _bound ? _currentColor : color;
        RestoreRenderer();
        _puddle = null;
        _spread = null;
        _renderer = renderer;
        _targetColor = color;
        if (!isActiveAndEnabled || renderer == null || renderer.sprite == null) return;
        if (!EnsureResources()) return;

        _originalMaterial = renderer.sharedMaterial;
        _originalSprite = renderer.sprite;
        _originalColor = renderer.color;
        _originalScale = renderer.transform.localScale;
        _originalProperties ??= new MaterialPropertyBlock();
        renderer.GetPropertyBlock(_originalProperties);

        // A neutral full quad avoids both baked-in color and tight sprite mesh clipping.
        Vector2 size = _originalSprite.bounds.size;
        renderer.sprite = _surfaceSprite;
        renderer.transform.localScale = new Vector3(
            _originalScale.x * size.x, _originalScale.y * size.y, _originalScale.z);
        renderer.sharedMaterial = _material;
        renderer.color = Color.white;

        _fromColor = transition ? previousColor : color;
        _currentColor = _fromColor;
        _startedAt = Time.time;
        _colorChangedAt = _startedAt;
        _bound = true;
        ApplyProperties();
    }

    public void SetColor(Color color)
    {
        _fromColor = _currentColor;
        _targetColor = color;
        _colorChangedAt = Time.time;
    }

    public void SetExpiration(float expiresAt) => _expiresAt = expiresAt;

    internal void SetPuddleShape(PotionPuddleShape shape)
    {
        _puddle = shape;
        if (_bound) ApplyProperties();
    }

    internal void SetCombinationSpread(PotionCombinationSpread spread)
    {
        _spread = spread;
        if (_bound) ApplyProperties();
    }

    private bool EnsureResources()
    {
        if (_material == null)
        {
            Shader shader = Resources.Load<Shader>("PotionAreaVisual");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("PotionAreaVisual shader unavailable; keeping the original sprite.", this);
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

    private void Update()
    {
        if (!_bound || _renderer == null || !_renderer.enabled) return;
        _flowTime += Time.deltaTime * _flowSpeed * _styleSettings.x;
        ApplyProperties();
    }

    private void ApplyProperties()
    {
        float age = Mathf.Max(0f, Time.time - _startedAt);
        float blend = _colorTransitionDuration > 0f
            ? Mathf.Clamp01((Time.time - _colorChangedAt) / _colorTransitionDuration) : 1f;
        _currentColor = Color.Lerp(_fromColor, _targetColor, Mathf.SmoothStep(0f, 1f, blend));
        float appear = _appearDuration > 0f ? Mathf.Clamp01(age / _appearDuration) : 1f;
        // The old lobes are already visible: reveal the combination spatially,
        // instead of shrinking/fading the entire replacement surface from its center.
        if (_spread != null) appear = 1f;
        float fade = _fadeDuration > 0f
            ? Mathf.Clamp01((_expiresAt - Time.time) / _fadeDuration) : 1f;
        Visibility = Mathf.SmoothStep(0f, 1f, appear) * fade;

        _renderer.GetPropertyBlock(_properties);
        _properties.SetColor(AreaColorId, _spread != null ? _targetColor : _currentColor);
        _properties.SetColor(AccentColorId, _accentColor);
        _properties.SetVector(SurfaceId, new Vector4(_edgeWobble * _styleSettings.y,
            _surfaceDetail * _styleSettings.z, _opacity * _styleSettings.w, (float)_surfaceStyle));
        _properties.SetVector(AnimationId, new Vector4(_flowTime,
            Mathf.SmoothStep(0f, 1f, appear), fade,
            (GetInstanceID() & 1023) * 0.137f));
        if (_puddle != null)
        {
            Vector2 offset = _puddle.BoundsCenter - _puddle.Center;
            _properties.SetVector(PuddleShapeId, new Vector4(_puddle.HalfDistance,
                _puddle.FirstRadius, _puddle.SecondRadius, _puddle.Blend));
            _properties.SetVector(PuddleFrameId, new Vector4(_puddle.Direction.x,
                _puddle.Direction.y, _puddle.NeckRadius, 1f));
            _properties.SetVector(PuddleBoundsId, new Vector4(_puddle.BoundsSize.x,
                _puddle.BoundsSize.y, offset.x, offset.y));
        }
        else
        {
            _properties.SetVector(PuddleFrameId, Vector4.zero);
        }

        if (_spread != null)
        {
            _properties.SetColor(FirstColorId, _spread.FirstColor);
            _properties.SetColor(SecondColorId, _spread.SecondColor);
            _properties.SetVector(PuddleSpreadId, new Vector4(_spread.Reach,
                Mathf.Max(0.03f, Mathf.Min(_puddle.FirstRadius, _puddle.SecondRadius) * 0.04f),
                1f, _spread.IsComplete ? 1f : 0f));
        }
        else
        {
            _properties.SetVector(PuddleSpreadId, Vector4.zero);
        }

        _renderer.SetPropertyBlock(_properties);
    }

    private void OnEnable()
    {
        if (_renderer != null && !_bound)
        {
            PotionPuddleShape puddle = _puddle;
            PotionCombinationSpread spread = _spread;
            Bind(_renderer, _targetColor);
            SetPuddleShape(puddle);
            SetCombinationSpread(spread);
        }
    }

    private void OnDisable()
    {
        RestoreRenderer();
        Visibility = 1f;
    }

    private void RestoreRenderer()
    {
        if (_bound && _renderer != null)
        {
            _renderer.sharedMaterial = _originalMaterial;
            _renderer.sprite = _originalSprite;
            _renderer.color = _originalColor;
            _renderer.transform.localScale = _originalScale;
            _renderer.SetPropertyBlock(_originalProperties);
        }
        _bound = false;
    }

    private void OnDestroy()
    {
        RestoreRenderer();
        if (_material != null) Destroy(_material);
        if (_surfaceSprite != null) Destroy(_surfaceSprite);
    }
}
