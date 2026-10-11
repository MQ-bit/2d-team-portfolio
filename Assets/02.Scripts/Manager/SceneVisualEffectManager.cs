using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// 인게임 씬의 Post Process 및 화면 전환 효과를 관리합니다.
/// </summary>
public class SceneVisualEffectManager : MonoBehaviour
{
    private static SceneVisualEffectManager _instance;
    public static SceneVisualEffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SceneVisualEffectManager>();
            }

            return _instance;
        }
    }


    [Header("Renderer Feature Controllers")]
    [SerializeField] private RendererFeatureController _wobbleController;

    [Header("Screen Fade")]
    [SerializeField] private ScreenFadeController _fadeController;
    [SerializeField, Min(0f)] private float _sceneFadeInDuration = 0.8f;

    [Header("Input Target")]
    [SerializeField] private PlayerController _player;

    private bool _ownsFadeInputLock;
    private bool _didWarnMissingPlayer;

    public bool IsFading => _fadeController != null && _fadeController.IsFading;

    public bool IsScreenCovered => _fadeController != null && _fadeController.IsCovered;

    
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        ResolveReferences();
        ControllerInitialize();

        ResetAllEffects();
    }

    private IEnumerator Start()
    {
        if (!EnsureFadeController())
        {
            yield break;
        }

        // Awake의 ResetAllEffects가 화면을 투명하게 초기화하므로,
        // 모든 Awake가 끝난 Start에서 진입용 검은 화면을 설정합니다.
        _fadeController.ResetImmediately(1f);

        // 검은 화면에서 기다리는 동안에도 플레이어 입력을 차단합니다.
        AcquireFadeInputLock();

        yield return null;

        // 기존 매니저의 TryFadeIn을 사용하면 완료 시 입력 잠금도 해제됩니다.
        if (!TryFadeIn(_sceneFadeInDuration))
        {
            Debug.LogError("인게임 진입 페이드인을 시작하지 못했습니다.", this);

            _fadeController.ResetImmediately(0f);
            ReleaseFadeInputLock();
        }
    }
    
    private void ControllerInitialize()
    {
        if (_wobbleController != null)
        {
            _wobbleController.Initialize();
        }

        if (_fadeController != null)
        {
            _fadeController.Initialize();
        }
    }

    public void SetWobbleActive(bool active)
    {
        if (_wobbleController == null)
        {
            return;
        }

        _wobbleController.SetFeatureActive(active);
    }

    public bool TryFadeOut(float duration = -1f, Action onCompleted = null)
    {
        if (!EnsureFadeController())
        {
            return false;
        }

        bool acquiredInputLock = AcquireFadeInputLock();
        bool accepted = _fadeController.TryFadeOut(duration, onCompleted);

        if (!accepted && acquiredInputLock)
        {
            ReleaseFadeInputLock();
        }

        return accepted;
    }

    public bool TryFadeIn(float duration = -1f, Action onCompleted = null)
    {
        if (!EnsureFadeController())
        {
            return false;
        }

        bool acquiredInputLock = AcquireFadeInputLock();
        bool accepted = _fadeController.TryFadeIn(duration, CreateFadeInCompletedHandler(onCompleted));

        if (!accepted && acquiredInputLock)
        {
            ReleaseFadeInputLock();
        }

        return accepted;
    }

    public bool TryFadeTransition(Action onCovered, float fadeOutDuration = -1f, float coveredDuration = 0f, float fadeInDuration = -1f, Action onCompleted = null)
    {
        if (!EnsureFadeController())
        {
            return false;
        }

        bool acquiredInputLock = AcquireFadeInputLock();
        bool accepted = _fadeController.TryTransition(onCovered, fadeOutDuration, coveredDuration, fadeInDuration, CreateFadeInCompletedHandler(onCompleted));

        if (!accepted && acquiredInputLock)
        {
            ReleaseFadeInputLock();
        }

        return accepted;
    }

    private Action CreateFadeInCompletedHandler(Action onCompleted)
    {
        return () =>
        {
            ReleaseFadeInputLock();
            onCompleted?.Invoke();
        };
    }

    public void ResetAllEffects()
    {
        SetWobbleActive(false);

        if (_fadeController != null)
        {
            _fadeController.ResetImmediately();
        }

        ReleaseFadeInputLock();
    }

    private bool AcquireFadeInputLock()
    {
        if (_ownsFadeInputLock)
        {
            return false;
        }

        ResolvePlayer();

        if (_player == null)
        {
            if (!_didWarnMissingPlayer)
            {
                Debug.LogWarning($"{nameof(SceneVisualEffectManager)}: {nameof(PlayerController)}를 찾지 못해 페이드 중 게임 입력을 차단하지 못했습니다.",
                    this);

                _didWarnMissingPlayer = true;
            }

            return false;
        }

        _player.AcquireGamePlayInputLock(this);
        _ownsFadeInputLock = true;

        return true;
    }

    private void ReleaseFadeInputLock()
    {
        if (!_ownsFadeInputLock)
        {
            return;
        }

        if (_player != null)
        {
            _player.ReleaseGamePlayInputLock(this);
        }

        _ownsFadeInputLock = false;
    }

    private bool EnsureFadeController()
    {
        if (_fadeController == null)
        {
            _fadeController = FindAnyObjectByType<ScreenFadeController>(FindObjectsInactive.Include);
        }

        if (_fadeController == null)
        {
            Debug.LogError($"{nameof(SceneVisualEffectManager)}: {nameof(ScreenFadeController)}가 연결되지 않았습니다.");
            return false;
        }

        return _fadeController.Initialize();
    }

    private void ResolvePlayer()
    {
        if (_player == null)
        {
            _player = FindAnyObjectByType<PlayerController>();
        }

        if (_player != null)
        {
            _didWarnMissingPlayer = false;
        }
    }

    private void ResolveReferences()
    {
        if (_wobbleController == null)
        {
            _wobbleController = GetComponentInChildren<RendererFeatureController>(true);
        }

        if (_fadeController == null)
        {
            _fadeController = FindAnyObjectByType<ScreenFadeController>(FindObjectsInactive.Include);
        }

        ResolvePlayer();

        if (_wobbleController == null)
        {
            Debug.LogError($"{nameof(SceneVisualEffectManager)}: {nameof(RendererFeatureController)}가 연결되지 않았습니다.");
        }

        if (_fadeController == null)
        {
            Debug.LogError($"{nameof(SceneVisualEffectManager)}: {nameof(ScreenFadeController)}가 연결되지 않았습니다.");
        }
    }

    private void OnDisable()
    {
        ResetAllEffects();
    }
}