using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameOverSoundController : MonoBehaviour
{
    [SerializeField] private GameSoundId _buttonClickSoundId = GameSoundId.TitleClick;
    [SerializeField] private GameSoundId _buttonHoverSoundId = GameSoundId.TitleHover;

    private PauseButtonSound _exitButtonSound;
    private float _nextHoverSoundTime;

    public void Initialize(Button exitButton)
    {
        if (exitButton == null) return;

        // 다시 초기화되는 경우 이전 버튼의 이벤트 연결이 남지 않도록 먼저 해제합니다.
        if (_exitButtonSound != null)
            _exitButtonSound.OnFocused -= PlayButtonHoverSound;

        // 호버와 키보드 선택 감지는 기존 공통 버튼 컴포넌트를 재사용합니다.
        _exitButtonSound = exitButton.GetComponent<PauseButtonSound>();
        if (_exitButtonSound == null)
            _exitButtonSound = exitButton.gameObject.AddComponent<PauseButtonSound>();

        _exitButtonSound.OnFocused += PlayButtonHoverSound;
    }

    public void PlayButtonClickSound()
    {
        SoundManager.Play(_buttonClickSoundId);
    }

    private void PlayButtonHoverSound()
    {
        if (!isActiveAndEnabled) return;

        GameSoundData.Entry sound = SoundManager.GetSound(_buttonHoverSoundId);

        // 클립이 없거나 최소 재생 간격이 지나지 않았으면 호버음을 재생하지 않습니다.
        if (sound?.Clip == null || Time.unscaledTime < _nextHoverSoundTime) return;

        _nextHoverSoundTime = Time.unscaledTime + sound.Interval;
        SoundManager.Play(_buttonHoverSoundId);
    }

    private void OnDestroy()
    {
        // 이벤트 구독을 해제해 씬 전환 뒤에도 참조가 남지 않게 합니다.
        if (_exitButtonSound != null)
            _exitButtonSound.OnFocused -= PlayButtonHoverSound;
    }
}