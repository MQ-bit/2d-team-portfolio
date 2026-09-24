using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class PotionGroundArea : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _duration = 3f;
    [SerializeField, Min(0f)] private float _radius = 1.5f;

    private CircleCollider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _collider.isTrigger = true;
        _collider.radius = _radius;
    }

    public void Initialize(PotionData data)
    {
        _duration = data.AreaDuration;
        _radius = data.Radius;
        _collider.radius = _radius;
    }

    public void Activate()
    {
        Destroy(gameObject, _duration);
    }
}
