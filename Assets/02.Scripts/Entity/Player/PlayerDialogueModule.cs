using System;
using UnityEngine;

public sealed class PlayerDialogueModule : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField]
    private ThirstDialogueData _thirstDialogueData;

    private PlayerStatus _playerStatus;
    private bool _isInitialized;
    private bool _isSubscribed;

    public event Action<string> OnDialogueRequested;

    public void Initialize(PlayerStatus playerStatus)
    {
        if (playerStatus == null)
        {
            throw new ArgumentNullException(nameof(playerStatus));
        }

        UnlinkEvents();

        _playerStatus = playerStatus;
        _isInitialized = true;

        if (isActiveAndEnabled)
        {
            LinkEvents();
        }
    }

    private void HandleThirstStageChanged(ThirstWarningLevel previousLevel, ThirstWarningLevel currentLevel)
    {
        if (currentLevel <= previousLevel)
        {
            return;
        }

        RequestThirstDialogue(currentLevel);
    }

    private void RequestThirstDialogue(ThirstWarningLevel level)
    {
        if (_thirstDialogueData == null)
        {
            Debug.LogWarning($"{nameof(PlayerDialogueModule)}: {nameof(ThirstDialogueData)}가 연결되지 않았습니다.");

            return;
        }

        string dialogue = _thirstDialogueData.GetRandomLine(level);

        if (string.IsNullOrWhiteSpace(dialogue))
        {
            Debug.LogWarning($"{nameof(PlayerDialogueModule)}: {level} 단계에 사용할 대사가 없습니다.");

            return;
        }

        OnDialogueRequested?.Invoke(dialogue);
    }

    private void LinkEvents()
    {
        if (_playerStatus == null || _isSubscribed)
        {
            return;
        }

        _playerStatus.OnThirstLevelChanged += HandleThirstStageChanged;

        _isSubscribed = true;
    }

    private void UnlinkEvents()
    {
        if (_playerStatus == null || !_isSubscribed)
        {
            return;
        }

        _playerStatus.OnThirstLevelChanged -= HandleThirstStageChanged;

        _isSubscribed = false;
    }

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            return;
        }

        LinkEvents();
    }

    private void OnDisable()
    {
        UnlinkEvents();
    }

    private void OnDestroy()
    {
        UnlinkEvents();
    }
}