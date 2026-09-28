using UnityEngine;
using System;
using System.Collections.Generic;

public enum PotionCombination
{
    None = 0,
    [InspectorName("01. 흡혈의 불꽃")] VampiricFire = 1,
    [InspectorName("02. 흡혈의 독")] VampiricPoison = 2,
    [InspectorName("03. 흡혈의 서리")] VampiricFrost = 3,
    [InspectorName("04. 흡혈의 전류")] VampiricLightning = 4,
    [InspectorName("05. 정화의 생명")] PurifyingLife = 5,
    [InspectorName("06. 맹독화염")] ToxicFlame = 6,
    [InspectorName("07. 열충격")] ThermalShock = 7,
    [InspectorName("08. 폭염전류")] BlazingCurrent = 8,
    [InspectorName("09. 화염 폭발")] FireExplosion = 9,
    [InspectorName("10. 동상독")] FrostPoison = 10,
    [InspectorName("11. 독성 전류")] ToxicCurrent = 11,
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
    [SerializeField] private PotionCombinationEffectKind _kind;
    [Tooltip("피해 속성 또는 제거할 장판의 속성")]
    [SerializeField] private PotionElement _element;
    [Tooltip("피해량을 해당 속성의 재료 포션에서 가져올지 여부")]
    [SerializeField] private bool _useSourcePotionDamage;
    [Tooltip("고정 피해량 또는 플레이어 회복량. 미지정 수치는 0으로 두고 설정 필요")]
    [SerializeField, Min(0f)] private float _amount;
    [Tooltip("실제로 입힌 피해 중 플레이어 HP로 회복할 비율")]
    [SerializeField, Range(0f, 1f)] private float _lifeStealRatio;
    [SerializeField] private PotionStatusEffect _status;
    [Tooltip("상태이상, 경직 또는 지속 피해의 유지 시간. 0은 미설정")]
    [SerializeField, Min(0f)] private float _duration;
    [Tooltip("지속 피해 간격. 0은 미설정")]
    [SerializeField, Min(0f)] private float _tickInterval;
    [Tooltip("범위 피해 또는 장판 제거 반경. 0은 미설정")]
    [SerializeField, Min(0f)] private float _radius;
    [SerializeField] private bool _removeAllGroundAreas;

    public PotionCombinationEffectKind Kind => _kind;
    public PotionElement Element => _element;
    public bool UseSourcePotionDamage => _useSourcePotionDamage;
    public float Amount => _amount;
    public float LifeStealRatio => _lifeStealRatio;
    public PotionStatusEffect Status => _status;
    public float Duration => _duration;
    public float TickInterval => _tickInterval;
    public float Radius => _radius;
    public bool RemoveAllGroundAreas => _removeAllGroundAreas;
}

[CreateAssetMenu(fileName = "PotionCombinationData", menuName = "Potion/Potion Combination Data")]
public sealed class PotionCombinationData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private PotionCombination _combination;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;

    [Header("Input")]
    [SerializeField] private PotionElement _elementA;
    [SerializeField] private PotionElement _elementB;

    [Header("Result")]
    [SerializeField] private PotionData _resultPotion;

    [Header("Combination Effects (목록 순서대로 적용)")]
    [SerializeField] private List<PotionCombinationEffect> _effects = new();

    public PotionCombination Combination => _combination;
    public string Name => _displayName;
    public string Description => _description;
    public PotionElement ElementA => _elementA;
    public PotionElement ElementB => _elementB;
    public PotionData ResultPotion => _resultPotion;
    public IReadOnlyList<PotionCombinationEffect> Effects => _effects;

    public bool IsMatch(PotionElement a, PotionElement b)
    {
        return
            (_elementA == a && _elementB == b) ||
            (_elementA == b && _elementB == a);
    }
}
