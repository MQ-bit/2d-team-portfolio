using UnityEngine;

public interface IDamageable
{
    bool IsDead { get; }
    float CurrentHealth { get; }
    float MaxHealth { get; }
}
