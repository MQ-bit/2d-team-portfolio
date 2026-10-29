using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PotionAreaController : MonoBehaviour
{
    private const float GroundHeightRatio = 0.5f;
    private const int EllipseSegments = 128;
    private const float VaporizationSoundFadeOutTime = 0.3f;
    [SerializeField, Min(0f)] private float _duration = 3f;
    [SerializeField, Min(0f)] private float _radius = 1.5f;

    private static int _activationCount;

    private Collider2D _collider;
    private CircleCollider2D _impactCollider;
    private PolygonCollider2D _floorCollider;
    private SpriteRenderer _renderer;
    private PotionAreaEffect _areaEffect;
    private PotionAreaCombiner _combiner;
    private PotionAreaVisual _visual;
    private float _expiresAt;
    private bool _activated;
    private GameObject _areaVfx;
    private VFXBurstField _vfxField;
    private SoundHandle _loopSound;

    // 장판이 깔린 순서. 실제 투척 순서(Ownership.ThrowOrder)와 별개다.
    internal int ActivationOrder { get; private set; }

    public PotionData Data { get; private set; }
    public PotionRunCatalog Catalog { get; private set; }
    public PotionAreaOwnership Ownership { get; } = new PotionAreaOwnership();
    public IDamageable Owner => Ownership.Owner;
    public Collider2D AreaCollider => _collider;
    public PotionAreaEffect AreaEffect => _areaEffect;
    public SpriteRenderer AreaRenderer => _renderer;
    public PotionAreaVisual AreaVisual => _visual;
    public bool IsCombined => _combiner != null && _combiner.IsCombined;
    public float RemainingDuration => Mathf.Max(0f, _expiresAt - Time.time);
    internal Vector2 OriginalCenter { get; private set; }
    internal float OriginalRadius => _radius;
    internal LayerMask WallLayers { get; private set; }
    internal PotionAreaFootprint Footprint { get; private set; }
    private bool _hasValidFootprint = true;

    public void SetOwner(IDamageable owner) => Ownership.SetOwner(owner);
    public void SetCatalog(PotionRunCatalog catalog) => Catalog = catalog;
    public void SetThrowOrder(long order) => Ownership.SetThrowOrder(order);

    internal void SetCombinedFootprint(PotionAreaFootprint footprint) => Footprint = footprint;

    private void Awake()
    {
        _impactCollider = GetComponent<CircleCollider2D>();
        // Keep this separate from the polygon reserved for combined puddles.
        _floorCollider = gameObject.AddComponent<PolygonCollider2D>();
        _floorCollider.isTrigger = true;
        _floorCollider.enabled = false;
        _collider = _impactCollider;
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _areaEffect = GetComponent<PotionAreaEffect>();
        _combiner = GetComponent<PotionAreaCombiner>();
        _visual = GetComponent<PotionAreaVisual>();
        if (_visual == null) _visual = gameObject.AddComponent<PotionAreaVisual>();
        _collider.isTrigger = true;
        _impactCollider.radius = _radius;
    }

    public void Initialize(PotionData data) => Initialize(data, default);

    public void Initialize(PotionData data, LayerMask wallLayers)
    {
        if (data == null)
        {
            Debug.LogError("PotionGroundArea requires PotionData.", this);
            return;
        }

        Data = data;
        OriginalCenter = transform.position;
        WallLayers = wallLayers;
        _duration = data.FloorDuration;
        _radius = data.EffectRadius;
        // Vaporization can wait for a later potion; use the same visible footprint
        // for overlap detection regardless of which potion lands first.
        ConfigureAreaCollider(data.CreatesFloor || data.Element == PotionElement.Vaporization);
        if (!_hasValidFootprint) return;

        if (_renderer != null)
        {
            if (data.VisualData.AreaSprite != null)
            {
                _renderer.sprite = data.VisualData.AreaSprite;
            }
            _renderer.color = data.VisualData.AreaColor;
            if (_renderer.sprite != null)
            {
                Vector2 spriteSize = _renderer.sprite.bounds.size;
                Vector3 parentScale = _renderer.transform.parent != null
                    ? _renderer.transform.parent.lossyScale : Vector3.one;
                float diameter = _radius * 2f;
                _renderer.transform.localScale = new Vector3(
                    diameter / Mathf.Max(0.001f, spriteSize.x * Mathf.Abs(parentScale.x)),
                    diameter * GroundHeightRatio / Mathf.Max(0.001f, spriteSize.y * Mathf.Abs(parentScale.y)), 1f);
            }
        }

        if (_areaEffect != null)
        {
            using (OptimizationMarkers.LandEffectInit.Auto()) _areaEffect.Initialize(data);
        }

        using (OptimizationMarkers.LandVisualBind.Auto())
        {
            _visual.Bind(_renderer, data.VisualData);
            _visual.SetFootprint(Footprint);
        }

        if (data.CreatesFloor && data.VisualData.AreaVfxPrefab != null)
        {
            // Match the aim preview's 2:1 ground ellipse, including VFX placement.
            using (OptimizationMarkers.LandAreaVfx.Auto())
            {
                SetAreaVfx(data.VisualData, data.AreaWidth, data.AreaWidth * GroundHeightRatio);
            }
        }
    }

    private void ConfigureAreaCollider(bool createsFloor)
    {
        Footprint = null;
        _hasValidFootprint = true;
        _impactCollider.radius = _radius;
        _impactCollider.enabled = !createsFloor;
        _floorCollider.enabled = false;
        _collider = createsFloor ? _floorCollider : _impactCollider;
        if (!createsFloor) return;

        // Build in world space, like the combined puddle, to match the visual
        // even when the prefab's parent has a non-unit scale.
        var points = new Vector2[EllipseSegments];
        Vector3 center = transform.position;
        for (int i = 0; i < points.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / points.Length;
            Vector3 worldPoint = center + new Vector3(
                Mathf.Cos(angle) * _radius,
                Mathf.Sin(angle) * _radius * GroundHeightRatio, 0f);
            points[i] = worldPoint;
        }

        using (OptimizationMarkers.LandClip.Auto())
        {
            Footprint = PotionAreaClipper.Clip(points, OriginalCenter, WallLayers);
        }

        _hasValidFootprint = Footprint != null;
        if (_hasValidFootprint)
        {
            using (OptimizationMarkers.LandCollider.Auto()) Footprint.ApplyTo(_floorCollider);
        }
    }

    public GameObject SetAreaVfx(PotionVisualData visualData, float width, float height)
    {
        ClearAreaVfx();
        if (visualData.AreaVfxPrefab == null) return null;

        _areaVfx = new GameObject("PotionVfxBurstField");
        _areaVfx.transform.SetParent(transform, false);
        _areaVfx.transform.localPosition = Vector3.zero;
        _areaVfx.transform.localRotation = Quaternion.identity;
        Vector3 parentScale = transform.lossyScale;
        _areaVfx.transform.localScale = new Vector3(
            1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
        VFXBurstField field = _areaVfx.AddComponent<VFXBurstField>();
        field.ConfigureArea(width, height, _renderer);
        if (!IsCombined && Data != null && Data.CreatesFloor && Data.Element != PotionElement.Vaporization)
        {
            field.UseLegacySinglePotionBursts();
        }
        else if (IsCombined && visualData.AreaSurfaceStyle != PotionAreaSurfaceStyle.ThermalShock
                 && Data != null && Data.Element != PotionElement.Vaporization &&
                 _combiner.PartnerArea != null && _combiner.PartnerArea.Data != null &&
                 _combiner.PartnerArea.Data.Element != PotionElement.Vaporization)
        {
            field.UseIntenseCombinationBursts();
        }

        if (visualData.AreaSurfaceStyle == PotionAreaSurfaceStyle.ThermalShock)
            field.UseDeterministicBursts(_visual.CombinationSeed);
        field.SetVfx(visualData.AreaVfxPrefab);
        field.ConfigureFootprint(Footprint);
        _vfxField = field;
        return _areaVfx;
    }

    public void ClearAreaVfx()
    {
        if (_areaVfx == null) return;
        _areaVfx.SetActive(false);
        Destroy(_areaVfx);
        _areaVfx = null;
        _vfxField = null;
    }

    internal void RetainAreaVfxForSpread(PotionCombinationSpread spread)
    {
        if (_vfxField == null) return;
        _vfxField.ConfigureSpread(spread, false);
        // The old field hides bursts as the reaction reaches them and releases itself.
        _areaVfx = null;
        _vfxField = null;
    }

    public void Activate()
    {
        if (Data == null || _activated) return;
        if (!_hasValidFootprint)
        {
            Remove();
            return;
        }
        _activated = true;
        if (Ownership.ThrowOrder == 0) Ownership.SetThrowOrder(PotionAreaOwnership.NextThrowOrder());
        ActivationOrder = ++_activationCount;
        Ownership.MarkReaction();
        SetExpiration(Time.time + _duration);
        // 조합에서 먼저 던져진 쪽의 소리를 끌 수 있도록 조합 탐색보다 먼저 재생한다.
        // 기화는 장판을 만들지 않지만 반응을 기다리는 동안 소리를 낸다.
        if (Data.CreatesFloor || Data.Element == PotionElement.Vaporization) PlayLoopSound();
        if (_combiner != null)
        {
            using (OptimizationMarkers.LandRegister.Auto()) _combiner.RegisterArea();
        }

        if (!IsCombined && _areaEffect != null)
        {
            using (OptimizationMarkers.LandSingleImpact.Auto())
            {
                _areaEffect.ActivateSingleProfile(Owner, _expiresAt);
                _areaEffect.ApplySingleImpact(_collider);
            }
        }

        // Instant effects may leave a trace; uncombined vaporization can still react.
        if (!IsCombined && !Data.CreatesFloor)
        {
            FinishInstantImpact();
        }
    }

    // 장판이 있는 동안 반복 재생하며, 증기 대기음은 종료 시 짧게 페이드아웃한다.
    private void PlayLoopSound()
    {
        _loopSound.Stop();
        _loopSound = SoundManager.Play(GameSoundData.GetAreaSoundId(Data.Element));
    }

    // 각 클립의 원래 재생 음량에 대한 비율로 장판음을 조절한다.
    internal void FadeLoopSoundTo(float volumeRatio, float fadeTime)
    {
        _loopSound.FadeTo(volumeRatio, fadeTime);
    }

    internal void FadeOutLoopSound(float fadeTime, float volumeScale)
    {
        _loopSound.FadeOut(fadeTime, volumeScale);
        // 장판 제거 시 OnDisable이 잔향을 즉시 정지하지 않도록 소유권을 놓는다.
        _loopSound = default;
    }

    internal void StopLoopSound()
    {
        if (Data != null && Data.Element == PotionElement.Vaporization)
        {
            // 장판이 사라져도 매니저가 남은 페이드를 처리한다.
            _loopSound.FadeOut(VaporizationSoundFadeOutTime);
        }
        else
        {
            _loopSound.Stop();
        }

        // 조합 처리와 OnDisable에서 중복 호출해도 페이드를 다시 시작하지 않는다.
        _loopSound = default;
    }

    private void OnDisable()
    {
        StopLoopSound();
    }

    internal void FinishInstantImpact()
    {
        // 즉발 효과는 흔적만 남고 장판은 없으므로 소리를 끈다. 반응을 기다리는 기화만 계속 소리를 낸다.
        if (IsCombined || Data == null || Data.Element != PotionElement.Vaporization) StopLoopSound();
        if (IsCombined && Data != null && Data.Element == PotionElement.Vaporization)
        {
            Remove();
            return;
        }

        float visualDuration = Data != null && Data.Element == PotionElement.Vaporization
            ? Data.VaporizationSettings != null ? Data.VaporizationSettings.WaitingDuration : 2f
            : Data != null ? Data.VisualData.ImpactVisualDuration : 0f;
        if (visualDuration <= 0f)
        {
            Remove();
            return;
        }

        bool canAwaitCombination = !IsCombined && Data.Element == PotionElement.Vaporization;
        foreach (Collider2D areaCollider in GetComponents<Collider2D>())
            areaCollider.enabled = canAwaitCombination && areaCollider == _collider;
        if (_areaEffect != null) _areaEffect.enabled = false;
        if (_combiner != null) _combiner.enabled = canAwaitCombination;
        ClearAreaVfx();
        SetExpiration(Time.time + visualDuration);
    }

    public void SetExpiration(float expiresAt)
    {
        _expiresAt = expiresAt;
        _visual.SetExpiration(expiresAt);
    }

    private void Update()
    {
        if (_vfxField != null) _vfxField.SetVisibility(_visual.Visibility);
        if (_activated && Time.time >= _expiresAt) Remove();
    }

    public void Remove()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
