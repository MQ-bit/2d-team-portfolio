using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [Tooltip("새 연출: GAME OVER → 점수·최고 기록 → 나가기. 꺼두면 기존 점수 우선 순서를 유지합니다.")]
    [SerializeField] private bool _gameOverFirst;
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

    [SerializeField, Min(0f)] private float _bestResultInterval = 0.3f;

    private bool _isNewBest;
    private Sequence _newBestLoop;

    [Header("Game Over Timing")]
    [Tooltip("새 연출에서 비석 효과음이 끝난 뒤 점수판 전환을 시작하기까지의 대기 시간입니다.")]
    [SerializeField, Min(0f)] private float _gameOverHoldDuration = 1f;
    [SerializeField, Min(0f)] private float _scoreFadeOutDuration = 0.3f;
    [SerializeField, Min(0f)] private float _gameOverFadeDuration = 0.4f;
    [SerializeField, Min(0f)] private float _tombFadeDuration = 0.15f;
    [SerializeField, Min(0f)] private float _tombDropDuration = 0.55f;
    [SerializeField, Min(0f)] private float _tombImpactDuration = 0.1f;
    [SerializeField, Min(0f)] private float _exitPopDuration = 0.4f;
    [SerializeField] private float _tombStartOffsetY = 700f;

    private Sequence _sequence;
    private IDisposable _inputSubscription;
    private SoundHandle _tombSoundHandle;
    private Coroutine _scoreTransitionRoutine;

    private float _tombTargetY;
    private bool _isLeaving;

    private void Awake()
    {
        Time.timeScale = 1f; // 게임 오버 씬 진입 시점에 Time.timeScale이 0으로 되어있을 수 있으므로 초기화
        SoundManager.StopChannel(SoundChannel.Player);
        SoundManager.StopChannel(SoundChannel.Potion);
        SoundManager.SetPaused(false, 0f);
        // 씬을 직접 실행해도 음악을 재생하며, 사망음 뒤에 예약된 곡은 유지한다.
        if (!SoundManager.IsBgmIdPending(GameBgmId.GameEnd)
            && !SoundManager.IsBgmIdPlaying(GameBgmId.GameEnd))
        {
            SoundManager.PlayBgm(GameBgmId.GameEnd);
        }
        SoundManager.Preload(GameSoundId.Tomb);

        if (_gameOverFirst)
        {
            var exitRect = (RectTransform)_exitButton.transform;
            Vector2 exitPosition = exitRect.anchoredPosition;
            _exitButton.transform.SetParent(_scoreRoot.transform, false);
            exitRect.anchoredPosition = exitPosition;
        }
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

        if (_scoreTransitionRoutine != null)
        {
            StopCoroutine(_scoreTransitionRoutine);
            _scoreTransitionRoutine = null;
        }

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

        if (_gameOverFirst)
        {
            // 사망음과 착지음이 겹치지 않도록 기다린다.
            while (SoundManager.IsBgmIdPending(GameBgmId.GameEnd))
                yield return null;
            PlayGameOverSequence();
        }
        else
        {
            PlayScoreSequence();
        }
    }

    private void InitializePresentation()
    {
        _gameOverRoot.SetActive(true);

        if (_gameOverFirst) SetHidden(_scoreRoot);
        else _scoreRoot.alpha = 1f;

        SetHidden(_floorRow);
        SetHidden(_enemyRow);
        SetHidden(_potionRow);
        SetHidden(_pressAnyKey);
        _pressAnyKey.gameObject.SetActive(!_gameOverFirst);
        SetHidden(_gameOverText);
        SetHidden(_tombCanvasGroup);

        _newBestText.alpha = 1f;
        _newBestText.gameObject.SetActive(false);

        _bestScoreRoot.SetActive(false);

        _tombTargetY = _tombImage.anchoredPosition.y;
        _tombImage.gameObject.SetActive(false);

        _exitButton.gameObject.SetActive(false);
        _exitButton.transform.localScale = Vector3.zero;
        _exitButton.interactable = false;
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

        _floorTarget = currentScore.ClearedFloorCount;
        _enemyKillTarget = currentScore.TotalKillCount;
        _potionTarget = currentScore.ConsumedPotionCount;

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
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        if (_gameOverFirst)
            _sequence.Append(_scoreRoot.DOFade(1f, _rowFadeDuration));

        _sequence.Append(CreateScoreRowSequence(_floorRow, _floorText, _floorTarget, "층")) // 층수: Row 표시 + 숫자 증가
            .AppendInterval(_rowInterval)
            .Append(CreateScoreRowSequence(_enemyRow, _enemyKillText, _enemyKillTarget, "마리")) // 적 처치 수
            .AppendInterval(_rowInterval)
            .Append(CreateScoreRowSequence(_potionRow, _potionText, _potionTarget, "병")) // 포션 섭취 수
            .AppendInterval(_bestResultInterval)
            .AppendCallback(ShowBestResult)
            .AppendInterval(_rowInterval);

        if (_gameOverFirst)
        {
            _sequence.OnComplete(ShowResultButtons);
        }
        else
        {
            _sequence.Append(_pressAnyKey.DOFade(1f, _rowFadeDuration).SetEase(Ease.OutSine))
                .OnComplete(BeginWaitingForInput);
        }
    }

    private void ShowResultButtons()
    {
        _scoreRoot.interactable = true;
        _scoreRoot.blocksRaycasts = true;
        _exitButton.gameObject.SetActive(true);
        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Append(_exitButton.transform.DOScale(Vector3.one, _exitPopDuration).SetEase(Ease.OutBack, 1.7f))
            .OnComplete(() =>
            {
                _exitButton.interactable = true;
                EventSystem.current?.SetSelectedGameObject(_exitButton.gameObject);
            });
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
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        if (!_gameOverFirst)
            _sequence.Append(_scoreRoot.DOFade(0f, _scoreFadeOutDuration));

        _sequence.AppendCallback(() => // 비석 시작 상태 준비
            {
                _tombImage.gameObject.SetActive(true);

                Vector2 position = _tombImage.anchoredPosition;
                position.y = _tombTargetY + _tombStartOffsetY;
                _tombImage.anchoredPosition = position;

                _tombCanvasGroup.alpha = 0f;
                _tombImage.localScale = Vector3.one;
            })
            .Append(_gameOverText.DOFade(1f, _gameOverFadeDuration).SetEase(Ease.OutSine))// GAME OVER가 천천히 나타남
            .Join(CreateTombDropSequence()); // 비석 낙하와 착지 연출

        if (_gameOverFirst)
        {
            _sequence.OnComplete(() =>
                _scoreTransitionRoutine = StartCoroutine(TransitionToScoreAfterTombSound()));
        }
        else
        {
            _sequence.AppendInterval(0.35f)
                .AppendCallback(() => _exitButton.gameObject.SetActive(true))
                .Append(_exitButton.transform.DOScale(Vector3.one, _exitPopDuration).SetEase(Ease.OutBack, 1.7f))
                .OnComplete(() => _exitButton.interactable = true);
        }
    }

    private IEnumerator TransitionToScoreAfterTombSound()
    {
        // 클립 길이를 추정하지 않고 실제 효과음 재생이 끝날 때까지 기다린다.
        while (_tombSoundHandle.IsPlaying)
            yield return null;

        yield return new WaitForSecondsRealtime(_gameOverHoldDuration);

        _scoreTransitionRoutine = null;
        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .Append(_gameOverText.DOFade(0f, _scoreFadeOutDuration))
            .Join(_tombCanvasGroup.DOFade(0f, _scoreFadeOutDuration))
            .AppendCallback(() => _gameOverRoot.SetActive(false))
            .OnComplete(PlayScoreSequence);
    }

    private Sequence CreateTombDropSequence()
    {
        SoundHandle bgm = default;
        return DOTween.Sequence()
            .Append(_tombImage.DOAnchorPosY(_tombTargetY, _tombDropDuration).SetEase(Ease.InQuad))
            .Join(_tombCanvasGroup.DOFade(1f, Mathf.Min(_tombFadeDuration, _tombDropDuration)).SetEase(Ease.OutSine))
            // Tomb의 시작 구간은 음원 타격 지점(0.4초)으로 맞춰 착지와 동시에 소리가 난다.
            .AppendCallback(() => _tombSoundHandle = SoundManager.Play(GameSoundId.Tomb))
            .Append(_tombImage.DOScale(new Vector3(1.08f, 0.92f, 1f), _tombImpactDuration).SetEase(Ease.OutQuad))
            .Append(_tombImage.DOScale(Vector3.one, _tombImpactDuration * 2f).SetEase(Ease.OutBack))
            // 착지 직전 음악만 낮춰 타격음을 살리고, 잔향 중에 원래 음량으로 복원한다.
            .InsertCallback(Mathf.Max(0f, _tombDropDuration - 0.1f), () =>
            {
                bgm = SoundManager.PlayBgm(GameBgmId.GameEnd);
                bgm.FadeTo(0.5f, 0.08f);
            })
            .InsertCallback(_tombDropDuration + 0.3f, () => bgm.FadeTo(1f, 0.7f));
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

    private void ShowBestResult()
    {
        _bestScoreRoot.SetActive(!_isNewBest);
        if (_isNewBest)
        {
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
