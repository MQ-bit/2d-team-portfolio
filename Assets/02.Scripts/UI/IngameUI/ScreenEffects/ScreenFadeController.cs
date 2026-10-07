using System;
using System.Collections;
using UnityEngine;

public sealed class ScreenFadeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private float _defaultFadeOutDuration = 0.25f;
    [SerializeField] private float _defaultFadeInDuration = 0.25f;

    private Coroutine _fadeCoroutine;
    private bool _isInitialized;
    private bool _isFading;

    public bool IsFading => _isFading;
    public bool IsCovered => _canvasGroup != null && _canvasGroup.alpha >= 0.999f;

    private void Awake()
    {
        Initialize();
    }

    public bool Initialize()
    {
        if (_isInitialized)
        {
            return _canvasGroup != null;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_canvasGroup == null)
        {
            Debug.LogError($"{nameof(ScreenFadeController)}: {nameof(CanvasGroup)}이 연결되지 않았습니다.");
            return false;
        }

        _isInitialized = true;
        ResetImmediately();

        return true;
    }
    public bool TryFadeOut(float duration = -1f, Action onCompleted = null)
    {
        return TryStart(FadeOutCoroutine(ResolveFadeOutDuration(duration)), onCompleted);
    }

    public bool TryFadeIn(float duration = -1f, Action onCompleted = null)
    {
        return TryStart(FadeInCoroutine(ResolveFadeInDuration(duration)), onCompleted);
    }

    public bool TryTransition(Action onCovered, float fadeOutDuration = -1f, float coveredDuration = 0f, float fadeInDuration = -1f, Action onCompleted = null)
    {
        float resolvedFadeOutDuration = ResolveFadeOutDuration(fadeOutDuration);
        float resolvedFadeInDuration = ResolveFadeInDuration(fadeInDuration);
        coveredDuration = Mathf.Max(0f, coveredDuration);

        return TryStart(TransitionCoroutine(onCovered, resolvedFadeOutDuration, coveredDuration, resolvedFadeInDuration), onCompleted);
    }

    public void ResetImmediately()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        _isFading = false;

        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = 0f;
        SetRaycastBlocked(false);
    }

    private bool TryStart(IEnumerator fadeRoutine, Action onCompleted)
    {
        if (!Initialize() || _isFading)
        {
            return false;
        }

        _isFading = true;
        _fadeCoroutine = StartCoroutine(RunFadeCoroutine(fadeRoutine, onCompleted));

        return true;
    }

    private IEnumerator RunFadeCoroutine(IEnumerator fadeRoutine, Action onCompleted)
    {
        yield return fadeRoutine;

        _isFading = false;
        _fadeCoroutine = null;

        InvokeSafely(onCompleted);
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        SetRaycastBlocked(true);
        yield return FadeTo(1f, duration);
    }

    private IEnumerator FadeInCoroutine(float duration)
    {
        SetRaycastBlocked(true);
        yield return FadeTo(0f, duration);
        SetRaycastBlocked(false);
    }

    private IEnumerator TransitionCoroutine(Action onCovered, float fadeOutDuration, float coveredDuration, float fadeInDuration)
    {
        SetRaycastBlocked(true);

        yield return FadeTo(1f, fadeOutDuration);

        InvokeSafely(onCovered);

        if (coveredDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(coveredDuration);
        }

        yield return FadeTo(0f, fadeInDuration);

        SetRaycastBlocked(false);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = _canvasGroup.alpha;

        if (duration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);

            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }

    private float ResolveFadeOutDuration(float duration)
    {
        return duration < 0f ? _defaultFadeOutDuration : Mathf.Max(0f, duration);
    }

    private float ResolveFadeInDuration(float duration)
    {
        return duration < 0f ? _defaultFadeInDuration : Mathf.Max(0f, duration);
    }

    private void SetRaycastBlocked(bool blocked)
    {
        _canvasGroup.interactable = blocked;
        _canvasGroup.blocksRaycasts = blocked;
    }

    private void InvokeSafely(Action callback)
    {
        if (callback == null)
        {
            return;
        }

        try
        {
            callback.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void OnDisable()
    {
        ResetImmediately();
    }
}