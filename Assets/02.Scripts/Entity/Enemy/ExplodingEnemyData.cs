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

    [Header("Rush")]
    [Tooltip("플레이어가 이 거리 안에 들어오면 이동 속도가 RushSpeedMultiplier배가 된다.")]
    [SerializeField, Min(0f)] private float _rushTriggerRange = 4f;
    [Tooltip("돌진 중 이동 속도 배율. 1이면 돌진하지 않는다.")]
    [SerializeField, Min(1f)] private float _rushSpeedMultiplier = 1f;

    [Tooltip("돌진을 시작하기 전에 멈춰 있는 시간(초).")]
    [SerializeField, Min(0f)] private float _rushWindupDuration = 0.5f;
    [Tooltip("멈춰 있는 동안 몸에 입히는 색.")]
    [SerializeField] private Color _rushWindupColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("Split (엘리트)")]
    [Tooltip("비어 있으면 그냥 폭발한다. 지정하면 예열이 끝났을 때 폭발하는 대신 이 프리팹을 SplitCount개 만들어 대신 터뜨린다.")]
    [SerializeField] private GameObject _splitPrefab;
    [SerializeField, Min(1)] private int _splitCount = 2;
    [Tooltip("분열체의 폭발 피해 배율. 0.6이면 분열체는 ExplosionDamage의 60%만 준다.")]
    [SerializeField, Min(0f)] private float _splitDamageMultiplier = 0.6f;
    [Tooltip("분열한 적이 원래 자리에서 퍼져 나가는 거리.")]
    [SerializeField, Min(0f)] private float _splitSpawnDistance = 0.6f;

    public float FuseTriggerRange => _fuseTriggerRange;
    public float FuseDuration => _fuseDuration;
    public float ExplosionRadius => _explosionRadius;
    public float ExplosionDamage => _explosionDamage;
    public float RushTriggerRange => _rushTriggerRange;
    public float RushSpeedMultiplier => _rushSpeedMultiplier;
    public float RushWindupDuration => _rushWindupDuration;
    public Color RushWindupColor => _rushWindupColor;
    public GameObject SplitPrefab => _splitPrefab;
    public int SplitCount => _splitCount;
    public float SplitDamageMultiplier => _splitDamageMultiplier;
    public float SplitSpawnDistance => _splitSpawnDistance;
}
