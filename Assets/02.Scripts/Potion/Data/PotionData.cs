using System.Collections.Generic;
using UnityEngine;

public enum PotionElement
{
    None,
    Fire,
    Poison,
    Ice,
    Heal,
    Lightning,
    Vaporization
}

[CreateAssetMenu(fileName = "PotionData", menuName = "Potion/Potion Data")]
public sealed class PotionData : ScriptableObject, IPotionEffectData
{
    [Header("Identity")]
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private PotionElement _element;

    [Header("Thrown Effect")]
    [Tooltip("투척 효과의 대상. 섭취 효과는 마신 대상에게 적용한다.")]
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

    [Header("Drink")]
    [Tooltip("양수는 갈증 감소, 음수는 갈증 증가")]
    [SerializeField] private float _thirstDelta;

    [Header("Visual")]
    [SerializeField] private PotionVisualData _visualData = new(null, Color.red, null, null);

    public string Id => _id;
    public string Name => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
    public string Description => _description;
    public PotionElement Element => _element;
    public float AreaWidth => _effectRadius * 2f;
    public float ThirstDelta => _thirstDelta;
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

    private void OnValidate()
    {
        _id = _id?.Trim();
        _displayName = _displayName?.Trim();
        _description = _description?.Trim();
        _thirstDelta = PotionDataValidation.Finite(_thirstDelta);
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
