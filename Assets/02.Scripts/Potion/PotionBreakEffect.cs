using System.Collections;
using UnityEngine;

public sealed class PotionBreakEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _visual;
    [SerializeField] private SpriteRenderer[] _renderers;

    [Header("Break Animation")]
    [SerializeField, Min(0.01f)] private float _duration = 0.3f;
    [SerializeField, Min(0f)] private float _startScale = 0.5f;
    [SerializeField, Min(0f)] private float _peakScale = 1.25f;
    [SerializeField] private float _rotation = 45f;

    private bool _isPlaying;
    private Vector3 _baseScale;
    private Quaternion _baseRotation;
    private Color[] _baseColors;

    private void Awake()
    {
        if (_visual == null)
        {
            _visual = transform;
        }

        CacheRenderers();
        _baseScale = _visual.localScale;
        _baseRotation = _visual.localRotation;
        _baseColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _baseColors[i] = _renderers[i].color;
        }
    }

    public void Play()
    {
        if (_isPlaying)
        {
            return;
        }

        _isPlaying = true;
        ApplyVisual(_startScale, 0f, 1f);
        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        float elapsedTime = 0f;
        float duration = Mathf.Max(0.01f, _duration);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            ApplyVisual(EvaluateScale(progress), progress, 1f - progress);

            yield return null;
        }

        Destroy(gameObject);
    }

    private float EvaluateScale(float progress)
    {
        const float peakTime = 0.4f;
        return progress < peakTime
            ? Mathf.Lerp(_startScale, _peakScale, progress / peakTime)
            : Mathf.Lerp(_peakScale, 0f, (progress - peakTime) / (1f - peakTime));
    }

    private void ApplyVisual(float scale, float rotationProgress, float alpha)
    {
        _visual.localScale = _baseScale * scale;
        _visual.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, _rotation * rotationProgress);

        for (int i = 0; i < _renderers.Length; i++)
        {
            Color color = _baseColors[i];
            color.a *= alpha;
            _renderers[i].color = color;
        }
    }

    private void CacheRenderers()
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null)
            {
                _renderers = GetComponentsInChildren<SpriteRenderer>(true);
                return;
            }
        }
    }
}
