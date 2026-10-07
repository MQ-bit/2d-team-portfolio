using System;
using UnityEngine;

public class PauseVolumeView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VolumeControlView _masterVolume;
    [SerializeField] private VolumeControlView _bgmVolume;
    [SerializeField] private VolumeControlView _sfxVolume;

    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnBgmVolumeChanged;
    public event Action<float> OnSfxVolumeChanged;

    public float MasterVolume => _masterVolume.Value;
    public float BgmVolume => _bgmVolume.Value;
    public float SfxVolume => _sfxVolume.Value;

    private void OnEnable()
    {
        _masterVolume.OnValueChanged += HandleMasterVolumeChanged;
        _bgmVolume.OnValueChanged += HandleBgmVolumeChanged;
        _sfxVolume.OnValueChanged += HandleSfxVolumeChanged;
    }

    private void OnDisable()
    {
        _masterVolume.OnValueChanged -= HandleMasterVolumeChanged;
        _bgmVolume.OnValueChanged -= HandleBgmVolumeChanged;
        _sfxVolume.OnValueChanged -= HandleSfxVolumeChanged;
    }

    public void SetVolumes(float master, float bgm, float sfx)
    {
        _masterVolume.SetValue(master);
        _bgmVolume.SetValue(bgm);
        _sfxVolume.SetValue(sfx);
    }

    public void SetInteractable(bool interactable)
    {
        _masterVolume.SetInteractable(interactable);
        _bgmVolume.SetInteractable(interactable);
        _sfxVolume.SetInteractable(interactable);
    }

    private void HandleMasterVolumeChanged(float value)
    {
        OnMasterVolumeChanged?.Invoke(value);
    }

    private void HandleBgmVolumeChanged(float value)
    {
        OnBgmVolumeChanged?.Invoke(value);
    }

    private void HandleSfxVolumeChanged(float value)
    {
        OnSfxVolumeChanged?.Invoke(value);
    }
}