using UnityEngine;

[CreateAssetMenu(fileName = "ExplodingEnemyData", menuName = "Enemy/Exploding Enemy Data")]
public class ExplodingEnemyData : EnemyData
{
    [Header("Explosion")]
    [Tooltip("플레이어가 이 거리 안에 들어오면 멈추고 예열을 시작한다.")]
    [SerializeField, Min(0f)] private float _fuseTriggerRange = 1.2f;
    [SerializeField, Min(0f)] private float _fuseDuration = 1f;
    [SerializeField, Min(0f)] private float _explosionRadius = 2f;
    [SerializeField, Min(0f)] private float _explosionDamage = 30f;

    public float FuseTriggerRange => _fuseTriggerRange;
    public float FuseDuration => _fuseDuration;
    public float ExplosionRadius => _explosionRadius;
    public float ExplosionDamage => _explosionDamage;
}
