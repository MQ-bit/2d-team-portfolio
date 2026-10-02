using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D _rigid;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Impact")]
    [SerializeField] private GameObject _impactEffectPrefab;
    [SerializeField, Min(0.1f)] private float _impactEffectLifetime = 1f;

    [Header("Placeholder Visual")]
    [SerializeField] private Color _color = Color.magenta;

    private static Sprite _placeholderSprite;

    private Vector2 _direction;
    private float _speed;
    private float _damage;

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
    }

    public void Launch(Vector2 direction, float speed, float damage, float lifetime)
    {
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        _rigid.MovePosition(_rigid.position + _direction * _speed * Time.fixedDeltaTime);
    }

    // 플레이어는 태그/레이어가 없어서 TargetKind가 Player인 PotionEffectReceiver로 찾는다.
    private void OnTriggerEnter2D(Collider2D other)
    {
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
        Destroy(effect, _impactEffectLifetime); // TODO : 풀링 적용 시 반환으로 변경
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
