using UnityEngine;

[CreateAssetMenu(fileName = "RangedEnemyData", menuName = "Enemy/Ranged Enemy Data")]
public class RangedEnemyData : EnemyData
{
    [Header("Attack")]
    [Tooltip("플레이어가 이 거리 안에 들어오면 멈추고 공격한다.")]
    [SerializeField, Min(0f)] private float _attackRange = 4f;
    [Tooltip("공격 상태에서 추적으로 되돌아가기까지 AttackRange에 더해지는 여유 거리. " +
             "AttackRange와 같은 값이면 경계선에서 추적/공격이 매 프레임 반복되며 버벅거릴 수 있다.")]
    [SerializeField, Min(0f)] private float _chaseResumeBuffer = 0.5f;
    [SerializeField, Min(0f)] private float _attackCooldown = 1.5f;
    [Tooltip("공격을 시작한 뒤 투사체가 실제로 나가기까지의 시간. 공격 애니메이션에서 투사체가 나가는 프레임에 맞춘다.")]
    [SerializeField, Min(0f)] private float _attackFireDelay = 0.3f;

    [Header("Projectile")]
    [SerializeField, Min(0f)] private float _projectileSpeed = 5f;
    [SerializeField, Min(0f)] private float _projectileDamage = 10f;
    [SerializeField, Min(0f)] private float _projectileLifetime = 3f;

    [Tooltip("한 번 공격에 나가는 투사체 수. 2개 이상이면 플레이어 방향을 중심으로 부채꼴로 퍼진다.")]
    [SerializeField, Min(1)] private int _projectileCount = 1;
    [Tooltip("부채꼴 전체 각도(도). 첫 투사체와 마지막 투사체 사이의 각도다.")]
    [SerializeField, Range(0f, 180f)] private float _spreadAngle = 60f;

    public int ProjectileCount => _projectileCount;
    public float SpreadAngle => _spreadAngle;
    public float AttackRange => _attackRange;
    public float ChaseResumeRange => _attackRange + _chaseResumeBuffer;
    public float AttackCooldown => _attackCooldown;
    public float AttackFireDelay => _attackFireDelay;
    public float ProjectileSpeed => _projectileSpeed;
    public float ProjectileDamage => _projectileDamage;
    public float ProjectileLifetime => _projectileLifetime;
}
