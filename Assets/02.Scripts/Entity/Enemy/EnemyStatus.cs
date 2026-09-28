using System;
using UnityEngine;

// PlayerStatus와 같은 역할: 체력(EntityHealth)을 소유하고 IDamageable을 구현하는 상태 허브.
// 최대 체력 수치 자체는 EnemyData가 갖고 있고, 컨트롤러가 Awake에서 Initialize()로 넘겨준다.
public class EnemyStatus : MonoBehaviour, IDamageable
{
    private EntityHealth _health;

    public bool IsDead => _health.IsDead;
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    public void Initialize(float maxHealth)
    {
        _health = new EntityHealth(maxHealth, maxHealth);
    }

    public void RestoreHealth(float amount)
    {
        _health.RestoreHealth(amount);
        OnHealthChanged?.Invoke(_health.Current, _health.Max);
    }

    public void DecreaseHealth(float amount)
    {
        if (IsDead)
        {
            return;
        }

        _health.DecreaseHealth(amount);
        OnHealthChanged?.Invoke(_health.Current, _health.Max);

        if (_health.IsDead)
        {
            OnDeath?.Invoke();
        }
    }
}
