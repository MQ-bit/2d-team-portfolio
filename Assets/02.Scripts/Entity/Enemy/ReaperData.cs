using UnityEngine;

[CreateAssetMenu(fileName = "Reaper", menuName = "Enemy/Reaper Data")]
public sealed class ReaperData : EnemyData
{
    [Header("Abilities")]
    [SerializeField] private bool _useMeleeAttack = true;
    [SerializeField] private bool _useDashAttack = true;
    [SerializeField] private bool _usePotionSummon = true;
    [SerializeField] private bool _usePotionSteal = true;
    [Tooltip("0이면 기존 추적 방식. 원거리 타입은 이 거리 주변에서 공격을 기다린다.")]
    [SerializeField, Min(0f)] private float _preferredDistance;
    [SerializeField, Min(0.1f)] private float _distanceTolerance = 0.75f;

    [Header("Ability Colors")]
    [SerializeField] private Color _magicCircleTint = Color.white;
    [SerializeField] private Color _indicatorColor = new Color(0.56f, 0.16f, 0.38f, 1f);
    [SerializeField] private Color _indicatorRuneColor = new Color(0.65f, 0.39f, 0.85f, 1f);
    [SerializeField] private Color _indicatorChargedColor = new Color(0.94f, 0.8f, 0.68f, 1f);

    [Header("Melee")]
    [Tooltip("근접 공격을 시작하는 거리. 실제 타격 범위는 Melee Hitbox Size/Offset으로 조절한다.")]
    [SerializeField, Min(0f)] private float _meleeRange = 1.8f;
    [HideInInspector]
    [SerializeField, Range(1f, 360f)] private float _meleeAngle = 140f;
    [SerializeField, Min(0f)] private float _meleeDamage = 15f;
    [Tooltip("공격 애니메이션 시작부터 낫/베기 판정이 시작될 때까지의 시간.")]
    [SerializeField, Min(0f)] private float _meleeWindup = 0.5f;
    [SerializeField, Min(0.01f)] private float _meleeActiveDuration = 0.3f;
    [Tooltip("월드 단위의 낫/베기 판정 크기. Visual 크기를 바꾸면 함께 조절한다.")]
    [SerializeField] private Vector2 _meleeHitboxSize = new Vector2(3.3f, 2.8f);
    [Tooltip("리퍼 발 위치에서 판정 중심까지. X는 바라보는 좌우 방향, Y는 위쪽.")]
    [SerializeField] private Vector2 _meleeHitboxOffset = new Vector2(1.5f, 0.85f);
    [SerializeField, Min(0f)] private float _meleeRecovery = 0.2f;
    [SerializeField, Min(0.01f)] private float _meleeCooldown = 1.8f;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float _dashMinRange = 2f;
    [SerializeField, Min(0f)] private float _dashMaxRange = 6f;
    [SerializeField, Min(0f)] private float _dashWindup = 0.6f;
    [SerializeField, Min(0.01f)] private float _dashSpeed = 10f;
    [SerializeField, Min(0.01f)] private float _dashDistance = 4f;
    [SerializeField, Min(0f)] private float _dashDamage = 20f;
    [SerializeField, Min(0f)] private float _dashRecovery = 0.6f;
    [SerializeField, Min(0.01f)] private float _dashCooldown = 5f;

    [Header("Potion Summon")]
    [Tooltip("화염, 독, 빙결, 번개 SO를 할당한다. 생명/기화 및 장판을 만들지 않는 포션은 제외한다.")]
    [SerializeField] private PotionData[] _summonPotions = new PotionData[4];
    [Tooltip("플레이어가 이 거리 이상 떨어졌을 때만 포션 소환을 시작한다.")]
    [SerializeField, Min(0f)] private float _summonMinRange = 6f;
    [Tooltip("포션 소환이 가능한 최대 거리.")]
    [SerializeField, Min(0f)] private float _summonRange = 10f;
    [SerializeField, Min(0f)] private float _summonWindup = 0.8f;
    [SerializeField, Min(0f)] private float _summonRecovery = 0.6f;
    [SerializeField, Min(0.01f)] private float _summonCooldown = 8f;
    [SerializeField, Min(0f)] private float _warningDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float _fallDuration = 0.4f;
    [SerializeField, Min(0.1f)] private float _summonHeight = 2.5f;
    [Header("Potion Steal")]
    [SerializeField] private Color _stealIndicatorColor = new Color(0.18f, 0.05f, 0.28f, 1f);
    [SerializeField] private Color _stealRuneColor = new Color(0.6f, 0.25f, 0.85f, 1f);
    [SerializeField] private Color _stealAccentColor = new Color(0.25f, 0.85f, 0.8f, 1f);
    [SerializeField] private Color _stealChargedColor = new Color(0.85f, 0.7f, 1f, 1f);
    [SerializeField, Min(0f)] private float _stealMinRange = 3f;
    [SerializeField, Min(0f)] private float _stealRange = 6f;
    [Tooltip("강탈 준비 시작 시 고정되는 바닥 경고 범위. 벗어나면 강탈을 피한다.")]
    [SerializeField, Min(0.1f)] private float _stealAreaRadius = 2.6f;
    [SerializeField, Min(0.01f)] private float _stealWindup = 0.9f;
    [SerializeField, Min(0f)] private float _stealRecovery = 0.6f;
    [SerializeField, Min(0.01f)] private float _stealCooldown = 12f;
    [Tooltip("강탈 성공 후 포션이 플레이어에서 리퍼에게 이동하는 연출 시간.")]
    [SerializeField, Min(0.01f)] private float _stealCaptureDuration = 2.2f;
    [Tooltip("강탈 판정 순간에 플레이어가 가진 포션이 없으면 주는 피해.")]
    [SerializeField, Min(0f)] private float _stealEmptyDamage = 15f;
    [Header("Death")]
    [SerializeField, Min(0f)] private float _deathDuration = 1f;

    public bool UseMeleeAttack => _useMeleeAttack;
    public bool UseDashAttack => _useDashAttack;
    public bool UsePotionSummon => _usePotionSummon;
    public bool UsePotionSteal => _usePotionSteal;
    public float PreferredDistance => _preferredDistance;
    public float DistanceTolerance => _distanceTolerance;
    public Color MagicCircleTint => _magicCircleTint;
    public Color IndicatorColor => _indicatorColor;
    public Color IndicatorRuneColor => _indicatorRuneColor;
    public Color IndicatorChargedColor => _indicatorChargedColor;
    public float MeleeRange => _meleeRange;
    public float MeleeAngle => _meleeAngle;
    public float MeleeDamage => _meleeDamage;
    public float MeleeWindup => _meleeWindup;
    public float MeleeActiveDuration => _meleeActiveDuration;
    public Vector2 MeleeHitboxSize => _meleeHitboxSize;
    public Vector2 MeleeHitboxOffset => _meleeHitboxOffset;
    public float MeleeRecovery => _meleeRecovery;
    public float MeleeCooldown => _meleeCooldown;
    public float DashMinRange => _dashMinRange;
    public float DashMaxRange => _dashMaxRange;
    public float DashWindup => _dashWindup;
    public float DashSpeed => _dashSpeed;
    public float DashDistance => _dashDistance;
    public float DashDamage => _dashDamage;
    public float DashRecovery => _dashRecovery;
    public float DashCooldown => _dashCooldown;
    public float SummonRange => _summonRange;
    public float SummonMinRange => _summonMinRange;
    public float SummonWindup => _summonWindup;
    public float SummonRecovery => _summonRecovery;
    public float SummonCooldown => _summonCooldown;
    public float WarningDuration => _warningDuration;
    public float FallDuration => _fallDuration;
    public float SummonHeight => _summonHeight;
    public float DeathDuration => _deathDuration;
    public float StealMinRange => _stealMinRange;
    public Color StealIndicatorColor => _stealIndicatorColor;
    public Color StealRuneColor => _stealRuneColor;
    public Color StealAccentColor => _stealAccentColor;
    public Color StealChargedColor => _stealChargedColor;
    public float StealRange => _stealRange;
    public float StealAreaRadius => _stealAreaRadius;
    public float StealWindup => _stealWindup;
    public float StealRecovery => _stealRecovery;
    public float StealCooldown => _stealCooldown;
    public float StealCaptureDuration => _stealCaptureDuration;
    public float StealEmptyDamage => _stealEmptyDamage;

    public PotionData PickSummonPotion()
    {
        PotionData selected = null;
        int validCount = 0;
        if (_summonPotions == null) return null;
        foreach (PotionData potion in _summonPotions)
        {
            if (potion == null || !potion.CreatesFloor ||
                (potion.Element != PotionElement.Fire && potion.Element != PotionElement.Poison &&
                 potion.Element != PotionElement.Ice && potion.Element != PotionElement.Lightning))
            {
                continue;
            }
            // 유효한 항목만 같은 확률로 선택한다.
            if (UnityEngine.Random.Range(0, ++validCount) == 0) selected = potion;
        }
        return selected;
    }
}
