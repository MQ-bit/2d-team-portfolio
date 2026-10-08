using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

[CreateAssetMenu(fileName = "WavePlan", menuName = "Stage/Wave Plan")]
public sealed class WavePlanSO : ScriptableObject
{
    [SerializeField] private EnemyRoster _enemyRoster = new EnemyRoster();
    [SerializeField] private RoomWaveSet _normalRoom = new RoomWaveSet();
    [SerializeField] private RoomWaveSet _eliteRoom = new RoomWaveSet();
    [SerializeField, Min(0f)] private float _interWaveDelay = 1f;

    public bool TryResolve(RoomType roomType, int seed, out ResolvedWavePlan plan, out string error)
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

        Random random = new Random(seed);
        List<ResolvedWave> resolvedWaves = new List<ResolvedWave>();

        for (int waveIndex = 0; waveIndex < set.Waves.Count; waveIndex++)
        {
            WaveDefinition definition = set.Waves[waveIndex];

            if (!TryResolveWave(definition, random, out ResolvedWave wave, out error))
            {
                error = $"Wave {waveIndex + 1}: {error}";
                return false;
            }

            resolvedWaves.Add(wave);
        }

        plan = new ResolvedWavePlan(resolvedWaves, _interWaveDelay);
        error = null;
        return true;
    }

    private bool TryResolveWave(WaveDefinition definition, Random random, out ResolvedWave wave, out string error)
    {
        wave = null;

        if (definition == null)
        {
            error = "WaveDefinition이 null입니다.";
            return false;
        }

        Vector2Int baseRange = NormalizeRange(definition.BaseCountRange);
        Vector2Int eliteRange = NormalizeRange(definition.EliteBonusRange);

        int baseCount = NextInclusive(random, baseRange);
        int eliteCount = NextInclusive(random, eliteRange);

        int meleeCount = Mathf.Clamp(Mathf.FloorToInt(baseCount * definition.BombRatio + 0.5f), 0, baseCount);

        int rangedCount = baseCount - meleeCount;

        int eliteMeleeCount = eliteCount / 2;
        int eliteRangedCount = eliteCount / 2;

        if (eliteCount % 2 != 0)
        {
            if (random.Next(0, 2) == 0)
            {
                eliteMeleeCount++;
            }
            else
            {
                eliteRangedCount++;
            }
        }

        List<EnemySpawnRequest> requests = new List<EnemySpawnRequest>(baseCount + eliteCount);

        if (!TryAddRequests(requests, EnemyKind.Bomber, meleeCount, out error) ||
            !TryAddRequests(requests, EnemyKind.Ranged, rangedCount, out error) ||
            !TryAddRequests(requests, EnemyKind.EliteBomber, eliteMeleeCount, out error) ||
            !TryAddRequests(requests, EnemyKind.EliteRanged, eliteRangedCount, out error))
        {
            return false;
        }

        Shuffle(requests, random);

        wave = new ResolvedWave(requests, Mathf.Max(1, definition.BatchSize), Mathf.Max(0f, definition.SpawnInterval), Mathf.Max(0f, definition.BatchInterval));

        error = null;
        return true;
    }

    private bool TryAddRequests(List<EnemySpawnRequest> destination, EnemyKind kind, int count, out string error)
    {
        if (count <= 0)
        {
            error = null;
            return true;
        }

        WaveEnemyHandle prefab = _enemyRoster.GetPrefab(kind);

        if (prefab == null)
        {
            error = $"{kind} 적 프리팹이 없습니다.";
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            destination.Add(new EnemySpawnRequest(kind, prefab));
        }

        error = null;
        return true;
    }

    private static Vector2Int NormalizeRange(Vector2Int range)
    {
        int min = Mathf.Max(0, Mathf.Min(range.x, range.y));
        int max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return new Vector2Int(min, max);
    }

    private static int NextInclusive(Random random, Vector2Int range)
    {
        return random.Next(range.x, range.y + 1);
    }

    private static void Shuffle<T>(IList<T> values, Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int target = random.Next(i + 1);
            (values[i], values[target]) = (values[target], values[i]);
        }
    }
}