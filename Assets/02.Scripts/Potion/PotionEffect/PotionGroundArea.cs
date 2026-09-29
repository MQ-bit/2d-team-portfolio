using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PotionGroundArea : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _duration = 3f;
    [SerializeField, Min(0f)] private float _radius = 1.5f;

    private CircleCollider2D _collider;
    private SpriteRenderer _renderer;
    private PotionAreaEffect _areaEffect;

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _renderer = GetComponent<SpriteRenderer>();
        _areaEffect = GetComponent<PotionAreaEffect>();
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
        Destroy(gameObject, _duration);
    }
}
