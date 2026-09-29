using UnityEngine;
using System;
[Serializable]
public class EntityThirst
{
    private const float MinInterval = 0.01f;
    [SerializeField, Min(1f)] private float _maxThirst = 100f;
    [SerializeField, Min(0f)] private float _currentThirst = 100f;
    [SerializeField, Min(0f)] private float _increaseAmountPerTick = 1f;
    [SerializeField, Min(MinInterval)] private float _increaseInterval = 10f;

    private float _elapsedTime;

    public float CurrentThirst => _currentThirst;
    public float MaxThirst => _maxThirst;

    public EntityThirst(float startThirst = 100f, float maxThirst = 100f, float increaseAmountPerTick = 1f, float increaseInterval = 10f)
    {
        _maxThirst = Mathf.Max(1f, maxThirst);
        _currentThirst = Mathf.Clamp(startThirst, 0f, _maxThirst);
        _increaseAmountPerTick = Mathf.Max(0f, increaseAmountPerTick);
        _increaseInterval = Mathf.Max(MinInterval, increaseInterval);
        _elapsedTime = 0f;
    }

    public bool IncreaseTick(float deltaTime)
    {
        if (_currentThirst <= 0f)
        {
            return false;
        }

        _elapsedTime += deltaTime;

        if (_elapsedTime <= _increaseInterval)
        {
            return false;
        }


        RestoreThirst(_increaseAmountPerTick);
        _elapsedTime = 0f;

        return true;
    }

    public void DecreaseThirst(float amount)
    {
        _currentThirst = Mathf.Clamp(_currentThirst - Mathf.Max(0f, amount), 0f, _maxThirst);
    }

    public void RestoreThirst(float amount)
    {
        _currentThirst = Mathf.Clamp(_currentThirst + Mathf.Max(0f, amount), 0f, _maxThirst);
    }
}