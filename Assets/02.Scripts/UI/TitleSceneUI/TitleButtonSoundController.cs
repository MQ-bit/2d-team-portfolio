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

    private readonly List<Button> _clickConnectedButtons = new();


    private readonly List<PauseButtonSound> _connectedButtons = new();
    private float _nextSoundTime;

    private void Awake()
    {
        if (_buttonRoot == null)
        {
            Debug.LogError(
                "TitleButtonSoundController의 Button Root에 Canvas를 연결하세요.",
                this);
            enabled = false;
            return;
        }

        // true를 전달하면 비활성화된 튜토리얼/옵션 창 안의 버튼도 검색합니다.
        Button[] buttons = _buttonRoot.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            // 호버 효과음용 이벤트 연결
            PauseButtonSound buttonSound =
                button.GetComponent<PauseButtonSound>();

            if (buttonSound == null)
                buttonSound = button.gameObject.AddComponent<PauseButtonSound>();

            if (!_connectedButtons.Contains(buttonSound))
            {
                buttonSound.OnFocused += PlayHoverSound;
                _connectedButtons.Add(buttonSound);
            }

            // 클릭 효과음용 이벤트 연결
            if (!_clickConnectedButtons.Contains(button))
            {
                button.onClick.AddListener(PlayClickSound);
                _clickConnectedButtons.Add(button);
            }
        }
    }

    private void PlayHoverSound()
    {
        if (!isActiveAndEnabled || SoundManager.GetSound(GameSoundId.TitleHover)?.Clip == null)
            return;

        if (Time.unscaledTime < _nextSoundTime)
            return;

        _nextSoundTime = Time.unscaledTime + SoundManager.GetSound(GameSoundId.TitleHover).Interval;
        SoundManager.Play(GameSoundId.TitleHover);
    }

    private void PlayClickSound()
    {
        if (!isActiveAndEnabled || SoundManager.GetSound(GameSoundId.TitleClick)?.Clip == null)
            return;

        SoundManager.Play(GameSoundId.TitleClick);
    }

    private void OnDestroy()
    {
        // 씬이 종료될 때 이벤트 연결을 해제합니다.
        foreach (PauseButtonSound buttonSound in _connectedButtons)
        {
            if (buttonSound != null)
                buttonSound.OnFocused -= PlayHoverSound;
        }

        foreach (Button button in _clickConnectedButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayClickSound);
        }

        _clickConnectedButtons.Clear();

        _connectedButtons.Clear();
    }
}