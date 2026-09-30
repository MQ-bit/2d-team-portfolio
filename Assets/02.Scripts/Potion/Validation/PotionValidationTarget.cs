#if UNITY_EDITOR
using System;
using UnityEngine;

// Temporary scene target for the batch integration harness; never included in builds.
public sealed class PotionValidationTarget : MonoBehaviour, IDamageable, IThirstable
{
    public float CurrentHealth { get; private set; } = 1000;
    public bool IsDead => CurrentHealth <= 0;
    public float Damage, Healing, Thirst = 50;
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action<float, float> OnThirstChanged;
    public void DecreaseHealth(float amount)
    {
        float actual = Mathf.Min(CurrentHealth, amount);
        Damage += actual;
        CurrentHealth -= actual;
        OnHealthChanged?.Invoke(CurrentHealth, 2000);
        if (IsDead) OnDeath?.Invoke();
    }
    public void RestoreHealth(float amount)
    {
        Healing += amount;
        CurrentHealth += amount;
        OnHealthChanged?.Invoke(CurrentHealth, 2000);
    }
    public void RestoreThirst(float amount) { Thirst = Mathf.Clamp(Thirst + amount, 0, 100); OnThirstChanged?.Invoke(Thirst, 100); }
    public void DecreaseThirst(float amount) => RestoreThirst(-amount);
}
#endif
