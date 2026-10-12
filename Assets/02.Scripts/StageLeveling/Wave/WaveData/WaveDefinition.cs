using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveDefinition
{
    [Header("Total Enemies")]

    [Tooltip("엘리트를 포함한 웨이브 전체 최소 수량입니다.")]
    [SerializeField, Min(0)] private int _totalCountMin = 10;

    [Tooltip("엘리트를 포함한 웨이브 전체 최대 수량입니다.")]
    [SerializeField, Min(0)] private int _totalCountMax = 15;

    [Header("Normal Enemies")]
    [SerializeField]
    private List<WaveEnemyEntry> _normalEnemies = new List<WaveEnemyEntry>();

    [Header("Elite Enemies")]

    [Tooltip("이 웨이브에서 반드시 생성할 엘리트 수량입니다.")]
    [SerializeField, Min(0)] private int _eliteCount;

    [Tooltip("각 엘리트는 이 목록에서 동일 확률로 추첨됩니다.")]
    [SerializeField]
    private List<WaveEnemyHandle> _elitePrefabs = new List<WaveEnemyHandle>();

    [Header("Spawn Timing")]

    [SerializeField, Min(1)] private int _batchSize = 5;
    [SerializeField, Min(0f)] private float _spawnInterval = 0.15f;
    [SerializeField, Min(0f)] private float _batchInterval = 1f;

    public int TotalCountMin => _totalCountMin;
    public int TotalCountMax => _totalCountMax;

    public IReadOnlyList<WaveEnemyEntry> NormalEnemies => _normalEnemies;

    public int EliteCount => _eliteCount;
    public IReadOnlyList<WaveEnemyHandle> ElitePrefabs => _elitePrefabs;

    public int BatchSize => _batchSize;
    public float SpawnInterval => _spawnInterval;
    public float BatchInterval => _batchInterval;
}