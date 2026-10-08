using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneTransitionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStatus _playerStatus;
    [SerializeField] private SceneVisualEffectManager _visualEffectManager;

    [Header("Transition To GameOverScene")] //TODO : 스크립터블 오브젝트화 해보기
    [SerializeField] private string _gameOverSceneName = "GameOverScene";
    [SerializeField, Min(0f)] private float _deathDelay = 1.5f;
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.25f;

    private Coroutine _transitionRoutine;
    private bool _isTransitioning;

    public bool IsTransitioning => _isTransitioning;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (_playerStatus != null)
        {
            _playerStatus.OnDeath += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (_playerStatus != null)
        {
            _playerStatus.OnDeath -= HandlePlayerDeath;
        }

        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        _isTransitioning = false;
    }

    private void ResolveReferences()
    {
        if (_playerStatus == null)
        {
            _playerStatus = FindAnyObjectByType<PlayerStatus>();
        }

        if (_visualEffectManager == null)
        {
            _visualEffectManager = FindAnyObjectByType<SceneVisualEffectManager>();
        }
    }

    private void HandlePlayerDeath()
    {
        bool canTransition = TryTransitionToScene(_gameOverSceneName, _deathDelay, _fadeOutDuration);

        if (canTransition)
        {
            ScoreManager.Instance?.EndRun();
        }
    }

    public bool TryTransitionToScene(string sceneName, float delay = 0f, float fadeOutDuration = 0.25f)
    {
        if (_isTransitioning)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"{nameof(SceneTransitionController)}: 씬 이름이 비어 있습니다.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName)) //페이드 전 씬 전환 체크
        {
            Debug.LogError($"{nameof(SceneTransitionController)}: 씬 '{sceneName}'이 Build Settings에 등록되어 있지 않습니다.");
            return false;
        }

        _isTransitioning = true;

        _transitionRoutine = StartCoroutine(TransitionRoutine(sceneName, Mathf.Max(0f, delay), Mathf.Max(0f, fadeOutDuration)));
        return true;
    }

    private IEnumerator TransitionRoutine(string sceneName, float delay, float fadeOutDuration)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (_visualEffectManager != null)
        {
            while (_visualEffectManager.IsFading)
            {
                yield return null;
            }

            bool accepted = _visualEffectManager.TryFadeOut(fadeOutDuration, () => LoadScene(sceneName));

            if (accepted)
            {
                _transitionRoutine = null;
                yield break;
            }

            Debug.LogWarning($"{nameof(SceneTransitionController)}: 페이드아웃을 시작하지 못해 씬을 바로 전환합니다.");
        }
        else
        {
            Debug.LogError($"{nameof(SceneTransitionController)}: {nameof(SceneVisualEffectManager)}를 찾지 못했습니다.");
        }

        LoadScene(sceneName);
    }

    private void LoadScene(string sceneName)
    {
        _transitionRoutine = null;
        SceneManager.LoadScene(sceneName);
    }
}