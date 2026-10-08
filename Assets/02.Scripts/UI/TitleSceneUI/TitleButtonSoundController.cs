using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TitleScene의 Canvas 아래 모든 버튼에 호버 효과음을 연결합니다.
/// 비활성화된 튜토리얼/설정 창의 버튼도 미리 찾아 연결합니다.
/// </summary>
public class TitleButtonSoundController : MonoBehaviour
{
    [SerializeField] private Transform _buttonRoot;

    [SerializeField] private AudioClip _buttonHoverSound;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.35f;

    [SerializeField, Min(0f)] private float _soundInterval = 0.08f;

    private readonly List<PauseButtonSound> _connectedButtons = new();
    private float _nextSoundTime;

    private void Awake()
    {
        if (_buttonRoot == null)
        {
            Debug.LogError(
                "TitleButtonHoverSoundController의 Button Root에 Canvas를 연결하세요.",
                this);
            enabled = false;
            return;
        }

        // true를 전달하면 비활성화된 튜토리얼/옵션 창 안의 버튼도 검색합니다.
        Button[] buttons = _buttonRoot.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            // 기존 인게임 UI가 사용하는 포인터/선택 감지 컴포넌트를 재사용합니다.
            PauseButtonSound buttonSound =
                button.GetComponent<PauseButtonSound>();

            if (buttonSound == null)
                buttonSound = button.gameObject.AddComponent<PauseButtonSound>();

            // 같은 버튼에 이벤트가 중복 등록되지 않도록 확인합니다.
            if (_connectedButtons.Contains(buttonSound))
                continue;

            buttonSound.OnFocused += PlayHoverSound;
            _connectedButtons.Add(buttonSound);
        }
    }

    private void PlayHoverSound()
    {
        if (!isActiveAndEnabled || _buttonHoverSound == null)
            return;

        if (Time.unscaledTime < _nextSoundTime)
            return;

        _nextSoundTime = Time.unscaledTime + _soundInterval;
        SoundManager.PlayUi(_buttonHoverSound, _volume);
    }

    private void OnDestroy()
    {
        // 씬이 종료될 때 이벤트 연결을 해제합니다.
        foreach (PauseButtonSound buttonSound in _connectedButtons)
        {
            if (buttonSound != null)
                buttonSound.OnFocused -= PlayHoverSound;
        }

        _connectedButtons.Clear();
    }
}