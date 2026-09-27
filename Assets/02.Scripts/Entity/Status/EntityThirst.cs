using UnityEngine;
using System;

[System.Serializable]
public class EntityThirst
{
    [SerializeField, Min(1f)] private float _maxThirst = 100f;
    [SerializeField, Min(0f)] private float _currentThirst = 100f;

    public float CurrentThirst => _currentThirst;
    public float MaxThirst => _maxThirst;
    public float NormalizedThirst => _currentThirst / _maxThirst;

    public EntityThirst(float startThirst = 100f, float maxThirst = 100f)
    {
        _maxThirst = Mathf.Max(1f, maxThirst);
        _currentThirst = Mathf.Clamp(startThirst, 0f, _maxThirst);
    }

    public void RestoreThirst(float amount)
    {
        SetCurrent(_currentThirst + Mathf.Max(0f, amount));
    }

    public void DecreaseThirst(float amount)
    {
        SetCurrent(_currentThirst - Mathf.Max(0f, amount));
    }

    private void SetCurrent(float value)
    {
        float previousThirst = _currentThirst;
        _currentThirst = Mathf.Clamp(value, 0f, _maxThirst);
    }
}
