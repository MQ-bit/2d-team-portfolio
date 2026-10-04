using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class RendererFeatureController : MonoBehaviour
{
    //여러개의 포스트 프로세싱을 관리 해야한다면 구조를 다시 설계해야함.
    [SerializeField] private ScriptableRendererData _rendererData; 
    [SerializeField] private string _featureName = "WobbleEffect";
    [SerializeField] private bool _disableOnDisable = true;

    private ScriptableRendererFeature _rendererFeature;
    private bool _initialized;

    public bool IsAvailable => _rendererFeature != null;
    public bool IsActive => _rendererFeature != null && _rendererFeature.isActive;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        ResolveRendererFeature();
        SetFeatureActive(false);
    }

    public void SetFeatureActive(bool active)
    {
        if (!_initialized)
        {
            Initialize();
        }

        if (_rendererFeature == null)
        {
            return;
        }

        if (_rendererFeature.isActive != active)
        {
            _rendererFeature.SetActive(active);
        }
    }

    private void ResolveRendererFeature()
    {
        _rendererFeature = null;

        if (_rendererData == null)
        {
            Debug.LogError($"{nameof(RendererFeatureController)}: Renderer Data가 연결되지 않았습니다.");
            return;
        }

        foreach (ScriptableRendererFeature feature in _rendererData.rendererFeatures)
        {
            if (feature == null)
            {
                continue;
            }

            if (string.Equals(feature.name, _featureName, StringComparison.Ordinal))
            {
                _rendererFeature = feature;
                return;
            }
        }

        Debug.LogError($"{nameof(RendererFeatureController)}: '{_featureName}' Renderer Feature를 찾지 못했습니다.");
    }

    private void OnDisable()
    {
        if (_disableOnDisable && _initialized)
        {
            SetFeatureActive(false);
        }
    }
}