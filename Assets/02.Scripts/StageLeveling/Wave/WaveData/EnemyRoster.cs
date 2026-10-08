using System;
using UnityEngine;

[Serializable]
public class EnemyRoster
{
    [SerializeField] private WaveEnemyHandle _bomber;
    [SerializeField] private WaveEnemyHandle _ranged;
    [SerializeField] private WaveEnemyHandle _eliteBomber;
    [SerializeField] private WaveEnemyHandle _eliteRanged;

    public WaveEnemyHandle GetPrefab(EnemyKind kind)
    {
        return kind switch
        {
            EnemyKind.Bomber => _bomber,
            EnemyKind.Ranged => _ranged,
            EnemyKind.EliteBomber => _eliteBomber,
            EnemyKind.EliteRanged => _eliteRanged,
            _ => null
        };
    }
}