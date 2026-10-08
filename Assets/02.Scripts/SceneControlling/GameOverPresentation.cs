using System;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameOverPresentation : MonoBehaviour
{
    [Header("Score Sequence")]
    [SerializeField] private TMP_Text _floorText;
    [SerializeField] private TMP_Text _enemyKillText;
    [SerializeField] private TMP_Text _potionText;

    [SerializeField] private CanvasGroup _floorRow;
    [SerializeField] private CanvasGroup _enemyRow;
    [SerializeField] private CanvasGroup _potionRow;
    [SerializeField] private CanvasGroup _pressAnyKey;

    [Header("Game Over Sequence")]
    [SerializeField] private CanvasGroup _gameOverText;
    [SerializeField] private RectTransform _tombImage;
    [SerializeField] private Button _exitButton;

    [Header("Timing")]
    [SerializeField] private float _rowFadeDuration = 0.35f;
    [SerializeField] private float _rowInterval = 0.15f;
    [SerializeField] private float _tombDuration = 1.5f;
    [SerializeField] private float _tombStartOffsetY = 700f;

    private Sequence _sequence;
    private IDisposable _inputSubscription;
    private float _tombTargetY;

    private void Awake()
    {
        ApplyScore();
        InitializePresentation();
    }

    private void Start()
    {
        PlayScoreSequence();
    }

    private void ApplyScore()
    {
        ScoreData score = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScoreSnapshot() : new ScoreData();

        _floorText.text = score.ClearedFloorCount.ToString();
        _enemyKillText.text = score.TotalKillCount.ToString();
        _potionText.text = score.ConsumedPotionCount.ToString();
    }

    private void InitializePresentation()
    {
        SetAlpha(_floorRow, 0f);
        SetAlpha(_enemyRow, 0f);
        SetAlpha(_potionRow, 0f);
        SetAlpha(_pressAnyKey, 0f);
        SetAlpha(_gameOverText, 0f);

        _exitButton.gameObject.SetActive(false);

        _tombTargetY = _tombImage.anchoredPosition.y;
        _tombImage.anchoredPosition += Vector2.up * _tombStartOffsetY;
    }

    private void PlayScoreSequence()
    {
        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)

            .Append(_floorRow.DOFade(1f, _rowFadeDuration))
            .AppendInterval(_rowInterval)

            .Append(_enemyRow.DOFade(1f, _rowFadeDuration))
            .AppendInterval(_rowInterval)

            .Append(_potionRow.DOFade(1f, _rowFadeDuration))
            .AppendInterval(_rowInterval)

            .Append(_pressAnyKey.DOFade(1f, _rowFadeDuration))
            .OnComplete(BeginWaitingForInput);
    }

    private void BeginWaitingForInput()
    {
        _inputSubscription = InputSystem.onAnyButtonPress.CallOnce(_ => PlayGameOverSequence());
    }

    private void PlayGameOverSequence()
    {
        _inputSubscription?.Dispose();
        _inputSubscription = null;

        _sequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)

            .Append(_pressAnyKey.DOFade(0f, 0.2f))
            .Append(_gameOverText.DOFade(1f, 0.4f))

            // GAME OVER가 나타나면서 비석이 내려옴
            .Join(_tombImage
                    .DOAnchorPosY(_tombTargetY, _tombDuration)
                    .SetEase(Ease.OutCubic))

            .AppendInterval(0.3f)
            .AppendCallback(() =>
            {
                _exitButton.gameObject.SetActive(true);
            });
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        group.alpha = alpha;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void OnDisable()
    {
        _inputSubscription?.Dispose();
        _inputSubscription = null;

        _sequence?.Kill();
        transform.DOKill();
    }
}