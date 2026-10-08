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
        return handle != null;
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
