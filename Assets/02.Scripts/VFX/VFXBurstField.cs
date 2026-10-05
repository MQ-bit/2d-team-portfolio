using System.Collections.Generic;
using UnityEngine;

// 타원 또는 연결된 웅덩이 안의 무작위 위치에서 VFX를 터뜨리고, 끝나면 다른 위치에서 다시 재생한다.
// 버스트마다 SpriteSheetPlayer를 새로 켜지 않고, 스프라이트는 VFX 교체 시 한 번만 만들어
// 풀링된 SpriteRenderer들이 공유한다 (버스트마다 Sprite.Create 하면 GC가 쌓인다).
public class VFXBurstField : MonoBehaviour
{
    [Header("타원 장판")]
    [SerializeField] private SpriteRenderer _groundRenderer;
    [SerializeField, Min(0.1f)] private float _width = 3f;
    [SerializeField, Range(0.1f, 1f)] private float _heightRatio = 0.5f;
    [SerializeField] private Color _groundColor = new Color(1f, 1f, 1f, 0.25f);

    [Header("버스트")]
    [SerializeField] private GameObject _vfxPrefab;
    private int _burstCount = 12;
    private float _burstSize = 0.4f;
    private float _sizeJitter = 0.3f;
    private float _spawnAreaRatio = 0.85f;
    private Vector2 _restDelayRange = new(0f, 0.08f);
    private bool _fillArea;
    private float _fillScale = 1f;
    private bool _playOnce;
    private bool _legacySinglePotionBursts;
    private bool _intenseCombinationBursts;
    private bool _anchorAtBottom;

    private class Burst
    {
        public SpriteRenderer Renderer;
        public float StartTime;
        public Vector3 LocalAnchor;
    }

    private readonly List<Burst> _bursts = new();
    private Sprite[] _sprites;
    private float _fps;
    private SpriteRenderer _sortingRenderer;
    private float _visibility = 1f;
    private Color _vfxColor = Color.white;
    private Color _tint = Color.white;
    private PotionPuddleShape _puddle;
    private PotionCombinationSpread _spread;
    private bool _showReached;
    private Vector2[][] _footprint;
    private PotionAreaFootprint _clippedFootprint;
    private readonly PotionAreaMaskRenderer _clipMask = new();
    private MaterialPropertyBlock _clipProperties;
    private Material _clipMaterial;

    public float Width => _width;
    public float Height => _width * _heightRatio;

    // Only potion fields opt in; standalone VFX previews retain their own settings.

    public void SetVisibility(float visibility) => _visibility = Mathf.Clamp01(visibility);

    public void SetTint(Color tint)
    {
        _tint = tint;
        foreach (Burst burst in _bursts) burst.Renderer.color = _vfxColor * _tint;
    }

    internal void ApplyCombinationPalette(Color areaColor)
    {
        // A light tint preserves the sprite's highlights and the prefab's opacity.
        areaColor.a = 1f;
        SetTint(_tint * Color.Lerp(Color.white, areaColor, 0.3f));
    }

    private void Awake()
    {
        _clipProperties = new MaterialPropertyBlock();
        ApplyGround();
    }

    private void Start()
    {
        if (_vfxPrefab != null && _sprites == null)
        {
            SetVfx(_vfxPrefab);
        }
    }

    private void OnValidate()
    {
        ApplyGround();
    }

    private void OnDestroy()
    {
        ReleaseSprites();
        _clipMask.Release();
        if (_clipMaterial != null) Destroy(_clipMaterial);
    }

    // 장판 색을 바꾼다. 알파는 인스펙터의 _groundColor 알파를 유지한다.

    // 포션이 관리하는 바닥은 그대로 두고, 버스트 배치 범위와 표시 순서만 설정한다.
    public void ConfigureArea(float width, float height, SpriteRenderer sortingRenderer)
    {
        _width = Mathf.Max(0.1f, width);
        _heightRatio = Mathf.Max(0.001f, height / _width);
        _puddle = null;
        _spread = null;
        _legacySinglePotionBursts = false;
        _intenseCombinationBursts = false;
        _footprint = null;
        _clippedFootprint = null;
        _clipMask.SetFootprint(null);
        _sortingRenderer = sortingRenderer;
    }

    internal void ConfigureFootprint(Vector2[][] paths) => _footprint = paths;

    // Opt in only for single floor potions; combinations and vapor keep prefab settings.
    internal void UseLegacySinglePotionBursts() => _legacySinglePotionBursts = true;

    internal void UseIntenseCombinationBursts() => _intenseCombinationBursts = true;

    internal void ConfigureFootprint(PotionAreaFootprint footprint)
    {
        _clippedFootprint = footprint != null && footprint.IsClipped ? footprint : null;
        _clipMask.SetFootprint(_clippedFootprint);
        if (_clippedFootprint != null && _clipMaterial == null)
        {
            Shader shader = Resources.Load<Shader>("PotionAreaVfxClip");
            if (shader != null) _clipMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }
        if (_sprites == null || _sprites.Length == 0) return;
        var source = _vfxPrefab.GetComponent<SpriteRenderer>();
        foreach (Burst burst in _bursts)
        {
            burst.Renderer.sharedMaterial = _clippedFootprint != null && _clipMaterial != null
                ? _clipMaterial : source.sharedMaterial;
            burst.Renderer.GetPropertyBlock(_clipProperties);
            _clipMask.Apply(_clipProperties);
            burst.Renderer.SetPropertyBlock(_clipProperties);
            Respawn(burst, burst.StartTime);
        }
    }

    internal void ConfigurePuddle(PotionPuddleShape shape)
    {
        _puddle = shape;
        // SetVfx may have already placed the initial bursts using the default ellipse.
        if (_sprites == null || _sprites.Length == 0) return;
        foreach (Burst burst in _bursts) Respawn(burst, burst.StartTime);
    }

    internal void ConfigureSpread(PotionCombinationSpread spread, bool showReached)
    {
        _spread = spread;
        _showReached = showReached;
    }

    public void SetVfx(GameObject vfxPrefab)
    {
        var player = vfxPrefab != null ? vfxPrefab.GetComponent<SpriteSheetPlayer>() : null;
        if (player == null)
        {
            Debug.LogError("VFXBurstField requires a prefab with SpriteSheetPlayer.", this);
            return;
        }

        _vfxPrefab = vfxPrefab;
        ReadSettings(vfxPrefab);
        PrepareFrames(player);
        if (_sprites.Length == 0)
        {
            return;
        }

        StartBursts(vfxPrefab);
    }

    private void ReadSettings(GameObject vfxPrefab)
    {
        var settings = vfxPrefab.GetComponent<PotionAreaVfxSettings>();
        _burstCount = settings != null ? Mathf.Max(1, settings.burstCount) : 12;
        _burstSize = settings != null ? Mathf.Max(0.05f, settings.burstSize) : 0.4f;
        _sizeJitter = settings != null ? Mathf.Clamp(settings.sizeJitter, 0f, 0.9f) : 0.3f;
        _spawnAreaRatio = settings != null ? Mathf.Clamp01(settings.spawnAreaRatio) : 0.85f;
        Vector2 delay = settings != null ? settings.restDelayRange : new Vector2(0f, 0.08f);
        _restDelayRange = new Vector2(Mathf.Max(0f, Mathf.Min(delay.x, delay.y)),
            Mathf.Max(0f, Mathf.Max(delay.x, delay.y)));
        _tint = settings != null ? settings.tint : Color.white;
        _anchorAtBottom = settings != null && settings.anchorAtBottom;
        _fillArea = settings != null && settings.fillArea;
        _fillScale = settings != null ? Mathf.Max(0.05f, settings.fillScale) : 1f;
        _playOnce = settings != null && settings.playOnce;
        if (_legacySinglePotionBursts)
        {
            // Match PotionGroundArea.prefab at 8f5a966, before per-VFX profiles.
            _burstCount = 30;
            _burstSize = 1f;
            _sizeJitter = 0.3f;
            _spawnAreaRatio = 0.85f;
            _restDelayRange = new Vector2(0f, 0.08f);
            _tint = Color.white;
            _fillArea = false;
            _playOnce = false;
        }
        if (_intenseCombinationBursts)
        {
            // Broad effects retain their fill sizing; small bursts gain density and size.
            _burstCount = _fillArea
                ? Mathf.Max(5, Mathf.CeilToInt(_burstCount * 1.5f))
                : Mathf.Max(36, Mathf.CeilToInt(_burstCount * 1.5f));
            if (settings == null || !settings.preserveBurstSizeOnCombination)
                _burstSize = Mathf.Max(1f, _burstSize * 1.25f);
            _fillScale *= 1.2f;
            _restDelayRange *= 0.5f;
        }
    }

    private void PrepareFrames(SpriteSheetPlayer player)
    {
        ReleaseSprites();
        _sprites = player.CreateSprites();
        _fps = Mathf.Max(1f, player.fps);
    }

    private void StartBursts(GameObject vfxPrefab)
    {
        var source = vfxPrefab.GetComponent<SpriteRenderer>();
        _vfxColor = source.color;
        if (_intenseCombinationBursts)
        {
            _vfxColor.r *= 1.15f;
            _vfxColor.g *= 1.15f;
            _vfxColor.b *= 1.15f;
        }
        EnsurePool();

        float duration = _sprites.Length / _fps;
        foreach (var burst in _bursts)
        {
            burst.Renderer.sharedMaterial = _clippedFootprint != null && _clipMaterial != null
                ? _clipMaterial : source.sharedMaterial;
            burst.Renderer.GetPropertyBlock(_clipProperties);
            _clipMask.Apply(_clipProperties);
            burst.Renderer.SetPropertyBlock(_clipProperties);
            burst.Renderer.color = _vfxColor * _tint;
            // 처음부터 전부 동시에 터지지 않도록 재생 시점을 흩어 둔다.
            Respawn(burst, _playOnce ? Time.time : Time.time - Random.Range(0f, duration));
        }
    }

    private void Update()
    {
        if (_spread != null && !_showReached && _spread.IsComplete)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        if (_sprites == null || _sprites.Length == 0)
        {
            return;
        }

        float now = Time.time;
        foreach (var burst in _bursts)
        {
            float elapsed = now - burst.StartTime;
            if (elapsed < 0f)
            {
                burst.Renderer.enabled = false;
                continue;
            }

            int frame = (int)(elapsed * _fps);
            if (frame >= _sprites.Length)
            {
                burst.Renderer.enabled = false;
                if (!_playOnce)
                    Respawn(burst, now + Random.Range(_restDelayRange.x, _restDelayRange.y));
                continue;
            }

            burst.Renderer.enabled = true;
            ApplyFrame(burst, _sprites[frame]);
            Color color = _vfxColor * _tint;
            color.a *= _visibility;
            if (_spread != null && _spread.HasReached(transform.TransformPoint(burst.LocalAnchor)) != _showReached)
                color.a = 0f;
            burst.Renderer.color = color;
        }
    }

    private void EnsurePool()
    {
        while (_bursts.Count < _burstCount)
        {
            var go = new GameObject("Burst" + _bursts.Count);
            go.transform.SetParent(transform, false);
            _bursts.Add(new Burst { Renderer = go.AddComponent<SpriteRenderer>() });
        }

        while (_bursts.Count > _burstCount)
        {
            int last = _bursts.Count - 1;
            Destroy(_bursts[last].Renderer.gameObject);
            _bursts.RemoveAt(last);
        }
    }

    private void Respawn(Burst burst, float startTime)
    {
        burst.StartTime = startTime;

        // Sample uniformly inside the ellipse, without grid snapping or spacing correction.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(Random.value) * _spawnAreaRatio;
        Vector3 local = new(Mathf.Cos(angle) * radius * _width * 0.5f,
            Mathf.Sin(angle) * radius * Height * 0.5f, 0f);
        if (_puddle != null) local = _puddle.SampleLocalPoint(_spawnAreaRatio);
        if (_footprint != null) local = SampleFootprint();
        if (_footprint == null && _fillArea && _burstCount == 1) local = Vector3.zero;
        if (_clippedFootprint != null && !(_fillArea && _burstCount == 1))
        {
            // Rejection retains the profile's preferred spread; area-weighted
            // sampling supplies a valid fallback for narrow remaining slivers.
            if (!_clippedFootprint.Contains(transform.TransformPoint(local)))
                local = transform.InverseTransformPoint(_clippedFootprint.SamplePoint());
        }

        Transform t = burst.Renderer.transform;
        burst.LocalAnchor = local;
        float spriteWidth = Mathf.Max(0.0001f, _sprites[0].bounds.size.x);
        float size = _fillArea && _footprint == null ? Width * _fillScale
            : _burstSize * Random.Range(1f - _sizeJitter, 1f + _sizeJitter);
        t.localScale = Vector3.one * (size / spriteWidth);
        burst.Renderer.enabled = startTime <= Time.time;
        burst.Renderer.flipX = Random.value < 0.5f;
        ApplyFrame(burst, _sprites[0]);

        // 아래쪽(앞쪽) 버스트가 위에 그려지도록 y 기준으로 정렬한다.
        SpriteRenderer sortingRenderer = _sortingRenderer != null ? _sortingRenderer : _groundRenderer;
        int baseOrder = sortingRenderer != null ? sortingRenderer.sortingOrder : 0;
        burst.Renderer.sortingLayerID = sortingRenderer != null ? sortingRenderer.sortingLayerID : 0;
        burst.Renderer.sortingOrder = baseOrder + 1 + Mathf.RoundToInt((Height * 0.5f - local.y) * 100f);
    }

    private void ApplyFrame(Burst burst, Sprite sprite)
    {
        SpriteRenderer renderer = burst.Renderer;
        renderer.sprite = sprite;
        Vector3 position = burst.LocalAnchor;
        if (_anchorAtBottom)
        {
            // Keep the bottom center on the sampled floor point, even when frame bounds change.
            Vector3 scale = renderer.transform.localScale;
            position.x -= sprite.bounds.center.x * scale.x * (renderer.flipX ? -1f : 1f);
            position.y -= sprite.bounds.min.y * scale.y;
        }
        renderer.transform.localPosition = position;
    }

    private Vector2 SampleFootprint()
    {
        // Rejection sampling preserves concave connections between combined lobes.
        for (int attempt = 0; attempt < 256; attempt++)
        {
            Vector2 point = new(Random.Range(-Width * 0.5f, Width * 0.5f),
                Random.Range(-Height * 0.5f, Height * 0.5f));
            foreach (Vector2[] path in _footprint)
            {
                bool inside = false;
                for (int i = 0, j = path.Length - 1; i < path.Length; j = i++)
                {
                    Vector2 a = path[i], b = path[j];
                    if ((a.y > point.y) != (b.y > point.y) &&
                        point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    {
                        inside = !inside;
                    }
                }
                if (inside) return point;
            }
        }
        foreach (Vector2[] path in _footprint)
            if (path.Length > 0) return path[0];
        return Vector2.zero;
    }

    private void ApplyGround()
    {
        if (_groundRenderer == null || _groundRenderer.sprite == null)
        {
            return;
        }

        Vector3 spriteSize = _groundRenderer.sprite.bounds.size;
        _groundRenderer.transform.localScale = new Vector3(_width / spriteSize.x, Height / spriteSize.y, 1f);
        _groundRenderer.color = _groundColor;
    }

    private void ReleaseSprites()
    {
        if (_sprites == null)
        {
            return;
        }

        foreach (var sprite in _sprites)
        {
            if (sprite != null)
            {
                Destroy(sprite);
            }
        }
        _sprites = null;
    }
}
