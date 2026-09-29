using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MVP 패턴을 적용함. PlayerStatus가 model, PlayerStatusPresenter.cs가 presenter가 됨.
/// </summary>
public class PlayerStatusView : MonoBehaviour
{
    [Header("Portrait")]
    [SerializeField] private Image _portraitImage;

    [Header("Status")]
    [SerializeField] private Slider _healthGuage;
    [SerializeField] private Slider _thirstGuage;

    private void Awake()
    {
        InitiateSliderSetting(_healthGuage);
        InitiateSliderSetting(_thirstGuage);
    }

    public void Initialize(Sprite portrait)
    {
        _portraitImage.sprite = portrait;
        _portraitImage.enabled = portrait != null;
    }

    public void SetHealth(float current, float maximum)
    {
        float normalizedValue = CalculateNormalizedValue(current, maximum);
        _healthGuage.SetValueWithoutNotify(normalizedValue);
    }

    public void SetThirst(float current, float maximum)
    {
        float normalizedValue = CalculateNormalizedValue(current, maximum);
        _thirstGuage.SetValueWithoutNotify(normalizedValue);
    }

    private void InitiateSliderSetting(Slider guageSlider)
    {
        guageSlider.minValue = 0f;
        guageSlider.maxValue = 1f;
        guageSlider.wholeNumbers = false; //정수값 단위 세팅 제거
    }

    private float CalculateNormalizedValue(float current, float maximum)
    {
        if (maximum <= 0) return 0f;

        return Mathf.Clamp01(current / maximum);
    }
}
