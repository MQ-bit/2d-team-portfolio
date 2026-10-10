using UnityEngine;

// 근접/원거리 적이 공통으로 쓰는 수치. 체력은 EnemyStatus(EntityHealth)가 관리한다.
public abstract class EnemyData : ScriptableObject
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float _maxHealth = 100f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveSpeed = 2.5f;

    [Header("Contact Damage")]
    [SerializeField, Min(0f)] private float _contactDamage = 10f;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float _detectRange = 6f;

    public float MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public float ContactDamage => _contactDamage;
    public float DetectRange => _detectRange;
}
