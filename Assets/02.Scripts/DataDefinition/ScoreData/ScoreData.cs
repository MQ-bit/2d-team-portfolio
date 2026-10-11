using System;
using UnityEngine;

[Serializable]
public class ScoreData
{
    [SerializeField] private int _clearedFloorCount;
    [SerializeField] private int _consumedPotionCount;

    [SerializeField] private int _normalBomberKillCount;
    [SerializeField] private int _eliteBomberKillCount;
    [SerializeField] private int _normalRangedKillCount;
    [SerializeField] private int _eliteRangedKillCount;
    [SerializeField] private int _reaperKillCount;

    public int ClearedFloorCount => _clearedFloorCount;
    public int ConsumedPotionCount => _consumedPotionCount;

    public int NormalKillCount => _normalBomberKillCount + _normalRangedKillCount + _reaperKillCount;
    public int EliteKillCount => _eliteBomberKillCount + _eliteRangedKillCount;
    public int TotalKillCount => NormalKillCount + EliteKillCount;

    public int GetKillCount(EnemyKind kind)
    {
        return kind switch
        {
            EnemyKind.Bomber => _normalBomberKillCount,
            EnemyKind.EliteBomber => _eliteBomberKillCount,
            EnemyKind.Ranged => _normalRangedKillCount,
            EnemyKind.EliteRanged => _eliteRangedKillCount,
            EnemyKind.Reaper => _reaperKillCount,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public ScoreData Clone()
    {
        return new ScoreData
        {
            _clearedFloorCount = _clearedFloorCount,
            _consumedPotionCount = _consumedPotionCount,
            _normalBomberKillCount = _normalBomberKillCount,
            _eliteBomberKillCount = _eliteBomberKillCount,
            _normalRangedKillCount = _normalRangedKillCount,
            _eliteRangedKillCount = _eliteRangedKillCount,
            _reaperKillCount = _reaperKillCount
        };
    }

    internal bool TryAddClearedFloor(int amount = 1)
    {
        if (amount <= 0)
        {
            return false;
        }

        _clearedFloorCount += amount;
        return true;
    }

    internal bool TryAddConsumedPotion(int amount = 1)
    {
        if (amount <= 0)
        {
            return false;
        }

        _consumedPotionCount += amount;
        return true;
    }

    internal bool TryAddEnemyKill(EnemyKind kind, int amount = 1)
    {
        if (amount <= 0)
        {
            return false;
        }

        switch (kind)
        {
            case EnemyKind.Bomber:
                _normalBomberKillCount += amount;
                break;

            case EnemyKind.EliteBomber:
                _eliteBomberKillCount += amount;
                break;

            case EnemyKind.Ranged:
                _normalRangedKillCount += amount;
                break;

            case EnemyKind.EliteRanged:
                _eliteRangedKillCount += amount;
                break;

            case EnemyKind.Reaper:
                _reaperKillCount += amount;
                break;

            default:
                return false;
        }

        return true;
    }
}
