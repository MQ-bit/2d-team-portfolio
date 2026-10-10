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
    [Header("Spawn Pool Settings")]
    [SerializeField] private GameObject[] _monsterPrefabs;
    [SerializeField, Min(0)] private int _poolExpandSize = 10; // 각 몬스터 프리팹에 대한 풀 확장 단위 크기

    private readonly Dictionary<GameObject, IPoolSource<GameObject>> _pools = new Dictionary<GameObject, IPoolSource<GameObject>>();

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> _enemySpawnPoints = new List<Transform>();

    public IReadOnlyList<Transform> EnemySpawnPoints => _enemySpawnPoints;

    private void Awake()
    {
        SingletonInitiate();
        InitializePools();
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

        GameObject instance = Spawn(request.Prefab.gameObject, spawnPoint.position, spawnPoint.rotation);

        if (instance == null)
        {
            return false;
        }
        if (parent != null)
        {
            instance.transform.SetParent(parent, true);
        }

        handle = instance.GetComponent<WaveEnemyHandle>();
        return handle != null;
    }

    public void ClearEnemySpawnPoints()
    {
        _enemySpawnPoints.Clear();
    }

    private void InitializePools()
    {
        foreach (GameObject prefab in _monsterPrefabs)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"{name}: null 몬스터 Prefab({prefab.name})이 등록되어있습니다.");
                continue;
            }

            if (_pools.ContainsKey(prefab))
            {
                Debug.LogWarning($"{name}: 이미 등록된 몬스터 Prefab({prefab.name})이 있습니다.");
                continue;
            }

            GameObject poolObject = new GameObject($"{prefab.name} Pool");
            poolObject.transform.SetParent(transform, false);

            MonsterPool pool = poolObject.AddComponent<MonsterPool>();
            pool.Initialize(prefab, _poolExpandSize, transform);
            _pools.Add(prefab, pool);
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null || !_pools.TryGetValue(prefab, out IPoolSource<GameObject> pool))
        {
            Debug.LogWarning($"{name}: {prefab?.name}에 대한 풀을 찾을 수 없습니다.");
            return null;
        }

        return pool.Rent(position, rotation);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
