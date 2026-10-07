using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameOverSceneController : MonoBehaviour
{
    private static readonly int PlayIntroHash = Animator.StringToHash("PlayIntro");

    [Header("References")]
    [SerializeField] private ScreenFadeController _fadeController;
    [SerializeField] private Animator _canvasAnimator;
    [SerializeField] private Button _exitButton;

    [Header("Scene")]
    [SerializeField] private string _titleSceneName = "TitleScene";
    [SerializeField, Min(0f)] private float _introFadeDuration = 1.5f;

    private bool _isLeaving;

    private void Awake()
    {
        _exitButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _exitButton.onClick.AddListener(HandleExitRequested);
    }

    private void OnDisable()
    {
        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(HandleExitRequested);
        }
    }

    private IEnumerator Start()
    {
        _fadeController.ResetImmediately(1f);

        yield return null;

        bool accepted = _fadeController.TryFadeIn(_introFadeDuration, onCompleted: PlayIntro);

        if (!accepted)
        {
            Debug.LogError("FadeIn을 시작하지 못했습니다.", this);

            _fadeController.ResetImmediately(0f);
            PlayIntro();
        }
    }

    private void PlayIntro()
    {
        _canvasAnimator.SetTrigger(PlayIntroHash);
    }

    private void HandleExitRequested()
    {
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;
        _exitButton.interactable = false;

        _fadeController.TryFadeOut(onCompleted: LoadTitleScene);
    }

    private void LoadTitleScene()
    {
        SceneManager.LoadScene(_titleSceneName);
    }
}