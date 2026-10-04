using System;
using UnityEngine;

public sealed class ThirstFeedbackPresenter : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private VignetteEffectView _vignetteView;

    [SerializeField] private CharacterDialogueView _dialogView;

    [Header("Scene Effects")]
    [SerializeField] private SceneVisualEffectManager _sceneVisualEffectManager;

    private PlayerStatus _playerStatus;
    private PlayerDialogueModule _dialogueModule;

    private bool _isInitialized;
    private bool _isSubscribed;

    public void Initialize(PlayerStatus playerStatus, PlayerDialogueModule dialogueModule)
    {
        if (playerStatus == null)
        {
            throw new ArgumentNullException(nameof(playerStatus));
        }

        if (dialogueModule == null)
        {
            throw new ArgumentNullException(nameof(dialogueModule));
        }

        ValidateReferences();
        UnlinkEvents();

        _playerStatus = playerStatus;
        _dialogueModule = dialogueModule;

        _vignetteView.Initialize();
        _dialogView.Initialize();

        ApplyThirstLevel(_playerStatus.CurrentThirstLevel);

        _isInitialized = true;

        if (isActiveAndEnabled)
        {
            LinkEvents();
        }
    }

    private void HandleThirstLevelChanged(ThirstWarningLevel previousStage, ThirstWarningLevel currentStage)
    {
        ApplyThirstLevel(currentStage);
    }

    private void HandleDialogueRequested(string dialogue)
    {
        if (string.IsNullOrWhiteSpace(dialogue))
        {
            return;
        }

        _dialogView.Play(dialogue);
    }

    private void ApplyThirstLevel(ThirstWarningLevel level)
    {
        bool shouldShowVignette = level >= ThirstWarningLevel.Warning;
        bool shouldEnableWobble = level == ThirstWarningLevel.Critical;

        _vignetteView.SetVisible(shouldShowVignette);

        _sceneVisualEffectManager.SetWobbleActive(shouldEnableWobble);
    }

    private void LinkEvents()
    {
        if (_isSubscribed || _playerStatus == null || _dialogueModule == null)
        {
            return;
        }

        _playerStatus.OnThirstLevelChanged += HandleThirstLevelChanged;
        _dialogueModule.OnDialogueRequested += HandleDialogueRequested;

        _isSubscribed = true;
    }

    private void UnlinkEvents()
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (_playerStatus != null)
        {
            _playerStatus.OnThirstLevelChanged -= HandleThirstLevelChanged;
        }

        if (_dialogueModule != null)
        {
            _dialogueModule.OnDialogueRequested -= HandleDialogueRequested;
        }

        _isSubscribed = false;
    }

    private void ResetFeedback()
    {
        if (_vignetteView != null)
        {
            _vignetteView.SetVisible(false);
        }

        if (_dialogView != null)
        {
            _dialogView.HideImmediately();
        }

        if (_sceneVisualEffectManager != null)
        {
            _sceneVisualEffectManager.SetWobbleActive(false);
        }
    }

    private void ValidateReferences()
    {
        if (_vignetteView == null)
        {
            throw new MissingReferenceException($"{nameof(VignetteEffectView)}가 연결되지 않았습니다.");
        }

        if (_dialogView == null)
        {
            throw new MissingReferenceException($"{nameof(CharacterDialogueView)}가 연결되지 않았습니다.");
        }

        if (_sceneVisualEffectManager == null)
        {
            _sceneVisualEffectManager = FindAnyObjectByType<SceneVisualEffectManager>();
            if(_sceneVisualEffectManager == null)
            {
                throw new MissingReferenceException($"{nameof(SceneVisualEffectManager)}가 연결되지 않았습니다.");
            }
        }
    }

    private void OnEnable()
    {
        if (!_isInitialized || _playerStatus == null || _dialogueModule == null)
        {
            return;
        }

        ApplyThirstLevel(_playerStatus.CurrentThirstLevel);
        LinkEvents();
    }

    private void OnDisable()
    {
        UnlinkEvents();
        ResetFeedback();
    }

    private void OnDestroy()
    {
        UnlinkEvents();
    }
}