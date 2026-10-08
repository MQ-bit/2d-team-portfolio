using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DrinkingProgressView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerDrinkingModule _drinkingModule;
    [SerializeField] private Slider _slider;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.15f;

    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (_drinkingModule == null)
        {
            _drinkingModule = GetComponentInParent<PlayerDrinkingModule>();
        }

        if (_slider == null)
        {
            _slider = GetComponentInChildren<Slider>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.interactable = false;

        HideImmediately();
    }

    private void OnEnable()
    {
        if (_drinkingModule == null)
        {
            return;
        }

        _drinkingModule.OnDrinkStateChanged += HandleDrinkingStateChanged;
        HandleDrinkingStateChanged(_drinkingModule.IsDrinking);
    }

    private void OnDisable()
    {
        if (_drinkingModule != null)
        {
            _drinkingModule.OnDrinkStateChanged -= HandleDrinkingStateChanged;
        }

        StopFade();
        HideImmediately();
    }

    private void LateUpdate()
    {
        if (_drinkingModule == null || !_drinkingModule.IsDrinking)
        {
            return;
        }

        _slider.SetValueWithoutNotify(_drinkingModule.DrinkProgress);
    }

    private void HandleDrinkingStateChanged(bool isDrinking)
    {
        if (isDrinking)
        {
            BeginFadeIn();
        }
        else
        {
            HideImmediately();
        }
    }

    private void BeginFadeIn()
    {
        StopFade();

        _slider.SetValueWithoutNotify(0f);
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (_fadeInDuration <= 0f)
        {
            _canvasGroup.alpha = 1f;
            return;
        }

        _fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _fadeInDuration)
        {
            elapsedTime += Time.deltaTime;

            _canvasGroup.alpha = Mathf.Clamp01(elapsedTime / _fadeInDuration);

            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _fadeCoroutine = null;
    }

    private void HideImmediately()
    {
        StopFade();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_slider != null)
        {
            _slider.SetValueWithoutNotify(0f);
        }
    }

    private void StopFade()
    {
        if (_fadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
    }
}