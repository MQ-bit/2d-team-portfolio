using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private ScreenFadeController _fadeController;
    [SerializeField] private string _ingameSceneName = "IngameScene";
    [SerializeField] private float _fadeDuration = 0.8f;

    private bool _isTransitioning;
    private SoundHandle _exitSoundHandle;

    private void Awake()
    {
        // 페이드인이 끝날 때까지 Start/Exit 버튼의 전환 요청을 막습니다.
        _isTransitioning = true;

        if (_fadeController != null && _fadeController.Initialize())
        {
            // 첫 화면이 잠깐 노출되지 않도록 렌더링 전에 검은 화면으로 설정합니다.
            _fadeController.ResetImmediately(1f);
        }
    }

    private IEnumerator Start()
    {
        SoundManager.Preload(GameSoundId.GameExit);
        SoundManager.PlayBgm(GameBgmId.Title);

        // 씬의 초기화가 끝난 다음 프레임부터 화면을 밝힙니다.
        yield return null;

        if (_fadeController == null ||
            !_fadeController.TryFadeIn(
                _fadeDuration,
                onCompleted: () => _isTransitioning = false))
        {
            Debug.LogError("타이틀 페이드인을 시작하지 못했습니다.", this);

            // 설정 오류가 있어도 검은 화면이나 전환 잠금이 남지 않도록 복구합니다.
            _fadeController?.ResetImmediately(0f);
            _isTransitioning = false;
        }
    }

    // 게임 시작 버튼의 On Click()에 연결
    public void StartGame()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;

        if (_startButton != null)
            _startButton.interactable = false;

        SoundManager.Play(GameSoundId.GameStart);
        SoundManager.FadeOutBgm(_fadeDuration);
        StartCoroutine(FadeOutAndStartGame());
    }

    private IEnumerator FadeOutAndStartGame()
    {
        yield return FadeOut();

        SceneManager.LoadScene(_ingameSceneName);
    }

    // 나가기 버튼의 On Click()에 연결
    public void ExitGame()
    {
        if (_isTransitioning)
            return;

        _isTransitioning = true;
        _exitSoundHandle = SoundManager.Play(GameSoundId.GameExit);
        SoundManager.FadeOutBgm(_fadeDuration);
        StartCoroutine(FadeOutAndExitGame());
    }

    private IEnumerator FadeOutAndExitGame()
    {
        yield return FadeOut();

        // 앱 종료로 종료음이 잘리지 않도록 남은 재생을 기다린다.
        while (_exitSoundHandle.IsPlaying)
            yield return null;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeOut()
    {
        if (_fadeController == null)
        {
            Debug.LogError("TitleSceneController의 Fade Controller를 연결하세요.", this);
            yield break;
        }

        // 이미 진행 중인 페이드가 있다면 끝난 뒤 요청합니다.
        while (_fadeController.IsFading)
        {
            yield return null;
        }

        bool completed = false;

        bool accepted = _fadeController.TryFadeOut(
            _fadeDuration,
            onCompleted: () => completed = true);

        if (!accepted)
        {
            Debug.LogError("타이틀 페이드아웃을 시작하지 못했습니다.", this);
            yield break;
        }

        // 화면이 완전히 검게 된 다음 기존 코드가 씬을 로드합니다.
        while (!completed)
        {
            yield return null;
        }
    }
}
