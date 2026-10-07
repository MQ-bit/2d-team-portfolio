using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverSceneManager : MonoBehaviour
{
    [SerializeField] private ScreenFadeController _fadeController;
    [SerializeField] private Animator _canvasAnimator;

    private Coroutine _gameOverSequenceCoroutine;

    private void Awake()
    {
        if (_fadeController == null)
        {
            _fadeController = FindAnyObjectByType<ScreenFadeController>();
        }
        if (_canvasAnimator == null)
        {
            _canvasAnimator = FindAnyObjectByType<Animator>();
        }
    }

    private void Start()
    {
        _gameOverSequenceCoroutine = StartCoroutine(GameOverSequenceCoroutine());
    }

    public void OnExitButtonClick()
    {
        _fadeController.TryFadeOut(onCompleted: GoToTitleScene);
    }

    private IEnumerator GameOverSequenceCoroutine()
    {
        yield return new WaitForSeconds(0.1f);
        _fadeController.TryFadeIn(onCompleted: GameOverSequence);
    }

    private void GameOverSequence()
    {
        _canvasAnimator.enabled = true;
    }

    private void GoToTitleScene()
    {
        SceneManager.LoadScene(0);
    }

}
