using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PotionGroundArea : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _duration = 3f;
    [SerializeField, Min(0f)] private float _radius = 1.5f;

    private CircleCollider2D _collider;
    private SpriteRenderer _renderer;
    private PotionAreaEffect _areaEffect;
    private PotionAreaCombiner _combiner;
    private float _expiresAt;
    private bool _activated;

    public PotionData Data { get; private set; }
    public IDamageable Owner { get; private set; }
    public CircleCollider2D AreaCollider => _collider;
    public PotionAreaEffect AreaEffect => _areaEffect;
    public bool IsCombined => _combiner != null && _combiner.IsCombined;

    public void SetOwner(IDamageable owner) => Owner = owner;

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _renderer = GetComponent<SpriteRenderer>();
        _areaEffect = GetComponent<PotionAreaEffect>();
        _combiner = GetComponent<PotionAreaCombiner>();
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
        _duration = data.AreaDuration;
        _radius = data.AreaWidth * 0.5f;
        _collider.radius = _radius;

        if (_renderer != null)
        {
            _renderer.color = data.VisualData.AreaColor;
        }

        if (_areaEffect != null)
        {
            _areaEffect.Initialize(data);
        }
    }

    public void Activate()
    {
        if (Data == null || _activated) return;
        _activated = true;
        _expiresAt = Time.time + _duration;
        if (_combiner != null) _combiner.RegisterArea();

        // 기화는 착탄 순간의 조합 판정에만 사용하고 독립 장판을 남기지 않는다.
        if (Data.Element == PotionElement.Vaporization && !IsCombined)
        {
            Remove();
        }
    }

    public void SetExpiration(float expiresAt)
    {
        _expiresAt = expiresAt;
    }

    private void Update()
    {
        if (_activated && Time.time >= _expiresAt) Remove();
    }

    public void Remove()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
