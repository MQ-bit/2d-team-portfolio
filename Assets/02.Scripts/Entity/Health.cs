using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [SerializeField, Min(0f)] private float _currentHealth = 100f;

    public event Action<float, float> Changed;

    public float Current => _currentHealth;
    public float Max => _maxHealth;
    public bool IsDead => _currentHealth <= 0f;

    private void Awake()
    {
        _currentHealth = Mathf.Clamp(_currentHealth, 0f, _maxHealth);
    }

    public void Modify(float amount)
    {
        _currentHealth = Mathf.Clamp(_currentHealth + amount, 0f, _maxHealth);
        Changed?.Invoke(_currentHealth, _maxHealth);
    }
}
