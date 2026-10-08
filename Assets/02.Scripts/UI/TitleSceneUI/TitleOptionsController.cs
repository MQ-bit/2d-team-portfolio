using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬의 옵션 창을 열고 닫으며, 볼륨 UI의 변경 내용을 SoundManager에 전달합니다.
/// PausePresenter는 플레이어와 게임 일시정지 입력에 의존하므로 타이틀 씬에서는 사용하지 않습니다.
/// </summary>
public class TitleOptionsController : MonoBehaviour
{
    [SerializeField] private GameObject _optionWindowSet;
    [SerializeField] private PauseVolumeView _volumeView;
    [SerializeField] private Button _escButton;

    private bool _isOpen;
    private Image _escButtonImage;
    private Sprite _escButtonNormalSprite;

    private void Awake()
    {
        // 옵션 창이 시작부터 보이거나 타이틀 버튼 입력을 가로채지 않도록 숨깁니다.
        if (_optionWindowSet == null)
        {
            Debug.LogError("TitleOptionsController에 Option Window Set을 연결하세요.", this);
            enabled = false;
            return;
        }

        if (_volumeView == null)
        {
            Debug.LogError("TitleOptionsController에 Pause Volume View를 연결하세요.", this);
            enabled = false;
            return;
        }

        // 키보드 Esc를 눌렀을 때 바꿀 버튼 이미지와 원래 스프라이트를 저장합니다.
        if (_escButton != null)
        {
            _escButtonImage = _escButton.targetGraphic as Image;

            if (_escButtonImage != null)
                _escButtonNormalSprite = _escButtonImage.sprite;
        }

        _optionWindowSet.SetActive(false);
    }

    private void OnEnable()
    {
        // PauseVolumeView가 각 볼륨 컨트롤에서 받아온 변경 이벤트를 구독합니다.
        if (_volumeView == null)
            return;

        _volumeView.OnMasterVolumeChanged += HandleMasterVolumeChanged;
        _volumeView.OnBgmVolumeChanged += HandleBgmVolumeChanged;
        _volumeView.OnSfxVolumeChanged += HandleSfxVolumeChanged;
    }

    private void OnDisable()
    {
        // 중복 호출을 막기 위해 오브젝트가 꺼질 때 이벤트 구독을 해제합니다.
        if (_volumeView == null)
            return;

        _volumeView.OnMasterVolumeChanged -= HandleMasterVolumeChanged;
        _volumeView.OnBgmVolumeChanged -= HandleBgmVolumeChanged;
        _volumeView.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
    }

    private void Update()
    {
        // Esc 키를 누른 프레임에 버튼을 눌림 스프라이트로 바꿉니다.
        if (!_isOpen || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            SetEscPressedSprite(true);

        // Esc 키를 놓은 프레임에 원래 모양으로 되돌린 뒤 옵션 창을 닫습니다.
        if (Keyboard.current.escapeKey.wasReleasedThisFrame)
        {
            SetEscPressedSprite(false);
            CloseOptions();
        }
    }

    /// <summary>타이틀 씬의 옵션 버튼에서 호출합니다.</summary>
    public void OpenOptions()
    {
        if (_optionWindowSet == null || _volumeView == null)
            return;

        // 이전 입력의 눌림 모양이 남지 않도록 기본 스프라이트로 초기화합니다.
        SetEscPressedSprite(false);
        _isOpen = true;
        _optionWindowSet.SetActive(true);

        // 창을 열 때 SoundManager의 현재 볼륨 값을 UI에 표시합니다.
        // PauseVolumeView.SetVolumes는 슬라이더의 이벤트를 발생시키지 않고 값을 설정합니다.
        _volumeView.SetVolumes(
            SoundManager.GetMasterVolume(),
            SoundManager.GetBgmVolume(),
            SoundManager.GetSfxVolume());
    }

    /// <summary>옵션 창의 Esc/확인/닫기 버튼에서 호출합니다.</summary>
    public void CloseOptions()
    {
        SetEscPressedSprite(false);
        _isOpen = false;

        if (_optionWindowSet != null)
            _optionWindowSet.SetActive(false);
    }

    // Button의 Sprite Swap 설정에 있는 Pressed Sprite를 직접 표시합니다.
    // Unity Button의 기본 Sprite Swap은 키보드 Esc 입력에 자동으로 반응하지 않습니다.
    private void SetEscPressedSprite(bool pressed)
    {
        if (_escButton == null || _escButtonImage == null)
            return;

        Sprite pressedSprite = _escButton.spriteState.pressedSprite;

        if (pressed && pressedSprite != null)
            _escButtonImage.sprite = pressedSprite;
        else
            _escButtonImage.sprite = _escButtonNormalSprite;
    }

    // PauseVolumeView에서 전달된 값을 전역 사운드 설정에 적용합니다.
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
}
