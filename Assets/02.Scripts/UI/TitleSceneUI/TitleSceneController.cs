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

    [Header("Sound")]
    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField, Range(0f, 1f)] private float _buttonClickVolume = 1f;

    private bool _isTransitioning;

    private void Awake()
    {
        SetupButtonSounds();

        if (_fadeGroup != null)
        {
            _fadeGroup.alpha = 0f;
            _fadeGroup.blocksRaycasts = false;
            _fadeGroup.interactable = false;
        }
    }

    // 씬에 있는 모든 버튼(튜토리얼 창처럼 꺼져 있는 것도 포함)에 클릭 소리를 붙인다.
    private void SetupButtonSounds()
    {
        if (_buttonClickSound == null)
            return;

        SoundManager.Preload(_buttonClickSound);

        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            button.onClick.AddListener(PlayButtonClick);
        }
    }

    private void PlayButtonClick()
    {
        SoundManager.PlayUi(_buttonClickSound, _buttonClickVolume);
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