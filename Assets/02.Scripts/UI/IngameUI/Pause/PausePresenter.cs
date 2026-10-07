using System;
using UnityEngine;

public class PausePresenter : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private PauseView _view;
    [SerializeField] private QuitConfirmView _quitConfirmView;
    [SerializeField] private PauseVolumeView _volumeView;

    private InputReader _inputReader;
    private PlayerController _player;
    private bool _isSubscribed;

    public event Action<bool> OnPauseStateChanged;
    public event Action OnQuitConfirmed;

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

        if (_volumeView == null)
        {
            throw new InvalidOperationException(
                $"{name}: PauseVolumeView가 연결되지 않았습니다.");
        }

        UnlinkEventsAll();

        ClosePause();

        _inputReader = inputReader;
        _player = player;

        _view.Hide();

        if (_quitConfirmView != null)
        {
            _quitConfirmView.Hide();
        }

        // SoundManager의 현재 볼륨을 UI에 반영
        _volumeView.SetVolumes(
            SoundManager.GetMasterVolume(),
            SoundManager.GetBgmVolume(),
            SoundManager.GetSfxVolume()
        );

        LinkEventsAll();
    }

    private void LinkEventsAll()
    {
        if (_inputReader == null || _isSubscribed || !isActiveAndEnabled)
        {
            return;
        }

        _inputReader.OnPauseToggled += HandlePauseToggled;

        _view.OnResumeRequested += HandleResumeRequested;
        _view.OnMainMenuRequested += HandleMainMenuRequested;

        _volumeView.OnMasterVolumeChanged += HandleMasterVolumeChanged;
        _volumeView.OnBgmVolumeChanged += HandleBgmVolumeChanged;
        _volumeView.OnSfxVolumeChanged += HandleSfxVolumeChanged;

        if (_quitConfirmView != null)
        {
            _quitConfirmView.OnQuitRequested += HandleQuitRequested;
            _quitConfirmView.OnCancelRequested += HandleCancelRequested;
        }

        _isSubscribed = true;
    }

    private void UnlinkEventsAll()
    {
        if (!_isSubscribed)
        {
            return;
        }

        _inputReader.OnPauseToggled -= HandlePauseToggled;

        _view.OnResumeRequested -= HandleResumeRequested;
        _view.OnMainMenuRequested -= HandleMainMenuRequested;

        _volumeView.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
        _volumeView.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
        _volumeView.OnSfxVolumeChanged -= HandleSfxVolumeChanged;

        if (_quitConfirmView != null)
        {
            _quitConfirmView.OnQuitRequested -= HandleQuitRequested;
            _quitConfirmView.OnCancelRequested -= HandleCancelRequested;
        }

        _isSubscribed = false;
    }

    private void HandlePauseToggled()
    {
        if (_quitConfirmView != null && _quitConfirmView.IsOpen)
        {
            HandleCancelRequested();
            return;
        }

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
        if (_quitConfirmView != null && _quitConfirmView.IsOpen) return;
        ClosePause();
    }

    private void HandleMainMenuRequested()
    {
        if (!_view.IsOpen) return;
        if (_quitConfirmView == null)
        {
            Debug.LogError($"{name}: QuitConfirmView가 연결되지 않았습니다.", this);
            return;
        }

        _view.SetButtonsInteractable(false);
        _quitConfirmView.Show();
    }

    private void HandleCancelRequested()
    {
        if (_quitConfirmView == null || !_quitConfirmView.IsOpen) return;

        _quitConfirmView.Hide();
        _view.SetButtonsInteractable(true);
        _view.SelectMainMenuButton();
    }

    private void HandleQuitRequested()
    {
        if (!_view.IsOpen || _quitConfirmView == null || !_quitConfirmView.IsOpen) return;

        // 시간과 포션 정보창 정리, 씬 이동은 IngameUIController에서 처리한다.
        OnQuitConfirmed?.Invoke();
    }

    private void HandleMasterVolumeChanged(float value)
    {
        SoundManager.SetMasterVolume(value);
    }

    private void HandleBgmVolumeChanged(float value)
    {
        SoundManager.SetBgmVolume(value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        SoundManager.SetSfxVolume(value);
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
        if (_quitConfirmView != null)
        {
            _quitConfirmView.Hide();
        }
        _player?.ReleaseGamePlayInputLock(this);

        if (_view != null)
        {
            _view.SetButtonsInteractable(true);
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
