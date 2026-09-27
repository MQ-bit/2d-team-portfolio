using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(PotionAreaEffect))]
public sealed class PotionGroundArea : MonoBehaviour
{
    private CircleCollider2D _collider;
    private SpriteRenderer _renderer;
    private PotionAreaEffect _areaEffect;
    private float _duration;
    private bool _initialized;

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _renderer = GetComponent<SpriteRenderer>();
        _areaEffect = GetComponent<PotionAreaEffect>();
        _collider.isTrigger = true;
        _collider.enabled = false;
    }

    public void Initialize(PotionData data)
    {
        if (data == null)
        {
            Debug.LogError("PotionGroundArea requires PotionData.", this);
            return;
        }

        _duration = data.AreaDuration;
        _collider.radius = data.AreaRadius;

        if (_renderer != null)
        {
            _renderer.color = data.VisualData.AreaColor;
        }

        _areaEffect.Initialize(data);
        _initialized = true;
        _collider.enabled = true;
    }

    public void Activate()
    {
        if (!_initialized)
        {
            Debug.LogError("PotionGroundArea must be initialized before activation.", this);
            return;
        }

        Destroy(gameObject, _duration);
    }
}
