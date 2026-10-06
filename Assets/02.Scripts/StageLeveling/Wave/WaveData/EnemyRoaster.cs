using System;
using UnityEngine;

[Serializable]
public class EnemyRoster
{
    [SerializeField] private WaveEnemyHandle _melee;
    [SerializeField] private WaveEnemyHandle _ranged;
    [SerializeField] private WaveEnemyHandle _eliteMelee;
    [SerializeField] private WaveEnemyHandle _eliteRanged;

    public WaveEnemyHandle GetPrefab(EnemyKind kind)
    {
        return kind switch
        {
            EnemyKind.Melee => _melee,
            EnemyKind.Ranged => _ranged,
            EnemyKind.EliteMelee => _eliteMelee,
            EnemyKind.EliteRanged => _eliteRanged,
            _ => null
        };
    }
}