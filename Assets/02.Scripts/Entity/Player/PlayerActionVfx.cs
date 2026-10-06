using System.Collections.Generic;
using UnityEngine;

// 플레이어 행동 이펙트: 대시를 시작하면(PlayerMovementModule.OnDashStarted) 대시 방향(동/서/남/북)에 맞는 먼지 이펙트를,
// 근접 공격을 휘두르면(PlayerMeleeModule.OnAttackStarted) 공격 방향으로 베기 이펙트를 한 번 재생한다.
// 이펙트 시트는 가로로 프레임이 나열된 한 줄짜리 텍스처이고, 여기서 프레임 단위로 잘라 쓴다.
// 이펙트는 플레이어를 따라다니지 않고 재생을 시작한 위치에 남는다.
public class PlayerActionVfx : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerMovementModule _movementModule;
    [SerializeField] private PlayerMeleeModule _meleeModule;
    [SerializeField] private SpriteRenderer _visualRenderer; // 이펙트의 머티리얼과 정렬 레이어를 여기서 따온다.
    [SerializeField] private Transform _attackOrigin;

    [Header("Common")]
    [SerializeField, Min(1f)] private float _pixelsPerUnit = 100f;
    [SerializeField, Min(0.01f)] private float _effectScale = 2f; // 대시 이펙트 배율. (플레이어 Visual 배율은 1.5)

    [Header("Dash Effect (정사각형 프레임이 가로로 나열된 시트)")]
    [SerializeField] private Texture2D _dashEastSheet;
    [SerializeField] private Texture2D _dashWestSheet;
    [SerializeField] private Texture2D _dashNorthSheet;
    [SerializeField] private Texture2D _dashSouthSheet;
    [SerializeField, Min(1f)] private float _dashFps = 20f;
    [SerializeField] private Vector2 _dashOriginOffset = new Vector2(0f, -1f); // 플레이어 발 위치
    // 프레임 안에서 대시 시작 지점(발 위치)에 놓일 점. (0,0)=왼쪽 아래, (1,1)=오른쪽 위
    [SerializeField] private Vector2 _dashEastPivot = new Vector2(0.5f, 0.19f);
    [SerializeField] private Vector2 _dashWestPivot = new Vector2(0.5f, 0.19f);
    [SerializeField] private Vector2 _dashNorthPivot = new Vector2(0.5f, 0.09f);
    [SerializeField] private Vector2 _dashSouthPivot = new Vector2(0.5f, 0.8f);
    [SerializeField] private int _dashSortingOrderOffset = -1;

    [Header("Attack Effect (아래쪽으로 휘두르는 모양의 시트)")]
    [SerializeField] private Texture2D _attackSheet;
    [SerializeField, Min(1)] private int _attackFrameCount = 4;
    [SerializeField, Min(1f)] private float _attackFps = 20f;
    // 이펙트는 근접 공격 범위(부채꼴)에 맞춰 커진다: 가로는 부채꼴 끝의 폭, 볼록한 끝은 사거리에 닿는다. 이 값으로 그 크기를 미세 조정한다.
    [SerializeField, Min(0.01f)] private float _attackSizeMultiplier = 1f;
    [SerializeField] private int _attackSortingOrderOffset = 1;

    private readonly List<Sprite> _createdSprites = new List<Sprite>();

    private Sprite[] _dashEastFrames;
    private Sprite[] _dashWestFrames;
    private Sprite[] _dashNorthFrames;
    private Sprite[] _dashSouthFrames;
    private Sprite[] _attackFrames;

    private OneShotSpriteEffect _dashEffect;
    private OneShotSpriteEffect _attackEffect;

    private void Awake()
    {
        if (_movementModule == null)
        {
            _movementModule = GetComponent<PlayerMovementModule>();
        }
        if (_meleeModule == null)
        {
            _meleeModule = GetComponent<PlayerMeleeModule>();
        }
        if (_visualRenderer == null)
        {
            Animator animator = GetComponentInChildren<Animator>();
            _visualRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        }
        if (_attackOrigin == null)
        {
            _attackOrigin = transform;
        }

        _dashEastFrames = CreateFrames(_dashEastSheet, GetSquareFrameCount(_dashEastSheet), _dashEastPivot);
        _dashWestFrames = CreateFrames(_dashWestSheet, GetSquareFrameCount(_dashWestSheet), _dashWestPivot);
        _dashNorthFrames = CreateFrames(_dashNorthSheet, GetSquareFrameCount(_dashNorthSheet), _dashNorthPivot);
        _dashSouthFrames = CreateFrames(_dashSouthSheet, GetSquareFrameCount(_dashSouthSheet), _dashSouthPivot);
        _attackFrames = CreateFrames(_attackSheet, _attackFrameCount, new Vector2(0.5f, 0.5f));

        _dashEffect = CreateEffect("PlayerDashEffect");
        _attackEffect = CreateEffect("PlayerAttackEffect");
    }

    private void OnEnable()
    {
        if (_movementModule != null)
        {
            _movementModule.OnDashStarted += HandleDashStarted;
        }
        if (_meleeModule != null)
        {
            _meleeModule.OnAttackStarted += HandleAttackStarted;
        }
    }

    private void OnDisable()
    {
        if (_movementModule != null)
        {
            _movementModule.OnDashStarted -= HandleDashStarted;
        }
        if (_meleeModule != null)
        {
            _meleeModule.OnAttackStarted -= HandleAttackStarted;
        }
    }

    private void OnDestroy()
    {
        // 이펙트는 플레이어의 자식이 아니라서 직접 정리한다.
        if (_dashEffect != null)
        {
            Destroy(_dashEffect.gameObject);
        }
        if (_attackEffect != null)
        {
            Destroy(_attackEffect.gameObject);
        }

        foreach (Sprite sprite in _createdSprites)
        {
            if (sprite != null)
            {
                Destroy(sprite);
            }
        }
        _createdSprites.Clear();
    }

    private void HandleDashStarted(float dashCooltime)
    {
        if (_dashEffect == null)
        {
            return;
        }

        Vector2 direction = _movementModule.LastDashDirection;

        ApplySorting(_dashEffect.Renderer, _dashSortingOrderOffset);
        _dashEffect.transform.SetPositionAndRotation((Vector2)transform.position + _dashOriginOffset, Quaternion.identity);
        _dashEffect.Play(SelectDashFrames(direction), _dashFps);
    }

    private void HandleAttackStarted(Vector2 attackDirection)
    {
        if (_attackEffect == null || _attackFrames.Length == 0)
        {
            return;
        }

        // 부채꼴 끝의 폭(현의 길이)에 프레임 가로를 맞추고, 볼록한 끝이 사거리에 닿도록 중심을 그만큼 안쪽에 둔다.
        float range = _meleeModule.AttackRange;
        float halfAngle = Mathf.Min(_meleeModule.AttackAngle, 180f) * 0.5f * Mathf.Deg2Rad;
        Vector2 frameSize = _attackFrames[0].bounds.size;
        float scale = 2f * range * Mathf.Sin(halfAngle) / frameSize.x * _attackSizeMultiplier;
        float distance = Mathf.Max(0f, range - frameSize.y * scale * 0.5f);

        // 시트의 베기 모양이 아래쪽(-Y)을 향하므로 그 방향이 공격 방향이 되도록 돌린다.
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg + 90f;
        Vector2 position = (Vector2)_attackOrigin.position + attackDirection * distance;

        ApplySorting(_attackEffect.Renderer, _attackSortingOrderOffset);
        _attackEffect.transform.localScale = new Vector3(scale, scale, 1f);
        _attackEffect.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
        _attackEffect.Play(_attackFrames, _attackFps);
    }

    // 가로/세로 중 더 큰 축으로 동서남북 중 하나를 고른다. 같으면 가로를 우선한다. (PlayerAnimationDriver와 같은 기준)
    private Sprite[] SelectDashFrames(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? _dashEastFrames : _dashWestFrames;
        }

        return direction.y >= 0f ? _dashNorthFrames : _dashSouthFrames;
    }

    private OneShotSpriteEffect CreateEffect(string effectName)
    {
        GameObject effectObject = new GameObject(effectName);
        effectObject.transform.localScale = new Vector3(_effectScale, _effectScale, 1f);

        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        if (_visualRenderer != null)
        {
            effectRenderer.sharedMaterial = _visualRenderer.sharedMaterial;
        }

        return effectObject.AddComponent<OneShotSpriteEffect>();
    }

    // 씬마다 플레이어의 정렬 레이어가 다를 수 있어서(PlayerAnimationDriver 참고) 재생할 때마다 Visual의 정렬값을 따른다.
    private void ApplySorting(SpriteRenderer effectRenderer, int sortingOrderOffset)
    {
        if (_visualRenderer == null)
        {
            return;
        }

        effectRenderer.sortingLayerID = _visualRenderer.sortingLayerID;
        effectRenderer.sortingOrder = _visualRenderer.sortingOrder + sortingOrderOffset;
    }

    private static int GetSquareFrameCount(Texture2D sheet)
    {
        return sheet != null ? Mathf.Max(1, sheet.width / sheet.height) : 0;
    }

    private Sprite[] CreateFrames(Texture2D sheet, int frameCount, Vector2 pivot)
    {
        if (sheet == null || frameCount <= 0)
        {
            return new Sprite[0];
        }

        float frameWidth = sheet.width / (float)frameCount;
        Sprite[] frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            Rect rect = new Rect(i * frameWidth, 0f, frameWidth, sheet.height);
            frames[i] = Sprite.Create(sheet, rect, pivot, _pixelsPerUnit);
            _createdSprites.Add(frames[i]);
        }

        return frames;
    }
}
