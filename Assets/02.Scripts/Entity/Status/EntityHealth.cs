using System;
using UnityEngine;

[Serializable]
public class EntityHealth
{
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [SerializeField, Min(0f)] private float _currentHealth = 100f;

    public float Current => _currentHealth;
    public float Max => _maxHealth;
    public bool IsDead => _currentHealth <= 0f;

    public EntityHealth(float current, float max)
    {
        _maxHealth = Mathf.Max(1f, max);
        _currentHealth = Mathf.Clamp(current, 0f, _maxHealth);
    }

    public void RestoreHealth(float amount)
    {
        SetCurrent(_currentHealth + Mathf.Max(0f, amount));
    }

    public void DecreaseHealth(float amount)
    {
        SetCurrent(_currentHealth - Mathf.Max(0f, amount));
    }

    private void SetCurrent(float value)
    {
        _currentHealth = Mathf.Clamp(value, 0f, _maxHealth);
    }
}
