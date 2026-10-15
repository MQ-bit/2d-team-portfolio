using System.Collections.Generic;
using UnityEngine;

// 상태이상만 관찰한다. 별도 렌더러에 포션 셰이더를 적용해 대상 주변에 아우라를 표시한다.
[DisallowMultipleComponent]
public sealed class PotionStatusVisual : MonoBehaviour
{
    private sealed class Aura
    {
        public SpriteRenderer Renderer;
        public readonly MaterialPropertyBlock Properties = new();
        public float Visibility;
        public float Appearance;
        public float FlowTime;
        public float Seed;
    }

    private static readonly int AreaColorId = Shader.PropertyToID("_AreaColor");
    private static readonly int AccentColorId = Shader.PropertyToID("_AccentColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int AnimationId = Shader.PropertyToID("_Animation");
    private static readonly int AuraId = Shader.PropertyToID("_Aura");
    private static readonly int AuraElementsId = Shader.PropertyToID("_AuraElements");

    [Header("References")]
    [SerializeField] private PotionEffectReceiver _receiver;
    [Tooltip("비워두면 Resources/PotionStatusVisuals 설정을 사용합니다.")]
    [SerializeField] private PotionStatusVisualData _visualData;
    [SerializeField] private SpriteRenderer _targetRenderer;
    [Tooltip("비워두면 대상 중심에 StatusEffectAnchor를 생성합니다.")]
    [SerializeField] private Transform _anchor;

    [Header("Layout")]
    [SerializeField] private Vector3 _offset;
    [SerializeField, Min(0.1f)] private float _fallbackSize = 1f;
    [SerializeField, Range(0.5f, 1.5f)] private float _auraSizeScale = 0.65f;
    [SerializeField, Range(1f, 2f)] private float _auraHeightScale = 1.1f;
    [SerializeField, Min(1)] private int _sortingOrderOffset = 1;
    [Tooltip("무기/공격 이펙트가 포함된 스프라이트는 현재 프레임 대신 고정된 몸 영역을 사용한다.")]
    [SerializeField] private bool _useFixedBodyBounds;
    [SerializeField] private Vector3 _bodyBoundsCenter;
    [SerializeField] private Vector2 _bodyBoundsSize = Vector2.one;

    [Header("Color")]
    [SerializeField, Range(1f, 1.5f)] private float _colorSaturation = 1.3f;
    [SerializeField, Range(1f, 1.5f)] private float _colorStrength = 1.5f;

    [Header("Transitions")]
    [SerializeField, Min(0f)] private float _appearDuration = 0.65f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.45f;

    private readonly Dictionary<PotionStatusEffectTracker, Aura> _auras = new();
    private readonly Dictionary<PotionCombinationData, Aura> _combinationAuras = new();
    private readonly List<PotionCombinationData> _activeCombinations = new();
    private readonly HashSet<PotionStatusEffectTracker> _replacedStatuses = new();
    private Material _material;
    private Sprite _quadSprite;
    private bool _ownsAnchor;
    private bool _shaderUnavailable;

    private void Awake()
    {
        if (_receiver == null) _receiver = GetComponent<PotionEffectReceiver>();
        if (_visualData == null) _visualData = Resources.Load<PotionStatusVisualData>("PotionStatusVisuals");
        if (_targetRenderer == null)
        {
            Animator animator = GetComponentInChildren<Animator>();
            _targetRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : null;
            if (_targetRenderer == null) _targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (_anchor == null)
        {
            _anchor = new GameObject("StatusEffectAnchor").transform;
            _anchor.SetParent(transform, false);
            _ownsAnchor = true;
        }
    }

    private void LateUpdate()
    {
        if (_receiver == null || !_receiver.isActiveAndEnabled || _visualData == null
            || (_receiver.Damageable != null && !PotionStatApplier.IsAlive(_receiver.Damageable)))
        {
            HideAuras();
            return;
        }

        if (_ownsAnchor)
        {
            Vector3 center = GetTargetBounds().center;
            _anchor.position = center + transform.TransformVector(_offset);
        }

        UpdateCombinationAuras();
        foreach (PotionStatusVisualData.Entry entry in _visualData.Effects)
        {
            if (entry == null) continue;
            bool active = !_replacedStatuses.Contains(entry.Status) && IsActive(entry);
            if (!_auras.TryGetValue(entry.Status, out Aura aura))
            {
                if (!active || !EnsureResources()) continue;
                aura = CreateAura(entry.Status);
                _auras.Add(entry.Status, aura);
            }

            if (!AdvanceAura(aura, active, entry.FlowSpeed)) continue;
            ApplyProperties(aura, entry);
        }
    }

    private void UpdateCombinationAuras()
    {
        _receiver.GetVisualCombinations(_activeCombinations);
        _replacedStatuses.Clear();
        foreach (PotionCombinationData combination in _activeCombinations)
        {
            if (!EnsureResources()) continue;
            if (!_combinationAuras.ContainsKey(combination))
            {
                _combinationAuras.Add(combination, CreateAuraRenderer($"PotionAura_{combination.Combination}",
                    (int)combination.Combination + 10));
            }

            ReplaceElementStatus(combination.ElementA);
            ReplaceElementStatus(combination.ElementB);
        }

        foreach (var pair in _combinationAuras)
        {
            Aura aura = pair.Value;
            if (!AdvanceAura(aura, _activeCombinations.Contains(pair.Key), _visualData.CombinationFlowSpeed))
                continue;
            PotionVisualData visual = pair.Key.VisualData;
            Vector4 elements = ElementChannels(pair.Key.ElementA) + ElementChannels(pair.Key.ElementB);
            ApplyAuraProperties(aura, visual.AreaSurfaceStyle, visual.AreaColor, visual.AreaAccentColor,
                _visualData.CombinationOpacity, _visualData.CombinationSizeMultiplier,
                _visualData.CombinationCoreSpread, 0.14f, Vector3.zero, elements);
        }
    }

    private void ReplaceElementStatus(PotionElement element)
    {
        switch (element)
        {
            case PotionElement.Fire: _replacedStatuses.Add(PotionStatusEffectTracker.Burning); break;
            case PotionElement.Poison: _replacedStatuses.Add(PotionStatusEffectTracker.Poisoned); break;
            case PotionElement.Ice:
                _replacedStatuses.Add(PotionStatusEffectTracker.Frozen);
                _replacedStatuses.Add(PotionStatusEffectTracker.Slowed);
                break;
            case PotionElement.Lightning: _replacedStatuses.Add(PotionStatusEffectTracker.Paralyzed); break;
        }
    }

    private static Vector4 ElementChannels(PotionElement element) => element switch
    {
        PotionElement.Fire => new Vector4(1f, 0f, 0f, 0f),
        PotionElement.Poison => new Vector4(0f, 1f, 0f, 0f),
        PotionElement.Ice => new Vector4(0f, 0f, 1f, 0f),
        PotionElement.Lightning => new Vector4(0f, 0f, 0f, 1f),
        _ => Vector4.zero
    };

    private bool AdvanceAura(Aura aura, bool active, float flowSpeed)
    {
        float duration = active ? _appearDuration : _fadeDuration;
        if (active)
        {
            aura.Appearance = _appearDuration > 0f
                ? Mathf.MoveTowards(aura.Appearance, 1f, Time.deltaTime / _appearDuration) : 1f;
        }

        aura.Visibility = duration > 0f
            ? Mathf.MoveTowards(aura.Visibility, active ? 1f : 0f, Time.deltaTime / duration)
            : active ? 1f : 0f;
        aura.Renderer.enabled = aura.Visibility > 0f;
        if (!aura.Renderer.enabled)
        {
            aura.Appearance = 0f;
            aura.FlowTime = 0f;
            return false;
        }
        aura.FlowTime += Time.deltaTime * flowSpeed;
        return true;
    }

    private bool IsActive(PotionStatusVisualData.Entry entry)
    {
        if (entry.Status == PotionStatusEffectTracker.Slowed)
        {
            // 빙결/마비의 이동 불가를 둔화로 중복 표시하지 않는다.
            return _receiver.CanMove && _receiver.MoveSpeedMultiplier < 0.999f;
        }
        return _receiver.GetStatusStackCount(entry.Status) > 0
            || (entry.ShowInArea && _receiver.IsInStatusArea(entry.Status));
    }

    private bool EnsureResources()
    {
        if (_shaderUnavailable) return false;
        if (_material == null)
        {
            Shader shader = Resources.Load<Shader>("PotionAreaVisual");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("포션 아우라 셰이더를 사용할 수 없습니다.", this);
                _shaderUnavailable = true;
                return false;
            }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        if (_quadSprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            _quadSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.width, 0, SpriteMeshType.FullRect);
            _quadSprite.hideFlags = HideFlags.HideAndDontSave;
        }
        return true;
    }

    private Aura CreateAura(PotionStatusEffectTracker status)
    {
        return CreateAuraRenderer($"PotionAura_{status}", (int)status);
    }

    private Aura CreateAuraRenderer(string name, int seed)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_anchor, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = _quadSprite;
        renderer.sharedMaterial = _material;
        renderer.enabled = false;
        return new Aura
        {
            Renderer = renderer,
            Seed = ((GetInstanceID() & 1023) + seed * 137) * 0.137f
        };
    }

    private void ApplyProperties(Aura aura, PotionStatusVisualData.Entry entry)
    {
        ApplyAuraProperties(aura, entry.SurfaceStyle, entry.Color, entry.AccentColor,
            entry.Opacity, entry.SizeMultiplier, entry.InnerRadius, entry.EdgeSoftness, entry.Offset);
    }

    private void ApplyAuraProperties(Aura aura, PotionAreaSurfaceStyle style, Color color, Color accent,
        float opacity, float sizeMultiplier, float coreSpread, float softness, Vector3 offset,
        Vector4 elements = default)
    {
        Vector2 size = GetTargetBounds().size;
        Vector3 parentScale = _anchor.lossyScale;
        Transform effect = aura.Renderer.transform;
        effect.localPosition = offset;
        effect.rotation = Quaternion.identity;
        float auraSize = sizeMultiplier * _auraSizeScale;
        float width = Mathf.Max(0.1f, size.x) * auraSize;
        // 단일·조합 모두 대상의 실제 높이보다 조금 크게 표시한다.
        float height = Mathf.Max(0.1f, size.y) * _auraHeightScale;
        effect.localScale = new Vector3(
            width / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            height / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
        if (_targetRenderer != null)
        {
            aura.Renderer.sortingLayerID = _targetRenderer.sortingLayerID;
            aura.Renderer.sortingOrder = _targetRenderer.sortingOrder + Mathf.Max(1, _sortingOrderOffset);
        }

        MaterialPropertyBlock properties = aura.Properties;
        properties.SetColor(AreaColorId, StrengthenColor(color));
        properties.SetColor(AccentColorId, StrengthenColor(accent));
        properties.SetVector(SurfaceId, new Vector4(0f, 0.65f,
            Mathf.Clamp01(opacity * _colorStrength), (float)style));
        properties.SetVector(AnimationId, new Vector4(aura.FlowTime, aura.Visibility, 1f, aura.Seed));
        properties.SetVector(AuraId, new Vector4(1f, coreSpread, softness, aura.Appearance));
        properties.SetVector(AuraElementsId, elements);
        aura.Renderer.SetPropertyBlock(properties);
    }

    public void ConfigureBodyBounds(Vector3 localCenter, Vector2 localSize)
    {
        _useFixedBodyBounds = true;
        _bodyBoundsCenter = localCenter;
        _bodyBoundsSize = new Vector2(Mathf.Max(0.1f, localSize.x), Mathf.Max(0.1f, localSize.y));
    }

    private Bounds GetTargetBounds()
    {
        if (!_useFixedBodyBounds)
        {
            return _targetRenderer != null ? _targetRenderer.bounds
                : new Bounds(transform.position, Vector3.one * _fallbackSize);
        }

        // 루트의 위치/크기는 따라가되, 공격 스프라이트의 교체/좌우 반전에는 흔들리지 않는다.
        Vector3 horizontal = transform.TransformVector(new Vector3(_bodyBoundsSize.x, 0f, 0f));
        Vector3 vertical = transform.TransformVector(new Vector3(0f, _bodyBoundsSize.y, 0f));
        Vector3 worldSize = new Vector3(Mathf.Abs(horizontal.x) + Mathf.Abs(vertical.x),
            Mathf.Abs(horizontal.y) + Mathf.Abs(vertical.y), Mathf.Abs(horizontal.z) + Mathf.Abs(vertical.z));
        return new Bounds(transform.TransformPoint(_bodyBoundsCenter), worldSize);
    }

    private Color StrengthenColor(Color color)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);
        Color result = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * _colorSaturation), value, true);
        result.a = color.a;
        return result;
    }

    private void HideAuras()
    {
        HideAuraGroup(_auras.Values);
        HideAuraGroup(_combinationAuras.Values);
    }

    private static void HideAuraGroup(IEnumerable<Aura> auras)
    {
        foreach (Aura aura in auras)
        {
            if (aura.Renderer == null) continue;
            aura.Renderer.enabled = false;
            aura.Visibility = 0f;
            aura.Appearance = 0f;
            aura.FlowTime = 0f;
        }
    }

    private void OnDisable() => HideAuras();

    private void OnDestroy()
    {
        DestroyAuraGroup(_auras.Values);
        DestroyAuraGroup(_combinationAuras.Values);
        _auras.Clear();
        _combinationAuras.Clear();
        if (_material != null) Destroy(_material);
        if (_quadSprite != null) Destroy(_quadSprite);
        if (_ownsAnchor && _anchor != null) Destroy(_anchor.gameObject);
    }

    private void DestroyAuraGroup(IEnumerable<Aura> auras)
    {
        foreach (Aura aura in auras)
        {
            if (aura.Renderer == null) continue;
            aura.Renderer.enabled = false;
            Destroy(aura.Renderer.gameObject);
        }
    }
}
