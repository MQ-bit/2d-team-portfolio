using UnityEngine;

// 임시 투사체: 전용 스프라이트가 없으면 원형 텍스처를 직접 생성해서 사용한다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Rigidbody2D _rigid;
    [SerializeField] private SpriteRenderer _spriteRenderer;

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
        PotionEffectReceiver receiver = other.GetComponentInParent<PotionEffectReceiver>();
        if (receiver == null || receiver.TargetKind != PotionTargetKind.Player)
        {
            return;
        }

        receiver.ModifyHealth(-_damage);
        Destroy(gameObject);
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
