using System.Collections.Generic;
using UnityEngine;

public class ResolvedWave
{
    public IReadOnlyList<EnemySpawnRequest> Requests { get; }
    public int BatchSize { get; }
    public float SpawnInterval { get; }
    public float BatchInterval { get; }

    public ResolvedWave(IReadOnlyList<EnemySpawnRequest> requests, int batchSize, float spawnInterval, float batchInterval)
    {
        Requests = requests;
        BatchSize = Mathf.Max(1, batchSize);
        SpawnInterval = Mathf.Max(0f, spawnInterval);
        BatchInterval = Mathf.Max(0f, batchInterval);
    }
}