using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private CanvasGroup _fadeGroup;
    [SerializeField] private string _ingameSceneName = "IngameScene";
    [SerializeField] private float _fadeDuration = 0.8f;

    private bool _isTransitioning;

    private void Awake()
    {
        if (_fadeGroup != null)
        {
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
            _fadeGroup.interactable = false;
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
        StartCoroutine(FadeOutAndExitGame());
    }

    private IEnumerator FadeOutAndExitGame()
    {
        yield return FadeOut();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeOut()
    {
        if (_fadeGroup == null)
            yield break;

        _fadeGroup.blocksRaycasts = true;
        _fadeGroup.interactable = false;

        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            _fadeGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
            yield return null;
        }

        _fadeGroup.alpha = 1f;
    }
}