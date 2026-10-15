using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseSoundController : MonoBehaviour
{
    [SerializeField] private GameSoundId _clickSoundId = GameSoundId.PauseClick;

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
            || SoundManager.GetSound(GameSoundId.PauseHover)?.Clip == null || Time.unscaledTime < _nextButtonHoverSoundTime)
        {
            return;
        }

        _nextButtonHoverSoundTime = Time.unscaledTime + SoundManager.GetSound(GameSoundId.PauseHover).Interval;
        SoundManager.Play(GameSoundId.PauseHover);
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
        PlayConfiguredUiSound(_clickSoundId);
    }

    public void PlayVolumeFeedbackSound()
    {
        if (!isActiveAndEnabled || SoundManager.GetSound(GameSoundId.VolumeFeedback)?.Clip == null
            || Time.unscaledTime < _nextVolumeFeedbackTime)
        {
            return;
        }

        _nextVolumeFeedbackTime = Time.unscaledTime + SoundManager.GetSound(GameSoundId.VolumeFeedback).Interval;
        PlayConfiguredUiSound(GameSoundId.VolumeFeedback);
    }

    private void PlayConfiguredUiSound(GameSoundId id)
    {
        if (!isActiveAndEnabled) return;
        SoundManager.Play(id);
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
