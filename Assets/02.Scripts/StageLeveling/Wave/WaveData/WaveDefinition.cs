using System;
using UnityEngine;

[Serializable]
public class WaveDefinition //TODO: 구조체 고려
{
    [Header("Base Enemies")]
    [SerializeField] private int _baseCountMin = 10;
    [SerializeField] private int _baseCountMax = 15;

    [SerializeField, Range(0f, 1f)] private float _bombRatio = 0.9f;

    [Header("Elite Bonus")]
    [SerializeField] private int _eliteBonusMin = 1;
    [SerializeField] private int _eliteBonusMax = 2;


    [Header("Spawn Timing")]
    [SerializeField, Min(1)] private int _batchSize = 5;
    [SerializeField, Min(0f)] private float _spawnInterval = 0.15f;
    [SerializeField, Min(0f)] private float _batchInterval = 1f;

    public Vector2Int BaseCountRange => new Vector2Int(_baseCountMin, _baseCountMax);
    public float BombRatio => _bombRatio;
    public Vector2Int EliteBonusRange => new Vector2Int(_eliteBonusMin, _eliteBonusMax);
    public int BatchSize => _batchSize;
    public float SpawnInterval => _spawnInterval;
    public float BatchInterval => _batchInterval;
}