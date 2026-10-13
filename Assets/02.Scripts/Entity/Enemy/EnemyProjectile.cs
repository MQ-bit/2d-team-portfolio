using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D _rigid;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Visual Size")]
    [Tooltip("Visual 자식의 초기 크기 배율.")]
    [SerializeField, Min(0.01f)] private float _visualScale = 1.4f;
    [Tooltip("초기 크기에 대한 최대 성장 배율. 이동 거리가 길어도 이 크기를 넘지 않는다.")]
    [SerializeField, Min(1f)] private float _maxGrowthMultiplier = 1.2f;
    [Tooltip("이 거리를 이동하면 최대 크기에 도달한다.")]
    [SerializeField, Min(0.01f)] private float _growthDistance = 6f;
    [SerializeField] private bool _scaleColliderWithVisual = true;

    [Header("Flight Particles")]
    [SerializeField] private ParticleSystem _flightEffectPrefab;
    [SerializeField, Min(0.01f)] private float _flightEffectScale = 1f;
    [Tooltip("투사체가 사라진 뒤 남은 잔상을 정리하기까지의 최소 시간.")]
    [SerializeField, Min(0.1f)] private float _flightEffectLifetime = 1f;

    [Header("Impact")]
    [SerializeField] private GameObject _impactEffectPrefab;
    [SerializeField, Min(0.01f)] private float _impactEffectScale = 1f;
    [SerializeField, Min(0.1f)] private float _impactEffectLifetime = 1f;

    [Header("Placeholder Visual")]
    [SerializeField] private Color _color = Color.magenta;

    private static Sprite _placeholderSprite;

    private Vector2 _direction;
    private float _speed;
    private float _damage;
    private ParticleSystem _flightEffect;
    private bool _hasImpacted;
    private Vector3 _initialVisualScale;
    private CircleCollider2D _bodyCollider;
    private float _initialColliderRadius;
    private Vector2 _lastPosition;
    private float _travelledDistance;
    private float _growthMultiplier = 1f;

    private void Awake()
    {
        if (_rigid == null)
        {
            _rigid = GetComponent<Rigidbody2D>();
        }
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (_spriteRenderer != null && _spriteRenderer.sprite == null)
        {
            _spriteRenderer.sprite = GetPlaceholderSprite();
            _spriteRenderer.color = _color;
        }
        if (_spriteRenderer != null)
            _initialVisualScale = _spriteRenderer.transform.localScale;
        _bodyCollider = GetComponent<CircleCollider2D>();
        _initialColliderRadius = _bodyCollider.radius;
        ApplySize();
    }

    public void Launch(Vector2 direction, float speed, float damage, float lifetime)
    {
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;
        _hasImpacted = false;
        _lastPosition = _rigid.position;
        _travelledDistance = 0f;
        _growthMultiplier = 1f;
        ApplySize();
        SpawnFlightEffect();

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        // 물리 몸통이 실제로 이동한 거리를 누적해서 크기를 키운다.
        _travelledDistance += Vector2.Distance(_lastPosition, _rigid.position);
        _lastPosition = _rigid.position;
        float progress = Mathf.Clamp01(_travelledDistance / Mathf.Max(0.01f, _growthDistance));
        _growthMultiplier = Mathf.SmoothStep(1f, Mathf.Max(1f, _maxGrowthMultiplier), progress);
        ApplySize();

        _rigid.MovePosition(_rigid.position + _direction * _speed * Time.fixedDeltaTime);
    }

    private void ApplySize()
    {
        float scale = Mathf.Max(0.01f, _visualScale) * _growthMultiplier;
        if (_spriteRenderer != null && _spriteRenderer.transform != transform)
            _spriteRenderer.transform.localScale = _initialVisualScale * scale;
        if (_bodyCollider != null)
            _bodyCollider.radius = _initialColliderRadius * (_scaleColliderWithVisual ? scale : 1f);
    }

    // 플레이어는 태그/레이어가 없어서 TargetKind가 Player인 PotionEffectReceiver로 찾는다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasImpacted) return;
        // 플레이어의 아이템 수집 범위 같은 트리거 영역은 몸통이 아니므로 무시한다.
        if (other.isTrigger)
        {
            return;
        }

        PotionEffectReceiver receiver = other.GetComponentInParent<PotionEffectReceiver>();
        if (receiver == null || receiver.TargetKind != PotionTargetKind.Player)
        {
            return;
        }
        _hasImpacted = true;

        // 맞은 방향을 알 수 있으면 함께 넘겨서 피격 연출이 탄환 진행 방향으로 튀게 한다.
        if (other.GetComponentInParent<IDirectionalDamageable>() is IDirectionalDamageable directional)
        {
            directional.DecreaseHealth(_damage, _direction);
        }
        else
        {
            other.GetComponentInParent<IDamageable>()?.DecreaseHealth(_damage);
        }

        SpawnImpactEffect();
        Destroy(gameObject);
    }

    private void SpawnImpactEffect()
    {
        if (_impactEffectPrefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(_impactEffectPrefab, transform.position, Quaternion.identity);
        effect.transform.localScale *= _impactEffectScale * _growthMultiplier;
        ConfigureParticleRenderers(effect, 1);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            particles.Play();
        Destroy(effect, _impactEffectLifetime); // TODO : 풀링 적용 시 반환으로 변경
    }

    private void SpawnFlightEffect()
    {
        if (_flightEffectPrefab == null || _flightEffect != null) return;
        Transform origin = _spriteRenderer != null ? _spriteRenderer.transform : transform;
        _flightEffect = Instantiate(_flightEffectPrefab, origin.position, Quaternion.identity);
        _flightEffect.transform.localScale *= _flightEffectScale;
        // 월드 크기를 유지한 채 따라가게 해서 투사체 본체 Scale과 분리한다.
        _flightEffect.transform.SetParent(origin, true);
        ConfigureParticleRenderers(_flightEffect.gameObject, -1);
        _flightEffect.Play(true);
    }

    private void ConfigureParticleRenderers(GameObject effect, int orderOffset)
    {
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
        if (_spriteRenderer == null) return;
        foreach (ParticleSystemRenderer renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            renderer.sortingLayerID = _spriteRenderer.sortingLayerID;
            renderer.sortingOrder = _spriteRenderer.sortingOrder + orderOffset;
        }
    }

    private void OnDisable()
    {
        if (_flightEffect == null) return;
        _flightEffect.transform.SetParent(null, true);
        float lifetime = _flightEffectLifetime;
        foreach (ParticleSystem particles in _flightEffect.GetComponentsInChildren<ParticleSystem>(true))
            lifetime = Mathf.Max(lifetime, particles.main.startLifetime.constantMax);
        _flightEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(_flightEffect.gameObject, lifetime);
        _flightEffect = null;
    }

    private static Sprite GetPlaceholderSprite()
    {
        if (_placeholderSprite != null)
        {
            return _placeholderSprite;
        }

        const int Diameter = 32;
        const float Radius = Diameter * 0.5f;

        Texture2D texture = new Texture2D(Diameter, Diameter, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < Diameter; y++)
        {
            for (int x = 0; x < Diameter; x++)
            {
                float dx = x + 0.5f - Radius;
                float dy = y + 0.5f - Radius;
                bool isInsideCircle = dx * dx + dy * dy <= Radius * Radius;
                texture.SetPixel(x, y, isInsideCircle ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        texture.Apply();

        _placeholderSprite = Sprite.Create(texture, new Rect(0f, 0f, Diameter, Diameter), new Vector2(0.5f, 0.5f), Diameter);
        return _placeholderSprite;
    }
}
