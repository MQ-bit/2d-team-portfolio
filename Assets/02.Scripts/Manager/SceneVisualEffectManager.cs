using UnityEngine;

/// <summary>
/// 인게임 씬에 있는 시각적 효과(PostProcess)들을 관리하는 매니저입니다.
/// </summary>
public class SceneVisualEffectManager : MonoBehaviour
{
    [Header("Renderer Feature Controllers")]
    [SerializeField] private RendererFeatureController _wobbleController;

    private void Awake()
    {
        ResolveReferences();

        if (_wobbleController != null)
        {
            _wobbleController.Initialize();
        }

        ResetAllEffects();
    }

    public void SetWobbleActive(bool active)
    {
        if (_wobbleController == null)
        {
            return;
        }

        _wobbleController.SetFeatureActive(active);
    }

    public void ResetAllEffects()
    {
        SetWobbleActive(false);
    }

    private void ResolveReferences()
    {
        if (_wobbleController == null)
        {
            _wobbleController = GetComponentInChildren<RendererFeatureController>(true);
        }

        if (_wobbleController == null)
        {
            Debug.LogError($"{nameof(SceneVisualEffectManager)}: {nameof(RendererFeatureController)}가 연결되지 않았습니다.");
        }
    }

    private void OnDisable()
    {
        ResetAllEffects();
    }
}
