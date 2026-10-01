using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PotionGroundArea : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _duration = 3f;
    [SerializeField, Min(0f)] private float _radius = 1.5f;

    [Header("Area VFX Bursts")]
    [SerializeField, Min(1)] private int _vfxBurstCount = 12;
    [SerializeField, Min(0.05f)] private float _vfxBurstSize = 0.4f;
    [SerializeField, Range(0.1f, 1f)] private float _vfxSpawnAreaRatio = 0.85f;

    private CircleCollider2D _collider;
    private SpriteRenderer _renderer;
    private PotionAreaEffect _areaEffect;
    private PotionAreaCombiner _combiner;
    private PotionAreaVisual _visual;
    private float _expiresAt;
    private bool _activated;
    private GameObject _areaVfx;
    private VFXBurstField _vfxField;

    public PotionData Data { get; private set; }
    public IDamageable Owner { get; private set; }
    public CircleCollider2D AreaCollider => _collider;
    public PotionAreaEffect AreaEffect => _areaEffect;
    public SpriteRenderer AreaRenderer => _renderer;
    public PotionAreaVisual AreaVisual => _visual;
    public bool IsCombined => _combiner != null && _combiner.IsCombined;

    public void SetOwner(IDamageable owner) => Owner = owner;

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _renderer = GetComponentInChildren<SpriteRenderer>();
        _areaEffect = GetComponent<PotionAreaEffect>();
        _combiner = GetComponent<PotionAreaCombiner>();
        _visual = GetComponent<PotionAreaVisual>();
        if (_visual == null) _visual = gameObject.AddComponent<PotionAreaVisual>();
        _collider.isTrigger = true;
        _collider.radius = _radius;
    }

    public void Initialize(PotionData data)
    {
        if (data == null)
        {
            Debug.LogError("PotionGroundArea requires PotionData.", this);
            return;
        }

        Data = data;
        _duration = data.FloorDuration;
        _radius = data.EffectRadius;
        _collider.radius = _radius;

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
                    diameter * 0.5f / Mathf.Max(0.001f, spriteSize.y * Mathf.Abs(parentScale.y)), 1f);
            }
        }

        if (_areaEffect != null)
        {
            _areaEffect.Initialize(data);
        }

        _visual.Bind(_renderer, data.VisualData);

        if (data.CreatesFloor && data.VisualData.AreaVfxPrefab != null)
        {
            // Match the aim preview's 2:1 ground ellipse, including VFX placement.
            SetAreaVfx(data.VisualData, data.AreaWidth, data.AreaWidth * 0.5f);
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
        field.ConfigureArea(width, height, _renderer,
            _vfxBurstCount, _vfxBurstSize, _vfxSpawnAreaRatio);
        field.SetVfx(visualData.AreaVfxPrefab);
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
        _activated = true;
        SetExpiration(Time.time + _duration);
        if (_combiner != null) _combiner.RegisterArea();

        if (!IsCombined && _areaEffect != null)
        {
            _areaEffect.ActivateSingleProfile(Owner, _expiresAt);
            _areaEffect.ApplySingleImpact();
        }

        // 기화처럼 CreatesFloor=false인 포션은 착탄 효과만 적용하고 장판을 남기지 않는다.
        if (!IsCombined && !Data.CreatesFloor)
        {
            Remove();
        }
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