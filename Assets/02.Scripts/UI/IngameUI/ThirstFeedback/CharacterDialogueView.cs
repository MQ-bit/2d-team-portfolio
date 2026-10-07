using UnityEngine;
using System.Collections;
using TMPro;

public class CharacterDialogueView : MonoBehaviour
{
    [SerializeField] private RectTransform _dialogArea;
    [SerializeField] private TMP_Text _dialogText;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float _openDuration = 0.25f;
    [SerializeField, Min(0f)] private float _holdDuration = 2f;
    [SerializeField, Min(0f)] private float _closeDuration = 0.25f;

    private Coroutine _dialogRoutine;
    private Vector3 _visibleScale = Vector3.one;
    private bool _initialized;

    public void Initialize()
    {
        if (!_initialized)
        {
            CacheVisibleScale();
            _initialized = true;
        }

        HideImmediately();
    }

    public void Play(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || _dialogArea == null || _dialogText == null)
        {
            return;
        }

        StopCurrentAnimation();
        _dialogRoutine = StartCoroutine(PlayCoroutine(line));
    }

    public void HideImmediately()
    {
        StopCurrentAnimation();

        if (_dialogArea != null)
        {
            _dialogArea.localScale = Vector3.zero;
            _dialogArea.gameObject.SetActive(false);
        }

        if (_dialogText != null)
        {
            _dialogText.text = string.Empty;
        }
    }

    private void CacheVisibleScale()
    {
        if (_dialogArea == null)
        {
            return;
        }

        _visibleScale = _dialogArea.localScale;

        if (_visibleScale.sqrMagnitude <= Mathf.Epsilon)
        {
            _visibleScale = Vector3.one;
        }
    }

    private IEnumerator PlayCoroutine(string line)
    {
        _dialogText.text = line;
        _dialogArea.localScale = Vector3.zero;
        _dialogArea.gameObject.SetActive(true);

        yield return AnimateScale(Vector3.zero, _visibleScale, _openDuration);

        if (_holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(_holdDuration);
        }

        yield return AnimateScale(_visibleScale, Vector3.zero, _closeDuration);

        _dialogArea.localScale = Vector3.zero;
        _dialogArea.gameObject.SetActive(false);
        _dialogRoutine = null;
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            _dialogArea.localScale = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

            _dialogArea.localScale = Vector3.LerpUnclamped(from, to, easedTime);

            yield return null;
        }

        _dialogArea.localScale = to;
    }

    private void StopCurrentAnimation()
    {
        if (_dialogRoutine == null)
        {
            return;
        }

        StopCoroutine(_dialogRoutine);
        _dialogRoutine = null;
    }

    private void OnDisable()
    {
        HideImmediately();
    }
}
