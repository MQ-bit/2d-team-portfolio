using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct WaveEnemyEntry
{
    [SerializeField] private WaveEnemyHandle _prefab;

    [FormerlySerializedAs("_weight")]
    [SerializeField, Min(0f)] private float _spawnWeight;

    public WaveEnemyHandle Prefab => _prefab;
    public float SpawnWeight => _spawnWeight;

    public WaveEnemyEntry(WaveEnemyHandle enemyPrefab, float spawnWeight)
    {
        _prefab = enemyPrefab;
        _spawnWeight = spawnWeight;
    }
}