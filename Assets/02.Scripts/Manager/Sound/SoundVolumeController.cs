using UnityEngine;
using UnityEngine.Audio;

// Mixer 연결과 사용자 설정 볼륨을 담당한다.
internal sealed class SoundVolumeController
{
    private const string MixerResourcePath = "MainAudioMixer";
    private const string MasterVolumeParameter = "MasterVolume";
    private const string BgmVolumeParameter = "BgmVolume";
    private const string SfxVolumeParameter = "SfxVolume";
    private const float MinDecibel = -80f;

    private readonly AudioMixer _mixer;
    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;

    public float MasterVolume => _masterVolume;
    public float BgmVolume => _bgmVolume;
    public float SfxVolume => _sfxVolume;

    public SoundVolumeController(AudioMixer mixer)
    {
        _mixer = mixer;
        if (_mixer == null)
        {
            _mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        }
        if (_mixer == null)
        {
            Debug.LogWarning($"Resources/{MixerResourcePath} AudioMixer를 찾을 수 없어 AudioSource 볼륨으로 대신 조절함.");
        }

        ApplyMixerVolumes();
    }

    // Bgm과 Sfx는 Master 바로 아래, 나머지 효과음 채널은 Sfx 아래에 있다.
    public AudioMixerGroup FindGroup(SoundChannel channel)
    {
        if (_mixer == null)
        {
            return null;
        }

        string path = channel == SoundChannel.Bgm || channel == SoundChannel.Sfx
            ? $"Master/{channel}"
            : $"Master/Sfx/{channel}";
        AudioMixerGroup[] groups = _mixer.FindMatchingGroups(path);
        if (groups.Length == 0)
        {
            Debug.LogWarning($"{_mixer.name} : {path} 그룹이 없어 Master 그룹으로 출력함.");
            return null;
        }

        return groups[0];
    }

    // 0~1 볼륨을 믹서의 데시벨 값으로 바꿔 넣는다. 믹서가 없으면 아무것도 하지 않는다(소리 볼륨에 직접 곱한다).
    private static float ToDecibel(float volume)
    {
        return volume <= 0.0001f ? MinDecibel : Mathf.Log10(volume) * 20f;
    }

    private void ApplyMixerVolumes()
    {
        if (_mixer == null)
        {
            return;
        }

        _mixer.SetFloat(MasterVolumeParameter, ToDecibel(_masterVolume));
        _mixer.SetFloat(BgmVolumeParameter, ToDecibel(_bgmVolume));
        _mixer.SetFloat(SfxVolumeParameter, ToDecibel(_sfxVolume));
    }

    // 믹서가 볼륨을 맡으면 소리 자체에는 곱하지 않는다.
    public float GetSoftwareVolume(bool isBgm)
    {
        return _mixer != null ? 1f : (isBgm ? _bgmVolume : _sfxVolume) * _masterVolume;
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        ApplyMixerVolumes();
    }

    public void SetBgmVolume(float volume)
    {
        _bgmVolume = Mathf.Clamp01(volume);
        ApplyMixerVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        ApplyMixerVolumes();
    }
}
