using System;
using UnityEngine;

public class PausePresenter : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private PauseView _view;

    private InputReader _inputReader;
    private PlayerController _player;
    private bool _isSubscribed;

    public event Action<bool> OnPauseStateChanged;

    // 컨트롤러가 비활성화될 때 화면과 입력 잠금도 함께 정리한다.
    public void ResetPause()
    {
        ClosePause();
    }

    private void OnEnable()
    {
        LinkEventsAll();
    }

    private void OnDisable()
    {
        UnlinkEventsAll();
        ClosePause();
    }

    public void Initialize(InputReader inputReader, PlayerController player)
    {
        if (inputReader == null)
        {
            throw new ArgumentNullException(nameof(inputReader));
        }

        if (player == null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        if (_view == null)
        {
            throw new InvalidOperationException(
                $"{name}: PauseView가 연결되지 않았습니다.");
        }

        UnlinkEventsAll();

        // 재초기화 전에 이전 플레이어의 잠금을 해제합니다.
        ClosePause();

        _inputReader = inputReader;
        _player = player;

        _view.Hide();
        LinkEventsAll();
    }

    private void LinkEventsAll()
    {
        if (_inputReader == null || _isSubscribed || !isActiveAndEnabled) return;

        _inputReader.OnPauseToggled += HandlePauseToggled;
        _view.OnResumeRequested += HandleResumeRequested;

        _isSubscribed = true;
    }

    private void UnlinkEventsAll()
    {
        if (!_isSubscribed) return;

        _inputReader.OnPauseToggled -= HandlePauseToggled;
        _view.OnResumeRequested -= HandleResumeRequested;

        _isSubscribed = false;
    }

    private void HandlePauseToggled()
    {
        if (_view.IsOpen)
        {
            ClosePause();
        }
        else
        {
            OpenPause();
        }
    }

    private void HandleResumeRequested()
    {
        ClosePause();
    }

    private void OpenPause()
    {
        if (_view.IsOpen) return;

        _player.AcquireGamePlayInputLock(this);
        _view.Show();
        OnPauseStateChanged?.Invoke(true);
    }

    private void ClosePause()
    {
        bool wasOpen = _view != null && _view.IsOpen;
        _player?.ReleaseGamePlayInputLock(this);

        if (_view != null)
        {
            _view.Hide();
        }

        if (wasOpen)
        {
            OnPauseStateChanged?.Invoke(false);
        }
    }

    private void OnDestroy()
    {
        UnlinkEventsAll();
        ClosePause();

        _inputReader = null;
        _player = null;
    }
}
