using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameOverSceneController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private ScreenFadeController _fadeController;
    [SerializeField] private string _titleSceneName = "TitleScene";
    [SerializeField, Min(0f)] private float _introFadeDuration = 1.5f;

    [Header("Score UI")]
    [SerializeField] private CanvasGroup _scoreRoot;
    [SerializeField] private CanvasGroup _floorRow;
    [SerializeField] private CanvasGroup _enemyRow;
    [SerializeField] private CanvasGroup _potionRow;
    [SerializeField] private CanvasGroup _pressAnyKey;

    [SerializeField] private TMP_Text _floorText;
    [SerializeField] private TMP_Text _enemyKillText;
    [SerializeField] private TMP_Text _potionText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject _gameOverRoot;
    [SerializeField] private CanvasGroup _gameOverText;
    [SerializeField] private CanvasGroup _tombCanvasGroup;
    [SerializeField] private RectTransform _tombImage;
    [SerializeField] private Button _exitButton;

    [Header("Score Count")]
    [SerializeField, Min(0f)] private float _scoreCountDuration = 1.2f;
    private int _floorTarget = 300;
    private int _enemyKillTarget = 300;
    private int _potionTarget = 500;

    [Header("Score Timing")]
    [SerializeField, Min(0f)] private float _rowFadeDuration = 0.35f;
    [SerializeField, Min(0f)] private float _rowInterval = 0.2f;

    [Header("Best Result")]
    [SerializeField] private CanvasGroup _newBestText;

    [SerializeField] private GameObject _bestScoreRoot;
    [SerializeField] private CanvasGroup _bestScoreGroup;

    [SerializeField] private TMP_Text _bestFloorText;
    [SerializeField] private TMP_Text _bestEnemyText;
    [SerializeField] private TMP_Text _bestPotionText;

    [SerializeField, Min(0f)] private float _bestResultFadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float _bestResultInterval = 0.3f;

    private bool _isNewBest;
    private Sequence _newBestLoop;

    [Header("Game Over Timing")]
    [SerializeField, Min(0f)] private float _scoreFadeOutDuration = 0.3f;
    [SerializeField, Min(0f)] private float _gameOverFadeDuration = 0.4f;
    [SerializeField, Min(0f)] private float _tombFadeDuration = 1.5f;
    [SerializeField, Min(0f)] private float _exitPopDuration = 0.4f;
    [SerializeField] private float _tombStartOffsetY = 700f;

    private Sequence _sequence;
    private IDisposable _inputSubscription;

    private float _tombTargetY;
    private bool _isLeaving;

    private void Awake()
    {
        Time.timeScale = 1f; // 게임 오버 씬 진입 시점에 Time.timeScale이 0으로 되어있을 수 있으므로 초기화

        ApplyScore();
        InitializePresentation();
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

        _inputSubscription?.Dispose();
        _inputSubscription = null;

        _newBestLoop?.Kill();
        _newBestLoop = null;

        _sequence?.Kill();
        _sequence = null;
    }

    private IEnumerator Start()
    {
        _fadeController.ResetImmediately(1f);

        yield return null;

        bool fadeCompleted = false;

        bool accepted = _fadeController.TryFadeIn(_introFadeDuration, onCompleted: () => fadeCompleted = true);

        if (accepted)
        {
            yield return new WaitUntil(() => fadeCompleted);
        }
        else
        {
            Debug.LogError("FadeIn을 시작하지 못했습니다.", this);
            _fadeController.ResetImmediately(0f);
        }

        PlayScoreSequence();
    }

    private void InitializePresentation()
    {
        _gameOverRoot.SetActive(true);

        _scoreRoot.alpha = 1f;

        SetHidden(_floorRow);
        SetHidden(_enemyRow);
        SetHidden(_potionRow);
        SetHidden(_pressAnyKey);
        SetHidden(_gameOverText);
        SetHidden(_tombCanvasGroup);

        _newBestText.alpha = 1f;
        _newBestText.gameObject.SetActive(false);

        _bestScoreRoot.SetActive(false);

        _tombTargetY = _tombImage.anchoredPosition.y;
        _tombImage.gameObject.SetActive(false);

        _exitButton.gameObject.SetActive(false);
        _exitButton.transform.localScale = Vector3.zero;
    }

    private void ApplyScore()
    {
        ScoreManager scoreManager = ScoreManager.Instance;
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager 인스턴스를 찾을 수 없습니다.", this);
            return;
        }

        ScoreData currentScore = scoreManager.GetCurrentScoreSnapshot();
        ScoreData bestScore = scoreManager.GetBestScoreSnapshot();

        _isNewBest = scoreManager.WasLastRunBest;

        //_floorTarget = currentScore.ClearedFloorCount;
        //_enemyKillTarget = currentScore.TotalKillCount;
        //_potionTarget = currentScore.ConsumedPotionCount;

        _bestFloorText.text = $"{bestScore.ClearedFloorCount}";
        _bestEnemyText.text = $"{bestScore.TotalKillCount}";
        _bestPotionText.text = $"{bestScore.ConsumedPotionCount}";
    }

    private Sequence CreateScoreRowSequence(CanvasGroup row, TMP_Text scoreText, int targetScore, string unit = "")
    {
        int displayedScore = 0;

        scoreText.text = "0";

        Sequence rowSequence = DOTween.Sequence();

        rowSequence.Append(row.DOFade(1f, _rowFadeDuration).SetEase(Ease.OutSine)); // Row 페이드 인
        rowSequence.Join(DOTween.To(
                                () => displayedScore,
                                value => { displayedScore = value; scoreText.text = $"{displayedScore} {unit}"; },
                                targetScore,
                                _scoreCountDuration).SetEase(Ease.Linear)
                        ); // 점수 카운트 애니메이션

        rowSequence.AppendCallback(() =>
        {
            scoreText.text = $"{targetScore} {unit}"; // 마지막 값이 정확히 표시되도록 보장
        });

        return rowSequence;
    }

    private void PlayScoreSequence()
    {
        _sequence?.Kill();

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Append(CreateScoreRowSequence(_floorRow, _floorText, _floorTarget, "층")) // 층수: Row 표시 + 숫자 증가
            .AppendInterval(_rowInterval)
            .Append(CreateScoreRowSequence(_enemyRow, _enemyKillText, _enemyKillTarget, "마리")) // 적 처치 수
            .AppendInterval(_rowInterval)
            .Append(CreateScoreRowSequence(_potionRow, _potionText, _potionTarget, "병")) // 포션 섭취 수
            .AppendInterval(_bestResultInterval)
            .AppendCallback(ShowBestResult)
            .AppendInterval(_rowInterval)
            .Append(_pressAnyKey.DOFade(1f, _rowFadeDuration).SetEase(Ease.OutSine)) // 모든 점수 표시 후 PRESS ANY KEY
            .OnComplete(BeginWaitingForInput);
    }

    private void BeginWaitingForInput()
    {
        _inputSubscription?.Dispose();

        _inputSubscription = InputSystem.onAnyButtonPress.CallOnce(_ => PlayGameOverSequence());
    }

    private void PlayGameOverSequence() //연출 시작 함수
    {
        _inputSubscription?.Dispose();
        _inputSubscription = null;

        _newBestLoop?.Kill();
        _newBestLoop = null;

        if (_newBestText != null)
        {
            _newBestText.transform.localScale = Vector3.one;
        }

        _sequence?.Kill();
        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Append(_scoreRoot.DOFade(0f, _scoreFadeOutDuration)) // 점수 화면 숨기기
            .AppendCallback(() => // 비석 시작 상태 준비
            {
                _tombImage.gameObject.SetActive(true);

                Vector2 position = _tombImage.anchoredPosition;
                position.y = _tombTargetY + _tombStartOffsetY;
                _tombImage.anchoredPosition = position;

                _tombCanvasGroup.alpha = 0f;
                _tombImage.localScale = Vector3.one * 0.95f;
            })
            .Append(_gameOverText.DOFade(1f, _gameOverFadeDuration).SetEase(Ease.OutSine))// GAME OVER가 천천히 나타남
            .Join(_tombCanvasGroup.DOFade(1f, _tombFadeDuration).SetEase(Ease.OutSine)) // 비석도 동시에 천천히 나타남
            .AppendInterval(0.35f) // 연출이 끝난 뒤 잠깐 정지
            .AppendCallback(() => // Exit 버튼 활성화
            {
                _exitButton.gameObject.SetActive(true);
                _exitButton.transform.localScale = Vector3.zero;
            })
            .Append(_exitButton.transform.DOScale(Vector3.one, _exitPopDuration).SetEase(Ease.OutBack, 1.7f)); // ExitButton 나타나는 효과
    }

    private static void SetHidden(CanvasGroup group)
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void HandleExitRequested()
    {
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;
        _exitButton.interactable = false;

        _sequence?.Kill();

        bool accepted = _fadeController.TryFadeOut(onCompleted: LoadTitleScene);

        if (!accepted)
        {
            LoadTitleScene();
        }
    }

    private void LoadTitleScene()
    {
        SceneManager.LoadScene(_titleSceneName);
    }

    private Sequence CreateBestResultSequence()
    {
        Sequence resultSequence = DOTween.Sequence();

        if (_isNewBest)
        {
            resultSequence.Append(_newBestText.DOFade(1f, _bestResultFadeDuration).SetEase(Ease.OutSine));
            resultSequence.Join(_newBestText.transform.DOScale(Vector3.one, _bestResultFadeDuration).SetEase(Ease.OutBack, 1.7f));
            resultSequence.Append(_newBestText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, vibrato: 5, elasticity: 0.5f));
        }
        else
        {
            resultSequence.Append(_bestScoreGroup.DOFade(1f, _bestResultFadeDuration).SetEase(Ease.OutSine));
        }

        return resultSequence;
    }

    private void ShowBestResult()
    {
        if (_isNewBest)
        {
            _bestScoreRoot.SetActive(false);

            _newBestText.gameObject.SetActive(true);
            _newBestText.alpha = 1f;
            _newBestText.transform.localScale = Vector3.one;

            StartNewBestLoop();
        }
        else
        {
            _newBestText.gameObject.SetActive(false);

            // 정확히 이 순간 확 나타남
            _bestScoreRoot.SetActive(true);
        }
    }

    private void StartNewBestLoop()
    {
        _newBestLoop?.Kill();

        Transform target = _newBestText.transform;
        target.localScale = Vector3.one;

        _newBestLoop = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Append(target.DOScale(1.18f, 0.14f).SetEase(Ease.OutQuad))
            .Append(target.DOScale(1f, 0.12f).SetEase(Ease.InQuad))
            .AppendInterval(0.01f)
            .SetLoops(-1, LoopType.Restart);
    }
}