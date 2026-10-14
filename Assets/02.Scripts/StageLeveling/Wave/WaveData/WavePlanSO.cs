using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WavePlan", menuName = "Stage/Wave Plan")]
public sealed class WavePlanSO : ScriptableObject
{
    [Header("Monster Entry")]
    [SerializeField] private List<WaveEnemyHandle> _enemyPrefabs = new List<WaveEnemyHandle>();

    [Header("Rooms")]
    [SerializeField] private RoomWaveSet _normalRoom = new RoomWaveSet();
    [SerializeField] private RoomWaveSet _eliteRoom = new RoomWaveSet();

    [SerializeField, Min(0f)] private float _interWaveDelay = 1f;

    public IReadOnlyList<WaveEnemyHandle> Prefabs => _enemyPrefabs;

    public bool TryResolve(RoomType roomType, out ResolvedWavePlan plan, out string error)
    {
        plan = null;

        RoomWaveSet set = roomType switch
        {
            RoomType.Normal => _normalRoom,
            RoomType.Elite => _eliteRoom,
            _ => null
        };

        if (set == null || set.Waves == null || set.Waves.Count == 0)
        {
            error = $"{roomType}에 대응하는 Wave 데이터가 없습니다.";
            return false;
        }

        if (!TryBuildPrefabSet(out HashSet<WaveEnemyHandle> registeredPrefabs, out error))
        {
            return false;
        }

        List<ResolvedWave> waves = new List<ResolvedWave>(set.Waves.Count);

        for (int i = 0; i < set.Waves.Count; i++)
        {
            if (!TryResolveWave(set.Waves[i], registeredPrefabs, out ResolvedWave wave, out error))
            {
                error = $"{roomType}, Wave {i + 1}: {error}";
                return false;
            }

            waves.Add(wave);
        }

        plan = new ResolvedWavePlan(waves, _interWaveDelay);
        error = null;
        return true;
    }

    private bool TryBuildPrefabSet(out HashSet<WaveEnemyHandle> registeredPrefabs, out string error)
    {
        registeredPrefabs = new HashSet<WaveEnemyHandle>();

        if (_enemyPrefabs == null)
        {
            error = "Enemy Prefabs 목록이 없습니다.";
            return false;
        }

        for (int i = 0; i < _enemyPrefabs.Count; i++)
        {
            WaveEnemyHandle prefab = _enemyPrefabs[i];

            if (prefab == null)
            {
                error = $"Enemy Prefabs 항목 {i + 1}: 프리팹이 없습니다.";
                return false;
            }

            if (!registeredPrefabs.Add(prefab))
            {
                error = $"Enemy Prefabs 항목 {i + 1}: {prefab.name}이 중복 등록되었습니다.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryValidateWave(WaveDefinition definition, HashSet<WaveEnemyHandle> registeredPrefabs, out string error)
    {
        if (definition == null)
        {
            error = "WaveDefinition이 null입니다.";
            return false;
        }

        if (definition.TotalCountMin < 0 || definition.TotalCountMax < definition.TotalCountMin)
        {
            error = "Total Count는 0 <= Min <= Max여야 합니다.";
            return false;
        }

        if (definition.EliteCount < 0 || definition.EliteCount > definition.TotalCountMin)
        {
            error = "Elite Count는 0 이상, Total Count Min 이하여야 합니다.";
            return false;
        }

        if (definition.NormalEnemies == null || definition.ElitePrefabs == null)
        {
            error = "Normal Enemies 또는 Elite Prefabs 목록이 없습니다.";
            return false;
        }

        HashSet<WaveEnemyHandle> usedPrefabs = new HashSet<WaveEnemyHandle>();

        double totalNormalWeight = 0d;

        for (int i = 0; i < definition.NormalEnemies.Count; i++)
        {
            WaveEnemyEntry entry = definition.NormalEnemies[i];
            string label = $"Normal Enemies 항목 {i + 1}";

            if (!TryValidatePrefab(entry.Prefab, label, registeredPrefabs, usedPrefabs, out error))
            {
                return false;
            }

            float weight = entry.SpawnWeight;

            if (weight < 0f || float.IsNaN(weight) || float.IsInfinity(weight))
            {
                error =
                    $"{label} ({entry.Prefab.name}): Spawn Weight는 유한한 0 이상의 값이어야 합니다.";
                return false;
            }

            totalNormalWeight += weight;
        }

        bool needsNormalEnemies = definition.TotalCountMax > definition.EliteCount;

        if (needsNormalEnemies && totalNormalWeight <= 0d)
        {
            error = "일반 몬스터가 필요한 웨이브에는 Spawn Weight가 양수인 일반 몬스터가 있어야 합니다.";
            return false;
        }

        if (definition.EliteCount > 0 && definition.ElitePrefabs.Count == 0)
        {
            error = "Elite Count가 양수인데 엘리트 후보가 없습니다.";
            return false;
        }

        for (int i = 0; i < definition.ElitePrefabs.Count; i++)
        {
            string label = $"Elite Prefabs 항목 {i + 1}";
            if (!TryValidatePrefab(definition.ElitePrefabs[i], label, registeredPrefabs, usedPrefabs, out error))
            {
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryValidatePrefab(WaveEnemyHandle prefab, string label, HashSet<WaveEnemyHandle> registeredPrefabs, HashSet<WaveEnemyHandle> usedPrefabs, out string error)
    {
        if (prefab == null)
        {
            error = $"{label}: 프리팹이 없습니다.";
            return false;
        }

        if (!registeredPrefabs.Contains(prefab))
        {
            error = $"{label} ({prefab.name}): Enemy Prefabs에 등록되지 않았습니다.";
            return false;
        }

        if (!usedPrefabs.Add(prefab))
        {
            error = $"{label} ({prefab.name}): 같은 웨이브의 일반·엘리트 목록에 중복 등록되었습니다.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryResolveWave(WaveDefinition definition, HashSet<WaveEnemyHandle> registeredPrefabs, out ResolvedWave wave, out string error)
    {
        wave = null;

        if (!TryValidateWave(definition, registeredPrefabs, out error))
        {
            return false;
        }

        int totalCount = NextInclusive(definition.TotalCountMin, definition.TotalCountMax);

        int normalCount = totalCount - definition.EliteCount;

        if (!TryAllocateNormalCounts(definition.NormalEnemies, normalCount, out int[] normalCounts, out error))
        {
            return false;
        }

        List<EnemySpawnRequest> requests = new List<EnemySpawnRequest>(totalCount);

        for (int i = 0; i < definition.NormalEnemies.Count; i++)
        {
            WaveEnemyHandle prefab = definition.NormalEnemies[i].Prefab;

            for (int count = 0; count < normalCounts[i]; count++)
            {
                requests.Add(new EnemySpawnRequest(prefab));
            }
        }

        for (int i = 0; i < definition.EliteCount; i++)
        {
            int selected = UnityEngine.Random.Range(0, definition.ElitePrefabs.Count);

            requests.Add(new EnemySpawnRequest(definition.ElitePrefabs[selected]));
        }

        Shuffle(requests);

        wave = new ResolvedWave(requests, definition.BatchSize, definition.SpawnInterval, definition.BatchInterval);

        error = null;
        return true;
    }

    private static bool TryAllocateNormalCounts(IReadOnlyList<WaveEnemyEntry> entries, int normalCount, out int[] counts, out string error)
    {
        counts = new int[entries.Count];

        if (normalCount == 0)
        {
            error = null;
            return true;
        }

        double totalWeight = 0d;

        for (int i = 0; i < entries.Count; i++)
        {
            totalWeight += entries[i].SpawnWeight;
        }

        if (totalWeight <= 0d)
        {
            error = "일반 몬스터 가중치 합이 0입니다.";
            return false;
        }

        double[] fractions = new double[entries.Count];
        List<int> candidates = new List<int>();
        int remaining = normalCount;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].SpawnWeight <= 0f)
            {
                continue;
            }

            double exactCount = normalCount * (entries[i].SpawnWeight / totalWeight);

            int floorCount = (int)Math.Floor(exactCount);

            counts[i] = floorCount;
            fractions[i] = exactCount - floorCount;
            remaining -= floorCount;

            candidates.Add(i);
        }

        if (remaining < 0 || remaining > candidates.Count)
        {
            error = "일반 몬스터의 가중치 정수 배분에 실패했습니다.";
            return false;
        }

        if (remaining == 0)
        {
            error = null;
            return true;
        }

        Shuffle(candidates);

        int[] tieRanks = new int[entries.Count];

        for (int i = 0; i < candidates.Count; i++)
        {
            tieRanks[candidates[i]] = i;
        }

        candidates.Sort((left, right) =>
        {
            int comparison = fractions[right].CompareTo(fractions[left]);
            return comparison != 0 ? comparison : tieRanks[left].CompareTo(tieRanks[right]);
        });

        for (int i = 0; i < remaining; i++)
        {
            counts[candidates[i]]++;
        }

        error = null;
        return true;
    }

    private static int NextInclusive(int min, int max)
    {
        // 호출 전 검증된 0 <= min <= max 범위에서 최댓값도 포함한다.
        // int.MaxValue에 1을 더할 때 발생하는 오버플로를 피한다.
        return max == int.MaxValue
            ? UnityEngine.Random.Range(min - 1, max) + 1
            : UnityEngine.Random.Range(min, max + 1);
    }

    private static void Shuffle<T>(IList<T> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int target = UnityEngine.Random.Range(0, i + 1);
            (values[i], values[target]) = (values[target], values[i]);
        }
    }
}
