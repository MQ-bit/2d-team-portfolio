using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰을 관리하는 매니저입니다. 방(RoomDefinition)의 EnemySpawnPoint들을 받아서 보관합니다.
/// 방이 런타임에 생성되므로 어디서든 넘겨줄 수 있게 씬에 하나만 두는 싱글톤입니다.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    private static SpawnManager _instance;
    public static SpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SpawnManager>();
            }

            return _instance;
        }
    }

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> _enemySpawnPoints = new List<Transform>();

    [Header("Spawn Portal Visual")]
    [Tooltip("몬스터가 나오는 자리에 띄울 포탈 연출의 원본. 층 이동 포탈 프리팹을 넣으면 색만 바꿔서 쓴다.")]
    [SerializeField] private GameObject _spawnPortalPrefab;
    [Tooltip("원본 포탈 색 대신 쓸 색조(색상만 바꾸고 채도/밝기는 원본을 유지한다).")]
    [SerializeField] private Color _spawnPortalColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField, Min(0.1f)] private float _spawnPortalLifetime = 1.5f;

    public IReadOnlyList<Transform> EnemySpawnPoints => _enemySpawnPoints;

    private void Awake()
    {
        SingletonInitiate();
    }

    private void SingletonInitiate()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    public void AddEnemySpawnPoints(RoomDefinition room)
    {
        if (room == null)
        {
            return;
        }

        AddEnemySpawnPoints(room.EnemySpawnPoints);
    }

    public void AddEnemySpawnPoints(IReadOnlyList<Transform> spawnPoints)
    {
        if (spawnPoints == null)
        {
            return;
        }

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null || _enemySpawnPoints.Contains(spawnPoint))
            {
                continue;
            }

            _enemySpawnPoints.Add(spawnPoint);
        }
    }

    public bool TrySpawn(EnemySpawnRequest request, Transform spawnPoint, Transform parent, out WaveEnemyHandle handle)
    {
        handle = null;

        if (request.Prefab == null || spawnPoint == null)
        {
            return false;
        }

        handle = Instantiate(request.Prefab, spawnPoint.position, spawnPoint.rotation, parent);
        if (handle != null)
        {
            ShowSpawnPortal(spawnPoint.position);
        }

        return handle != null;
    }

    // 층 이동 포탈의 복제본에서 상호작용 기능을 빼고 색상만 바꿔 몬스터가 나온 자리에 잠깐 띄운다.
    private void ShowSpawnPortal(Vector3 position)
    {
        if (_spawnPortalPrefab == null)
        {
            return;
        }

        GameObject portal = Instantiate(_spawnPortalPrefab, position, Quaternion.identity);
        portal.name = "EnemySpawnPortal";

        if (portal.TryGetComponent(out InteractablePortal interactable))
        {
            interactable.enabled = false;
        }
        if (portal.TryGetComponent(out Collider2D portalCollider))
        {
            portalCollider.enabled = false;
        }

        // 도착 지점 연출은 멀리 떨어진 곳에 있으므로 쓰지 않는다.
        Transform destination = portal.transform.Find("Destination");
        if (destination != null)
        {
            Destroy(destination.gameObject);
        }

        Color.RGBToHSV(_spawnPortalColor, out float hue, out _, out _);
        foreach (SpriteRenderer sprite in portal.GetComponentsInChildren<SpriteRenderer>())
        {
            sprite.color = ShiftHue(sprite.color, hue);
        }
        foreach (ParticleSystem particle in portal.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = particle.main;
            main.startColor = ShiftHue(main.startColor, hue);

            if (particle.colorOverLifetime.enabled)
            {
                ParticleSystem.ColorOverLifetimeModule overLifetime = particle.colorOverLifetime;
                overLifetime.color = ShiftHue(overLifetime.color, hue);
            }
        }

        Destroy(portal, _spawnPortalLifetime); // TODO : 풀링 적용 시 반환으로 변경
    }

    private static Color ShiftHue(Color color, float hue)
    {
        Color.RGBToHSV(color, out _, out float saturation, out float value);
        Color shifted = Color.HSVToRGB(hue, saturation, value);
        shifted.a = color.a;
        return shifted;
    }

    private static ParticleSystem.MinMaxGradient ShiftHue(ParticleSystem.MinMaxGradient source, float hue)
    {
        switch (source.mode)
        {
            case ParticleSystemGradientMode.Color:
                return ShiftHue(source.color, hue);
            case ParticleSystemGradientMode.TwoColors:
                return new ParticleSystem.MinMaxGradient(ShiftHue(source.colorMin, hue), ShiftHue(source.colorMax, hue));
            case ParticleSystemGradientMode.Gradient:
            case ParticleSystemGradientMode.RandomColor:
                return new ParticleSystem.MinMaxGradient(ShiftGradient(source.gradient, hue));
            case ParticleSystemGradientMode.TwoGradients:
                return new ParticleSystem.MinMaxGradient(ShiftGradient(source.gradientMin, hue), ShiftGradient(source.gradientMax, hue));
            default:
                return source;
        }
    }

    private static Gradient ShiftGradient(Gradient source, float hue)
    {
        GradientColorKey[] colorKeys = source.colorKeys;
        for (int i = 0; i < colorKeys.Length; i++)
        {
            colorKeys[i].color = ShiftHue(colorKeys[i].color, hue);
        }

        Gradient shifted = new Gradient();
        shifted.SetKeys(colorKeys, source.alphaKeys);
        return shifted;
    }

    public void ClearEnemySpawnPoints()
    {
        _enemySpawnPoints.Clear();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
