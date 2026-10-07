using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeControlView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider _slider;
    [SerializeField] private Button _decreaseButton;
    [SerializeField] private Button _increaseButton;
    [SerializeField] private TMP_Text _percentageText;

    [Header("Settings")]
    [SerializeField, Range(0.01f, 1f)] private float _step = 0.05f;

    public event Action<float> OnValueChanged;
    public float Value => _slider.value;

    private void OnEnable()
    {
        _slider.onValueChanged.AddListener(HandleValueChanged);
        _decreaseButton.onClick.AddListener(Decrease);
        _increaseButton.onClick.AddListener(Increase);
        UpdatePercentage();
    }

    private void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(HandleValueChanged);
        _decreaseButton.onClick.RemoveListener(Decrease);
        _increaseButton.onClick.RemoveListener(Increase);
    }

    // SoundManager 연결 시 저장된 값을 UI에 표시하기 위한 초기화 함수.
    public void SetValue(float value)
    {
        _slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        UpdatePercentage();
    }

    public void SetInteractable(bool interactable)
    {
        _slider.interactable = interactable;
        _decreaseButton.interactable = interactable;
        _increaseButton.interactable = interactable;
    }

    private void Decrease() => ChangeValue(-_step);
    private void Increase() => ChangeValue(_step);

    private void ChangeValue(float amount)
    {
        _slider.value = Mathf.Clamp01(Mathf.Round((_slider.value + amount) * 100f) / 100f);
    }

    private void HandleValueChanged(float value)
    {
        UpdatePercentage();
        OnValueChanged?.Invoke(value);
    }

    private void UpdatePercentage()
    {
        _percentageText.text = $"{Mathf.RoundToInt(_slider.value * 100f)}%";
    }
}
