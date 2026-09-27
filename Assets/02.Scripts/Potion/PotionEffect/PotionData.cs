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
    [SerializeField] private PotionVisualData _visualData = new(null, Color.red);

    public string Id => _id;
    public string Name => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
    public PotionElement Element => _element;
    public float AreaWidth => _areaWidth;
    public float AreaDuration => _areaDuration;
    public float AreaEffectAmount => _areaEffectAmount;
    public float TickInterval => _tickInterval;
    public float EffectDuration => _effectDuration;
    public float DrinkEffectAmount => _drinkEffectAmount;
    public float DrinkThirstRecoveryAmount => _drinkThirstRecoveryAmount;
    public PotionVisualData VisualData => _visualData;

    public bool HasAreaEffect => AreaDuration > 0f && AreaWidth > 0f;
    public bool HasPeriodicAreaEffect => AreaEffectAmount > 0f && TickInterval > 0f;

    private void OnValidate()
    {
        _id = _id?.Trim();
        _displayName = _displayName?.Trim();
        _areaWidth = SanitizeNonNegative(_areaWidth);
        _areaEffectAmount = SanitizeNonNegative(_areaEffectAmount);
        _tickInterval = SanitizeNonNegative(_tickInterval);
        _areaDuration = SanitizeNonNegative(_areaDuration);
        _effectDuration = SanitizeNonNegative(_effectDuration);
        _drinkEffectAmount = SanitizeNonNegative(_drinkEffectAmount);
        _drinkThirstRecoveryAmount = SanitizeFinite(_drinkThirstRecoveryAmount);
    }

    private static float SanitizeNonNegative(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

    private static float SanitizeFinite(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
}
