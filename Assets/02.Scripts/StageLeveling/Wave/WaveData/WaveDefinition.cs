using System;
using UnityEngine;

[Serializable]
public class WaveDefinition //TODO: 구조체 고려
{
    [Header("Base Enemies")]
    [SerializeField] private Vector2Int _baseCountRange = new Vector2Int(10, 15);

    [SerializeField, Range(0f, 1f)] private float _meleeRatio = 0.9f;

    [Header("Elite Bonus")]
    [SerializeField] private Vector2Int _eliteBonusRange = Vector2Int.zero;

    [Header("Spawn Timing")]
    [SerializeField, Min(1)] private int _batchSize = 5;
    [SerializeField, Min(0f)] private float _spawnInterval = 0.15f;
    [SerializeField, Min(0f)] private float _batchInterval = 1f;

    public Vector2Int BaseCountRange => _baseCountRange;
    public float MeleeRatio => _meleeRatio;
    public Vector2Int EliteBonusRange => _eliteBonusRange;
    public int BatchSize => _batchSize;
    public float SpawnInterval => _spawnInterval;
    public float BatchInterval => _batchInterval;
}