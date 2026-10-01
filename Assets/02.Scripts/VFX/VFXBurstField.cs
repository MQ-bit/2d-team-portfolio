using System.Collections.Generic;
using UnityEngine;

// 타원 장판 안의 무작위 위치에서 VFX를 한 번씩 터뜨리고, 끝나면 다른 위치로 옮겨 다시 터뜨린다.
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
    [SerializeField, Min(1)] private int _burstCount = 40;
    [SerializeField, Min(0.05f)] private float _burstSize = 0.4f;
    [SerializeField, Range(0f, 0.9f)] private float _sizeJitter = 0.3f;
    [SerializeField, Range(0.1f, 1f)] private float _spawnAreaRatio = 0.85f;
    [SerializeField] private Vector2 _restDelayRange = new Vector2(0f, 0.08f);

    private class Burst
    {
        public SpriteRenderer Renderer;
        public float StartTime;
    }

    private readonly List<Burst> _bursts = new();
    private Sprite[] _sprites;
    private float _fps;
    private SpriteRenderer _sortingRenderer;

    public float Width => _width;
    public float Height => _width * _heightRatio;

    private void Awake()
    {
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
    }

    // 장판 색을 바꾼다. 알파는 인스펙터의 _groundColor 알파를 유지한다.
    public void SetGroundColor(Color color)
    {
        color.a = _groundColor.a;
        _groundColor = color;
        ApplyGround();
    }

    // 포션이 관리하는 바닥은 그대로 두고, 버스트 배치 범위와 표시 순서만 설정한다.
    public void ConfigureArea(float width, float height, SpriteRenderer sortingRenderer,
        int burstCount, float burstSize, float spawnAreaRatio)
    {
        _width = Mathf.Max(0.1f, width);
        _heightRatio = Mathf.Clamp(height / _width, 0.001f, 1f);
        _sortingRenderer = sortingRenderer;
        _burstCount = Mathf.Max(1, burstCount);
        _burstSize = Mathf.Max(0.05f, burstSize);
        _spawnAreaRatio = Mathf.Clamp01(spawnAreaRatio);
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
        ReleaseSprites();
        _sprites = player.CreateSprites();
        _fps = Mathf.Max(1f, player.fps);
        if (_sprites.Length == 0)
        {
            return;
        }

        var source = vfxPrefab.GetComponent<SpriteRenderer>();
        EnsurePool();

        float duration = _sprites.Length / _fps;
        foreach (var burst in _bursts)
        {
            burst.Renderer.sharedMaterial = source.sharedMaterial;
            burst.Renderer.color = source.color;
            // 처음부터 전부 동시에 터지지 않도록 재생 시점을 흩어 둔다.
            Respawn(burst, Time.time - Random.Range(0f, duration));
        }
    }

    private void Update()
    {
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
                Respawn(burst, now + Random.Range(_restDelayRange.x, _restDelayRange.y));
                continue;
            }

            burst.Renderer.enabled = true;
            burst.Renderer.sprite = _sprites[frame];
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

        // 타원 내부 균일 분포: 반지름에 sqrt를 씌워야 가운데로 몰리지 않는다.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(Random.value) * _spawnAreaRatio;
        var local = new Vector3(Mathf.Cos(angle) * radius * _width * 0.5f, Mathf.Sin(angle) * radius * Height * 0.5f, 0f);

        Transform t = burst.Renderer.transform;
        t.localPosition = local;
        float spriteWidth = Mathf.Max(0.0001f, _sprites[0].bounds.size.x);
        float size = _burstSize * Random.Range(1f - _sizeJitter, 1f + _sizeJitter);
        t.localScale = Vector3.one * (size / spriteWidth);
        burst.Renderer.flipX = Random.value < 0.5f;

        // 아래쪽(앞쪽) 버스트가 위에 그려지도록 y 기준으로 정렬한다.
        SpriteRenderer sortingRenderer = _sortingRenderer != null ? _sortingRenderer : _groundRenderer;
        int baseOrder = sortingRenderer != null ? sortingRenderer.sortingOrder : 0;
        burst.Renderer.sortingLayerID = sortingRenderer != null ? sortingRenderer.sortingLayerID : 0;
        burst.Renderer.sortingOrder = baseOrder + 1 + Mathf.RoundToInt((Height * 0.5f - local.y) * 100f);
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
