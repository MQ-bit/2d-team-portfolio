using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class WaveController : MonoBehaviour
{
    [SerializeField] private SpawnManager _spawnManager; //캐싱용 변수

    private readonly HashSet<WaveEnemyHandle> _aliveEnemies = new HashSet<WaveEnemyHandle>();
    private readonly List<WaveEnemyHandle> _releaseBuffer = new List<WaveEnemyHandle>();

    private Coroutine _encounterCoroutine;
    private IReadOnlyList<Transform> _spawnPoints;
    private Transform _enemiesRoot;

    private int _encounterVersion;
    private int _prevSpawnPoint = -1;
    private bool _isCancelling;

    public bool IsRunning => _encounterCoroutine != null;
    public int CurrentWaveIndex { get; private set; } = -1;
    public int AliveEnemyCount => _aliveEnemies.Count;

    public event Action CombatCompleted;
    public event Action<string> CombatFailed;

    private void Awake()
    {
        if (_spawnManager == null)
        {
            _spawnManager = SpawnManager.Instance;
        }
    }

    private void OnDisable()
    {
        CancelEncounter(false);
    }

    public bool TryStartEncounter(ResolvedWavePlan plan, IReadOnlyList<Transform> spawnPoints, Transform enemiesRoot)
    {
        bool checkPlanOkay = (plan == null || plan.Waves == null || plan.Waves.Count == 0);
        bool checkSpawnPointOkay = (spawnPoints == null || spawnPoints.Count == 0);
        if (IsRunning || checkPlanOkay || checkSpawnPointOkay || enemiesRoot == null || _spawnManager == null)
        {
            return false;
        }

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] == null)
            {
                return false;
            }
        }

        _spawnPoints = spawnPoints;
        _enemiesRoot = enemiesRoot;
        _prevSpawnPoint = -1;
        CurrentWaveIndex = -1;

        int version = ++_encounterVersion;
        _encounterCoroutine = StartCoroutine(RunEncounter(plan, version));

        return true;
    }

    public void CancelEncounter(bool destroyTrackedEnemies)
    {
        _encounterVersion++;

        if (_encounterCoroutine != null)
        {
            StopCoroutine(_encounterCoroutine);
            _encounterCoroutine = null;
        }

        ReleaseTrackedEnemies(destroyTrackedEnemies);

        CurrentWaveIndex = -1;
        _spawnPoints = null;
        _enemiesRoot = null;
    }

    private IEnumerator RunEncounter(ResolvedWavePlan plan, int version)
    {
        for (int waveIndex = 0; waveIndex < plan.Waves.Count; waveIndex++) //몬스터를 n초 간격으로 연속 생성 후 m초간 또 텀을 둠
        {
            if (version != _encounterVersion)
            {
                yield break;
            }

            CurrentWaveIndex = waveIndex;

            ResolvedWave wave = plan.Waves[waveIndex];
            WaitForSeconds spawnIntervalWFS = new WaitForSeconds(wave.SpawnInterval);
            WaitForSeconds batchIntervalWFS = new WaitForSeconds(wave.BatchInterval);
            WaitForSeconds interWaveDelayWFS = new WaitForSeconds(plan.InterWaveDelay);

            for (int index = 0; index < wave.Requests.Count;)
            {
                int batchEnd = Mathf.Min(index + wave.BatchSize, wave.Requests.Count);

                while (index < batchEnd)
                {
                    if (version != _encounterVersion)
                    {
                        yield break;
                    }


                    Transform spawnPoint = GetNextSpawnPoint();

                    if (!_spawnManager.TrySpawn(wave.Requests[index], spawnPoint, _enemiesRoot, out WaveEnemyHandle enemy))
                    {
                        FailEncounter($"Wave {waveIndex + 1}, Enemy {index + 1} 생성에 실패했습니다.");
                        yield break;
                    }

                    TrackEnemy(enemy);
                    index++;

                    if (index < batchEnd && wave.SpawnInterval > 0f)
                    {
                        yield return spawnIntervalWFS;
                    }
                }

                if (index < wave.Requests.Count && wave.BatchInterval > 0f)
                {
                    yield return batchIntervalWFS;
                }
            }

            while (_aliveEnemies.Count > 0)
            {
                if (version != _encounterVersion)
                {
                    yield break;
                }

                yield return null;
            }

            if (waveIndex < plan.Waves.Count - 1 && plan.InterWaveDelay > 0f)
            {
                yield return interWaveDelayWFS;
            }
        }

        _encounterCoroutine = null;
        CurrentWaveIndex = -1;
        _spawnPoints = null;
        _enemiesRoot = null;

        CombatCompleted?.Invoke();
    }

    private void TrackEnemy(WaveEnemyHandle enemy)
    {
        if (enemy == null || !_aliveEnemies.Add(enemy))
        {
            return;
        }

        enemy.Released += HandleEnemyReleased;
    }

    private void HandleEnemyReleased(WaveEnemyHandle enemy)
    {
        if (_isCancelling || enemy == null)
        {
            return;
        }

        enemy.Released -= HandleEnemyReleased;
        _aliveEnemies.Remove(enemy);
    }

    private void FailEncounter(string reason)
    {
        _encounterCoroutine = null;
        _encounterVersion++;

        ReleaseTrackedEnemies(true);

        CurrentWaveIndex = -1;
        _spawnPoints = null;
        _enemiesRoot = null;

        CombatFailed?.Invoke(reason);
    }

    private void ReleaseTrackedEnemies(bool destroyEnemies)
    {
        _isCancelling = true;
        _releaseBuffer.Clear();
        _releaseBuffer.AddRange(_aliveEnemies);

        foreach (WaveEnemyHandle enemy in _releaseBuffer)
        {
            if (enemy == null)
            {
                continue;
            }

            enemy.Released -= HandleEnemyReleased;

            if (destroyEnemies)
            {
                Destroy(enemy.gameObject);
            }
        }

        _aliveEnemies.Clear();
        _releaseBuffer.Clear();
        _isCancelling = false;
    }

    private Transform GetNextSpawnPoint()
    {
        int count = _spawnPoints.Count;
        int nextIndex;

        if (_prevSpawnPoint < 0 || count == 1)
        {
            nextIndex = UnityEngine.Random.Range(0, count);
        }
        else
        {
            nextIndex = UnityEngine.Random.Range(0, count - 1);

            if (nextIndex >= _prevSpawnPoint)
            {
                nextIndex++;
            }
        }

        _prevSpawnPoint = nextIndex;
        return _spawnPoints[nextIndex];
    }
}