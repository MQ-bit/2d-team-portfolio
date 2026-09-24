using System.Collections;
using UnityEngine;

public class PotionGroundArea : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField, Min(0f)] private float _duration = 3f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;

    [Header("Visual")]
    [SerializeField] private Transform _visual;
    [SerializeField] private SpriteRenderer[] _renderers;
    [SerializeField, Min(0f)] private float _appearDuration = 0.15f;

    private bool _isActive;
    private Vector3 _baseScale;
    private Color[] _baseColors;

    private void Awake()
    {
        _visual ??= transform;
        CacheRenderers();
        _baseScale = _visual.localScale;
        _baseColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            _baseColors[i] = _renderers[i].color;
        }
    }

    public void Activate()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        ApplyVisual(0f, 1f);
        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        float elapsedTime = 0f;
        float lifetime = Mathf.Max(0f, _duration);

        while (elapsedTime < lifetime)
        {
            elapsedTime += Time.deltaTime;

            float appearProgress = _appearDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsedTime / _appearDuration);
            float remainingTime = lifetime - elapsedTime;
            float alpha = _fadeDuration <= 0f
                ? 1f
                : Mathf.Clamp01(remainingTime / _fadeDuration);
            ApplyVisual(appearProgress, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyVisual(float scale, float alpha)
    {
        _visual.localScale = _baseScale * scale;

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
