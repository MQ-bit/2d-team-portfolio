using System;
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

public enum PotionCombinationEffectKind
{
    Damage = 0,
    PeriodicDamage = 1,
    AreaDamage = 2,
    RestorePlayerHealth = 3,
    ApplyStatus = 4,
    Stagger = 5,
    RemoveGroundAreas = 6
}

[Serializable]
public sealed class PotionCombinationEffect
{
    [Header("Effect Type")]
    [SerializeField] private PotionCombinationEffectKind _kind;

    [Header("Element")]
    [Tooltip("피해 속성. 피해 효과가 아닌 경우 None")]
    [SerializeField] private PotionElement _element;

    [Header("Damage")]
    [Tooltip("고정 피해량 대신 조합에 사용된 해당 속성 포션의 피해량을 사용할지 여부")]
    [UnityEngine.Serialization.FormerlySerializedAs("_useSourcePotionDamage")]
    [SerializeField] private bool _useSourceElementDamage;
    [Tooltip("고정 피해량 또는 플레이어 회복량")]
    [SerializeField, Min(0f)] private float _amount;
    [Tooltip("실제로 입힌 피해량 중 플레이어 HP로 회복되는 비율")]
    [SerializeField, Range(0f, 1f)] private float _lifeStealRatio;
    [Header("Status")]
    [SerializeField] private PotionStatusEffect _status;
    [Tooltip("상태이상, 경직 또는 지속 피해의 유지 시간")]
    [SerializeField, Min(0f)] private float _duration;
    [Tooltip("지속 피해 적용 간격")]
    [SerializeField, Min(0f)] private float _tickInterval;

    public PotionCombinationEffectKind Kind => _kind;
    public PotionElement Element => _element;

    public bool UseSourceElementDamage => _useSourceElementDamage;
    public float Amount => _amount;
    public float LifeStealRatio => _lifeStealRatio;

    public PotionStatusEffect Status => _status;
    public float Duration => _duration;
    public float TickInterval => _tickInterval;
}

[CreateAssetMenu(fileName = "PotionCombinationData", menuName = "Potion/Potion Combination Data")]
public sealed class PotionCombinationData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private PotionCombination _combination;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;

    [Header("New Effect Data (runtime connection in later steps)")]
    [Tooltip("새 기획 설정을 작성한 에셋 표시. 기존 효과 목록과 API는 변경하지 않는다.")]
    [SerializeField] private bool _hasEffectProfile;
    [SerializeField] private PotionEffectProfile _effectProfile = new();
    [Tooltip("흡혈 조합용. None이면 자체 설정 사용. 지정 원소의 투척/잔류/서리 설정을 재사용하고 이 조합의 LifeStealRatio만 추가한다. 섭취는 상속하지 않는다.")]
    [SerializeField] private PotionElement _inheritThrownEffectsFrom = PotionElement.None;

    [Header("Input")]
    [SerializeField] private PotionElement _elementA;
    [SerializeField] private PotionElement _elementB;

    [Header("Combination Area")]
    [Tooltip("조합 타원 장판이 유지되는 시간")]
    [SerializeField, Min(0f)] private float _combinationDuration = 4f;
    [Tooltip("즉발 효과만 적용하고 조합 장판을 거의 즉시 제거할지 여부")]
    [SerializeField] private bool _instantOnly;
    [Tooltip("조합 장판의 시각 데이터")]
    [SerializeField] private PotionVisualData _visualData = new(null, Color.white, null);

    [Header("Effects")]
    [Tooltip("조합 장판 범위 안에 적용할 효과. 위에서부터 순서대로 적용")]
    [SerializeField] private List<PotionCombinationEffect> _effects = new();

    public PotionCombination Combination => _combination;
    public string Name => string.IsNullOrWhiteSpace(_displayName) ? _combination.ToString() : _displayName;
    public string Description => _description;
    public PotionElement ElementA => _elementA;
    public PotionElement ElementB => _elementB;

    public float CombinationDuration => _combinationDuration;
    public bool InstantOnly => _instantOnly;
    public PotionVisualData VisualData => _visualData;

    public IReadOnlyList<PotionCombinationEffect> Effects => _effects;

    public bool HasEffectProfile => _hasEffectProfile && _effectProfile != null;
    public PotionEffectProfile EffectProfile => _effectProfile;
    public PotionElement InheritThrownEffectsFrom => _inheritThrownEffectsFrom;

    // 외부 레거시 조합 에셋을 수정하지 않고 Potion 내부 기준 프로필을 사용한다.
    private PotionCombinationData _defaultRuntimeData;
    public PotionEffectProfile RuntimeProfile
    {
        get
        {
            if (HasEffectProfile) return _effectProfile;
            LoadDefaultRuntimeData();
            return _defaultRuntimeData != null && _defaultRuntimeData.HasEffectProfile
                ? _defaultRuntimeData.EffectProfile : null;
        }
    }

    public float RuntimeCombinationDuration
    {
        get
        {
            if (HasEffectProfile) return _combinationDuration;
            LoadDefaultRuntimeData();
            return _defaultRuntimeData != null && _defaultRuntimeData.HasEffectProfile
                ? _defaultRuntimeData.CombinationDuration : _combinationDuration;
        }
    }

    public PotionElement RuntimeInheritThrownEffectsFrom
    {
        get
        {
            if (HasEffectProfile) return _inheritThrownEffectsFrom;
            LoadDefaultRuntimeData();
            return _defaultRuntimeData != null && _defaultRuntimeData.HasEffectProfile
                ? _defaultRuntimeData.InheritThrownEffectsFrom : _inheritThrownEffectsFrom;
        }
    }

    public bool IsMatch(PotionElement a, PotionElement b)
    {
        return
            (_elementA == a && _elementB == b) ||
            (_elementA == b && _elementB == a);
    }

    public bool TryGetEffect(PotionCombinationEffectKind kind, out PotionCombinationEffect result)
    {
        foreach (PotionCombinationEffect effect in _effects)
        {
            if (effect == null)
                continue;

            if (effect.Kind != kind)
                continue;

            result = effect;
            return true;
        }

        result = null;
        return false;
    }

    private void OnValidate()
    {
        _defaultRuntimeData = null;
        _displayName = _displayName?.Trim();
        _description = _description?.Trim();

        _combinationDuration =
            SanitizeNonNegative(_combinationDuration);
        _effectProfile ??= new PotionEffectProfile();
        _effectProfile.Validate();
    }

    private void LoadDefaultRuntimeData()
    {
        if (_defaultRuntimeData == null && _combination != PotionCombination.None)
        {
            _defaultRuntimeData = Resources.Load<PotionCombinationData>(
                "CombinationDefaults/" + _combination);
        }
    }

    private static float SanitizeNonNegative(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return 0f;

        return Mathf.Max(0f, value);
    }
}
