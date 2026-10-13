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

    [Header("Visual Boundary")]
    [SerializeField, Range(0f, 0.2f)] private float _edgeIrregularity = 0.17f;
    [SerializeField, Range(2f, 12f)] private float _edgeDensity = 4.5f;
    [SerializeField, Range(0f, 0.5f)] private float _edgeMotionSpeed = 0.35f;
    [SerializeField, Range(0f, 0.5f)] private float _edgeMotionAmount = 0.45f;

    [Header("Transitions")]
    [SerializeField, Min(0f)] private float _appearDuration = 0.25f;
    [SerializeField, Min(0f)] private float _colorTransitionDuration = 0.35f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.4f;

    private readonly PotionAreaSurfaceRenderer _surfaceRenderer = new();
    private readonly PotionHealingSpringRenderer _healingSpring = new();
    private readonly PotionFireVortexRenderer _fireVortex = new();
    private readonly PotionFrostCrystalRenderer _frostCrystals = new();
    private SpriteRenderer _renderer;
    private PotionPuddleShape _puddle;
    private PotionAreaFootprint _footprint;
    private PotionCombinationSpread _spread;
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
    private uint _combinationSeed;
    private bool _hasCombinationSeed;
    private bool IsVampiric => _surfaceStyle == PotionAreaSurfaceStyle.VampiricFire
        || _surfaceStyle == PotionAreaSurfaceStyle.VampiricPoison
        || _surfaceStyle == PotionAreaSurfaceStyle.VampiricFrost
        || _surfaceStyle == PotionAreaSurfaceStyle.VampiricLightning;
    internal uint CombinationSeed => _combinationSeed;

    internal void SetCombinationSeed(uint seed)
    {
        _combinationSeed = seed;
        _hasCombinationSeed = true;
        _fireVortex.SetSeed(seed);
        _frostCrystals.SetSeed(seed);
    }

    public float Visibility { get; private set; } = 1f;

    // Use this when a replacement renderer is created for a combination.
    public Material OriginalMaterial => _surfaceRenderer.IsBound
        ? _surfaceRenderer.OriginalMaterial
        : _renderer != null ? _renderer.sharedMaterial : null;

    public void Bind(SpriteRenderer renderer, PotionVisualData visualData, bool transition = false)
    {
        _accentColor = visualData.AreaAccentColor;
        _surfaceStyle = visualData.AreaSurfaceStyle;
        if (_surfaceStyle == PotionAreaSurfaceStyle.ThermalShock) _flowTime = 0f;
        _styleSettings = _surfaceStyle switch
        {
            PotionAreaSurfaceStyle.Embers => new Vector4(1.8f, 1f, 1f, 0.8f),
            PotionAreaSurfaceStyle.Bubbles => new Vector4(0.65f, 0.7f, 1f, 0.7f),
            PotionAreaSurfaceStyle.Frost => new Vector4(0.65f, 0.12f, 1f, 0.85f),
            PotionAreaSurfaceStyle.Life => new Vector4(0.55f, 0.25f, 0.8f, 0.65f),
            PotionAreaSurfaceStyle.HealingSpring => new Vector4(0.8f, 0.15f, 1f, 0.8f),
            PotionAreaSurfaceStyle.FireVortex => new Vector4(1.1f, 0.45f, 0.75f, 0.8f),
            PotionAreaSurfaceStyle.ThermalShock => new Vector4(1f, 0.35f, 1f, 0.85f),
            PotionAreaSurfaceStyle.VampiricFire => new Vector4(1f, 0.3f, 1f, 0.8f),
            PotionAreaSurfaceStyle.VampiricPoison => new Vector4(0.8f, 0.5f, 1f, 0.85f),
            PotionAreaSurfaceStyle.VampiricFrost => new Vector4(0.7f, 0.12f, 1f, 0.85f),
            PotionAreaSurfaceStyle.VampiricLightning => new Vector4(1.6f, 0.25f, 1f, 0.8f),
            PotionAreaSurfaceStyle.FrostPoison => new Vector4(0.6f, 0.18f, 1f, 0.9f),
            PotionAreaSurfaceStyle.ToxicCurrent => new Vector4(1.2f, 0.6f, 1f, 0.85f),
            PotionAreaSurfaceStyle.BlazingCurrent => new Vector4(1.2f, 0.25f, 1f, 0.85f),
            PotionAreaSurfaceStyle.Electricity => new Vector4(2.2f, 0.4f, 1f, 0.65f),
            PotionAreaSurfaceStyle.Mist => new Vector4(0.8f, 0.8f, 0.8f, 0.6f),
            PotionAreaSurfaceStyle.Cracks => new Vector4(0.3f, 0.2f, 1f, 0.8f),
            _ => Vector4.one
        };
        Bind(renderer, visualData.AreaColor, transition);
    }

    public void Bind(SpriteRenderer renderer, Color color, bool transition = false)
    {
        Color previousColor = _surfaceRenderer.IsBound ? _currentColor : color;
        _surfaceRenderer.Restore();
        _healingSpring.Hide();
        _fireVortex.Hide();
        _frostCrystals.Hide();
        _frostCrystals.SetFootprint(null);
        _fireVortex.SetFootprint(null);
        _puddle = null;
        _footprint = null;
        _surfaceRenderer.SetFootprint(null);
        _spread = null;
        _renderer = renderer;
        _targetColor = color;
        if (!isActiveAndEnabled || renderer == null || renderer.sprite == null) return;
        if (!_surfaceRenderer.Bind(renderer, this)) return;

        _fromColor = transition ? previousColor : color;
        _currentColor = _fromColor;
        _startedAt = Time.time;
        _colorChangedAt = _startedAt;
        if (_surfaceStyle == PotionAreaSurfaceStyle.HealingSpring || IsVampiric)
            _healingSpring.Bind(renderer, this);
        if (_surfaceStyle == PotionAreaSurfaceStyle.FireVortex
            || _surfaceStyle == PotionAreaSurfaceStyle.BlazingCurrent
            || _surfaceStyle == PotionAreaSurfaceStyle.VampiricFire)
        {
            _fireVortex.Bind(renderer, this);
        }

        if (_surfaceStyle == PotionAreaSurfaceStyle.Frost || _surfaceStyle == PotionAreaSurfaceStyle.ThermalShock
            || _surfaceStyle == PotionAreaSurfaceStyle.VampiricFrost
            || _surfaceStyle == PotionAreaSurfaceStyle.FrostPoison)
        {
            _frostCrystals.Bind(renderer, this);
        }

        ApplyProperties();
    }

    public void SetColor(Color color)
    {
        _fromColor = _currentColor;
        _targetColor = color;
        _colorChangedAt = Time.time;
    }

    public void SetExpiration(float expiresAt) => _expiresAt = expiresAt;

    internal void SetFootprint(PotionAreaFootprint footprint)
    {
        _footprint = footprint;
        _surfaceRenderer.SetFootprint(footprint);
        if (_surfaceStyle == PotionAreaSurfaceStyle.FireVortex
            || _surfaceStyle == PotionAreaSurfaceStyle.BlazingCurrent
            || _surfaceStyle == PotionAreaSurfaceStyle.VampiricFire)
        {
            _fireVortex.SetFootprint(footprint);
        }

        if (_surfaceStyle == PotionAreaSurfaceStyle.Frost || _surfaceStyle == PotionAreaSurfaceStyle.ThermalShock
            || _surfaceStyle == PotionAreaSurfaceStyle.VampiricFrost
            || _surfaceStyle == PotionAreaSurfaceStyle.FrostPoison)
        {
            _frostCrystals.SetFootprint(footprint);
        }

        if (_surfaceRenderer.IsBound) ApplyProperties();
    }

    internal void SetPuddleShape(PotionPuddleShape shape)
    {
        _puddle = shape;
        if (_surfaceRenderer.IsBound) ApplyProperties();
    }

    internal void SetCombinationSpread(PotionCombinationSpread spread)
    {
        _spread = spread;
        if (_surfaceRenderer.IsBound) ApplyProperties();
    }

    private void Update()
    {
        if (!_surfaceRenderer.IsBound || _renderer == null || !_renderer.enabled)
        {
            _healingSpring.Hide();
            _fireVortex.Hide();
            _frostCrystals.Hide();
            return;
        }
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
        _surfaceRenderer.SetBoundarySettings(new Vector4(_edgeIrregularity, _edgeDensity,
            _edgeMotionSpeed, _edgeMotionAmount));
        _surfaceRenderer.Apply(
            _spread != null ? _targetColor : _currentColor, _accentColor,
            new Vector4(_edgeWobble * _styleSettings.y, _surfaceDetail * _styleSettings.z,
                _opacity * _styleSettings.w, (float)_surfaceStyle),
            new Vector4(_flowTime, Mathf.SmoothStep(0f, 1f, appear), fade,
                _hasCombinationSeed ? (_combinationSeed & 1023u) * 0.137f : (GetInstanceID() & 1023) * 0.137f),
            _puddle, _spread);

        // Flames read this frame's ground appearance and color to stay attached to it.
        if (_surfaceStyle == PotionAreaSurfaceStyle.HealingSpring)
            _healingSpring.Apply(_renderer, _currentColor, _accentColor, _flowTime, Visibility * _opacity);
        if (_surfaceStyle == PotionAreaSurfaceStyle.FireVortex)
            _fireVortex.Apply(_renderer, _accentColor, _flowTime, Visibility * _opacity);
        if (_surfaceStyle == PotionAreaSurfaceStyle.BlazingCurrent)
            _fireVortex.Apply(_renderer, _accentColor, _flowTime, Visibility * _opacity * 0.85f);
        if (_surfaceStyle == PotionAreaSurfaceStyle.Frost)
            _frostCrystals.Apply(_renderer, _accentColor, Visibility * _opacity);
        if (_surfaceStyle == PotionAreaSurfaceStyle.FrostPoison)
            _frostCrystals.Apply(_renderer, _accentColor, Visibility * _opacity * 0.85f);
        if (_surfaceStyle == PotionAreaSurfaceStyle.ThermalShock)
        {
            _frostCrystals.Apply(_renderer, new Color(0.9f, 0.96f, 1f), Visibility * _opacity);
        }
        if (_surfaceStyle == PotionAreaSurfaceStyle.VampiricFire)
        {
            _fireVortex.Apply(_renderer, new Color(1f, 0.34f, 0.06f), _flowTime, Visibility * _opacity * 0.8f);
        }
        if (_surfaceStyle == PotionAreaSurfaceStyle.VampiricFrost)
            _frostCrystals.Apply(_renderer, _accentColor, Visibility * _opacity * 0.85f);
        if (IsVampiric)
        {
            Color pillarColor = _surfaceStyle switch
            {
                PotionAreaSurfaceStyle.VampiricFire => new Color(1f, 0.3f, 0.055f),
                PotionAreaSurfaceStyle.VampiricPoison => new Color(0.48f, 0.78f, 0.08f),
                PotionAreaSurfaceStyle.VampiricFrost => new Color(0.3f, 0.72f, 0.95f),
                _ => new Color(0.62f, 0.3f, 0.92f)
            };
            Color pillarAccent = Color.Lerp(pillarColor, Color.white, 0.5f);
            _healingSpring.Apply(_renderer, pillarColor, pillarAccent,
                _flowTime, Visibility * _opacity * 0.5f);
        }
    }

    private void OnEnable()
    {
        if (_renderer != null && !_surfaceRenderer.IsBound)
        {
            PotionPuddleShape puddle = _puddle;
            PotionAreaFootprint footprint = _footprint;
            PotionCombinationSpread spread = _spread;
            Bind(_renderer, _targetColor);
            SetPuddleShape(puddle);
            SetFootprint(footprint);
            SetCombinationSpread(spread);
        }
    }

    private void OnDisable()
    {
        _healingSpring.Hide();
        _fireVortex.Hide();
        _frostCrystals.Hide();
        _surfaceRenderer.Restore();
        Visibility = 1f;
    }

    private void OnDestroy()
    {
        _healingSpring.Release();
        _fireVortex.Release();
        _frostCrystals.Release();
        _surfaceRenderer.Release();
    }
}
