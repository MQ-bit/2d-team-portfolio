using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseSoundController : MonoBehaviour
{
    [Header("UI Sound")]
    [SerializeField] private AudioClip _buttonClickSound;
    [SerializeField, Range(0f, 1f)] private float _uiSoundVolume = 1f;

    [Header("Button Hover Sound")]
    [SerializeField] private AudioClip _buttonHoverSound;
    [SerializeField, Range(0f, 1f)] private float _buttonHoverSoundVolume = 0.35f;
    [SerializeField, Min(0f)] private float _buttonHoverSoundInterval = 0.08f;

    [Header("Volume Feedback Sound")]
    [SerializeField] private AudioClip _volumeFeedbackSound;
    [SerializeField, Min(0f)] private float _volumeFeedbackInterval = 0.1f;

    private PauseView _view;
    private float _nextVolumeFeedbackTime;
    private float _nextButtonHoverSoundTime;
    private bool _suppressButtonHoverSound;
    private readonly List<PauseButtonSound> _buttonSounds = new List<PauseButtonSound>();

    public void Initialize(PauseView view, QuitConfirmView quitConfirmView)
    {
        UnlinkButtonSounds();
        _view = view;
        AddButtonSounds(view);
        AddButtonSounds(quitConfirmView);
    }

    private void AddButtonSounds(Component root)
    {
        if (root == null) return;
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            PauseButtonSound sound = button.GetComponent<PauseButtonSound>();
            if (sound == null) sound = button.gameObject.AddComponent<PauseButtonSound>();
            if (_buttonSounds.Contains(sound)) continue;
            sound.OnFocused += HandleButtonFocused;
            _buttonSounds.Add(sound);
        }
    }

    private void HandleButtonFocused()
    {
        if (!isActiveAndEnabled || _view == null || !_view.IsOpen || _suppressButtonHoverSound
            || _buttonHoverSound == null || Time.unscaledTime < _nextButtonHoverSoundTime)
        {
            return;
        }

        _nextButtonHoverSoundTime = Time.unscaledTime + _buttonHoverSoundInterval;
        SoundManager.PlayUi(_buttonHoverSound, _buttonHoverSoundVolume);
    }

    // 화면을 열거나 돌아올 때 자동 선택되는 버튼은 소리를 내지 않는다.
    public void RunWithoutHoverSound(Action select)
    {
        bool wasSuppressed = _suppressButtonHoverSound;
        _suppressButtonHoverSound = true;
        try
        {
            select();
        }
        finally
        {
            _suppressButtonHoverSound = wasSuppressed;
        }
    }

    public void PlayClickSound()
    {
        PlayUiSound(_buttonClickSound);
    }

    public void PlayVolumeFeedbackSound()
    {
        if (!isActiveAndEnabled || _volumeFeedbackSound == null
            || Time.unscaledTime < _nextVolumeFeedbackTime)
        {
            return;
        }

        _nextVolumeFeedbackTime = Time.unscaledTime + _volumeFeedbackInterval;
        PlayUiSound(_volumeFeedbackSound);
    }

    private void PlayUiSound(AudioClip clip)
    {
        if (!isActiveAndEnabled || clip == null) return;
        SoundManager.PlayUi(clip, _uiSoundVolume);
    }

    private void UnlinkButtonSounds()
    {
        foreach (PauseButtonSound sound in _buttonSounds)
        {
            if (sound != null) sound.OnFocused -= HandleButtonFocused;
        }
        _buttonSounds.Clear();
    }

    private void OnDestroy()
    {
        UnlinkButtonSounds();
    }
}
