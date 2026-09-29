using UnityEngine;
using UnityEngine.Serialization;

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
public sealed class PotionData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;

    [Header("New Effect Data (runtime connection in later steps)")]
    [Tooltip("새 기획 설정을 작성한 에셋 표시. 기존 에셋은 false이며 기존 API/실행은 그대로 유지된다.")]
    [SerializeField] private bool _hasEffectProfile;
    [SerializeField] private PotionEffectProfile _effectProfile = new();
    [SerializeField, Min(0f)] private float _flightSpeed = 10f;
    [SerializeField, Min(0f)] private float _maxThrowDistance = 8f;

    [Header("Area Effect")]
    [SerializeField] private PotionElement _element;
    [FormerlySerializedAs("_radius")]
    [SerializeField, Min(0f)] private float _areaWidth;
    [SerializeField, Min(0f)] private float _areaDuration;
    [FormerlySerializedAs("_damage")]
    [SerializeField, Min(0f)] private float _areaEffectAmount;
    [SerializeField, Min(0f)] private float _tickInterval;
    [SerializeField, Min(0f)] private float _effectDuration;

    [Header("Drink Effect")]
    [SerializeField, Min(0f)] private float _drinkEffectAmount;
    [SerializeField] private float _drinkThirstRecoveryAmount;

    [Header("Visual")]
    [SerializeField] private PotionVisualData _visualData = new(null, Color.red, null);

    public string Id => _id;
    public string Name => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
    public PotionElement Element => _element;
    public float AreaWidth => ThrownProfile != null ? ThrownProfile.EffectRadius * 2f : _areaWidth;
    public float AreaDuration => _areaDuration;
    public float AreaEffectAmount => _areaEffectAmount;
    public float TickInterval => _tickInterval;
    public float EffectDuration => _effectDuration;
    public float DrinkEffectAmount => _drinkEffectAmount;
    public float DrinkThirstRecoveryAmount => _drinkThirstRecoveryAmount;
    public PotionVisualData VisualData => _visualData;

    // 새 실행기는 HasEffectProfile을 확인한 뒤 사용한다. 레거시 필드와 자동 동기화하지 않는다.
    public bool HasEffectProfile => _hasEffectProfile && _effectProfile != null;
    public PotionEffectProfile EffectProfile => _effectProfile;
    public float FlightSpeed => _flightSpeed;
    public float MaxThrowDistance => _maxThrowDistance;

    // 외부 레거시 에셋을 덮어쓰지 않고 Potion 내부의 직렬화된 기본 설정을 사용한다.
    // 섭취/조합 레거시 API는 후속 단계에서 전환한다.
    private PotionData _defaultThrownData;
    public PotionEffectProfile ThrownProfile
    {
        get
        {
            if (HasEffectProfile) return _effectProfile;
            if (_defaultThrownData == null)
                _defaultThrownData = Resources.Load<PotionData>("SinglePotionDefaults/" + _element);
            return _defaultThrownData != null && _defaultThrownData.HasEffectProfile
                ? _defaultThrownData.EffectProfile : null;
        }
    }

    public bool HasAreaEffect => Element != PotionElement.Vaporization && AreaDuration > 0f && AreaWidth > 0f;
    public bool HasPeriodicAreaEffect => Element != PotionElement.Vaporization && AreaEffectAmount > 0f && TickInterval > 0f;

    private void OnValidate()
    {
        _defaultThrownData = null;
        _id = _id?.Trim();
        _displayName = _displayName?.Trim();
        _areaWidth = SanitizeNonNegative(_areaWidth);
        _areaEffectAmount = SanitizeNonNegative(_areaEffectAmount);
        _tickInterval = SanitizeNonNegative(_tickInterval);
        _areaDuration = SanitizeNonNegative(_areaDuration);
        _effectDuration = SanitizeNonNegative(_effectDuration);
        _drinkEffectAmount = SanitizeNonNegative(_drinkEffectAmount);
        _drinkThirstRecoveryAmount = SanitizeFinite(_drinkThirstRecoveryAmount);
        _flightSpeed = SanitizeNonNegative(_flightSpeed);
        _maxThrowDistance = SanitizeNonNegative(_maxThrowDistance);
        _effectProfile ??= new PotionEffectProfile();
        _effectProfile.Validate();
    }

    private static float SanitizeNonNegative(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

    private static float SanitizeFinite(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
}
