using System.Collections.Generic;
using UnityEngine;

public enum PotionCombination
{
    None = 0,
    [InspectorName("01. 흡혈의 화염")] VampiricFire = 1,
    [InspectorName("02. 흡혈의 독")] VampiricPoison = 2,
    [InspectorName("03. 흡혈의 서리")] VampiricFrost = 3,
    [InspectorName("04. 흡혈의 전류")] VampiricLightning = 4,
    [InspectorName("05. 정화의 생명")] PurifyingLife = 5,
    [InspectorName("06. 맹독화염")] ToxicFlame = 6,
    [InspectorName("07. 열충격")] ThermalShock = 7,
    [InspectorName("08. 과충전")] BlazingCurrent = 8,
    [InspectorName("09. 화염 폭발")] FireExplosion = 9,
    [InspectorName("10. 맹독 서리")] FrostPoison = 10,
    [InspectorName("11. 신경독")] ToxicCurrent = 11,
    [InspectorName("12. 독성 증기")] ToxicSteam = 12,
    [InspectorName("13. 빙전")] FrozenCurrent = 13,
    [InspectorName("14. 빙결 폭발")] FrostExplosion = 14,
    [InspectorName("15. 방전")] Discharge = 15
}

[CreateAssetMenu(fileName = "PotionCombinationData", menuName = "Potion/Potion Combination Data")]
public sealed class PotionCombinationData : ScriptableObject, IPotionEffectData
{
    [Header("Identity")]
    [SerializeField] private PotionCombination _combination;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;

    [Header("Input")]
    [SerializeField] private PotionElement _elementA;
    [SerializeField] private PotionElement _elementB;
    [Tooltip("흡혈 조합용. 지정 원소의 투척/잔류/서리 설정을 재사용하고 이 조합의 흡혈 비율을 적용한다. 섭취는 상속하지 않는다.")]
    [SerializeField] private PotionElement _inheritThrownEffectsFrom = PotionElement.None;

    [Header("Thrown Effect")]
    [Tooltip("조합 효과를 적용할 대상")]
    [SerializeField] private PotionTargetType _targetType = PotionTargetType.Enemy;
    [SerializeField, Min(0f)] private float _effectRadius = 2f;
    [SerializeField] private bool _createsFloor = true;
    [SerializeField, Min(0f)] private float _floorDuration = 4f;
    [SerializeField, Min(0f)] private float _tickInterval = 1f;
    [SerializeField] private bool _applyOnEnter = true;
    [Tooltip("실제 투척 및 잔류 피해에 대한 체력 흡혈 비율")]
    [SerializeField, Range(0f, 1f)] private float _lifeStealRatio;

    [Header("Effects By Phase")]
    [SerializeField] private List<PotionEffectDefinition> _effects = new();
    [SerializeField] private PotionFrostSettings _frost = new();

    [Header("Visual")]
    [SerializeField] private PotionVisualData _visualData = new(null, Color.white, null);

    public PotionCombination Combination => _combination;
    public string Name => string.IsNullOrWhiteSpace(_displayName) ? _combination.ToString() : _displayName;
    public string Description => _description;
    public PotionElement ElementA => _elementA;
    public PotionElement ElementB => _elementB;
    public PotionElement InheritThrownEffectsFrom => _inheritThrownEffectsFrom;
    public PotionVisualData VisualData => _visualData;
    public PotionTargetType TargetType => _targetType;
    public float EffectRadius => _effectRadius;
    public bool CreatesFloor => _createsFloor;
    public float FloorDuration => _floorDuration;
    public float TickInterval => _tickInterval;
    public bool ApplyOnEnter => _applyOnEnter;
    public float LifeStealRatio => _lifeStealRatio;
    public IReadOnlyList<PotionEffectDefinition> Effects => _effects;
    public PotionFrostSettings Frost => _frost;

    public bool IsMatch(PotionElement a, PotionElement b) =>
        (_elementA == a && _elementB == b) || (_elementA == b && _elementB == a);

    private void OnValidate()
    {
        _displayName = _displayName?.Trim();
        _description = _description?.Trim();
        _effectRadius = PotionDataValidation.NonNegative(_effectRadius);
        _floorDuration = PotionDataValidation.NonNegative(_floorDuration);
        _tickInterval = PotionDataValidation.NonNegative(_tickInterval);
        _lifeStealRatio = Mathf.Clamp01(PotionDataValidation.Finite(_lifeStealRatio));
        _effects ??= new List<PotionEffectDefinition>();
        foreach (PotionEffectDefinition effect in _effects) effect?.Validate();
        _frost ??= new PotionFrostSettings();
        _frost.Validate();
    }
}
