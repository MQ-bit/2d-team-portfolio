using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// 한 채널의 보이스 풀, 동시 재생 제한, Pause 페이드와 공통 볼륨을 관리한다.
internal sealed class SoundPlaybackChannel
{
    private readonly Transform _root;
    private readonly AudioMixerGroup _group;
    private readonly bool _isBgm;
    private readonly int _maxVoices;
    private readonly SoundVolumeController _volumeController;
    private readonly List<SoundVoice> _voices = new List<SoundVoice>();

    private bool _pauseRequested;
    private float _pauseVolume = 1f;
    private float _pauseFadeFrom;
    private float _pauseFadeTarget = 1f;
    private float _pauseFadeTime;
    private float _pauseFadeElapsed;

    private float ChannelVolume => _pauseVolume * _volumeController.GetSoftwareVolume(_isBgm);

    public SoundPlaybackChannel(SoundChannel channel, Transform rootTransform,
        SoundVolumeController volumeController)
    {
        _volumeController = volumeController;
        _root = new GameObject(channel.ToString()).transform;
        _root.SetParent(rootTransform, false);
        _group = volumeController.FindGroup(channel);
        _isBgm = channel == SoundChannel.Bgm;
        _maxVoices = GetMaxVoices(channel);
    }

    public SoundHandle Play(AudioClip clip, float volume, float startOffset,
        float endTime, float fadeOutTime, bool loop, float fadeInTime)
    {
        // Pause 중 단발음은 버리고, 반복음은 일시정지 상태로 보관한다.
        if (_pauseRequested && !loop) return default;

        SoundVoice voice = AcquireVoice();
        voice.Play(clip, volume, startOffset, endTime, fadeOutTime, loop, fadeInTime,
            ChannelVolume, _pauseRequested);
        return new SoundHandle(voice);
    }

    public void SetPaused(bool paused, float fadeTime)
    {
        _pauseRequested = paused;
        _pauseFadeFrom = _pauseVolume;
        _pauseFadeTarget = paused ? 0f : 1f;
        _pauseFadeTime = Mathf.Max(0f, fadeTime);
        _pauseFadeElapsed = 0f;
        if (_pauseFadeTime <= 0f)
        {
            _pauseVolume = _pauseFadeTarget;
        }

        float channelVolume = ChannelVolume;
        foreach (SoundVoice voice in _voices)
        {
            if (!voice.Active) continue;
            if (!paused)
            {
                voice.Resume(channelVolume);
            }
            else if (_pauseVolume <= 0f)
            {
                voice.Pause();
            }
        }
    }

    public void Update(float deltaTime)
    {
        UpdatePauseFade(deltaTime);
        float channelVolume = ChannelVolume;
        bool shouldPause = _pauseRequested && _pauseVolume <= 0f;
        foreach (SoundVoice voice in _voices)
        {
            if (voice.Active)
            {
                voice.Update(deltaTime, channelVolume, shouldPause);
            }
        }
    }

    public void FadeOut(float fadeTime)
    {
        foreach (SoundVoice voice in _voices)
        {
            if (voice.Active)
            {
                voice.StartFade(0f, fadeTime, true);
            }
        }
    }

    public void StopAll()
    {
        foreach (SoundVoice voice in _voices)
        {
            voice.StopAndClearClip();
        }
    }

    private void UpdatePauseFade(float deltaTime)
    {
        if (_pauseVolume == _pauseFadeTarget) return;
        _pauseFadeElapsed += deltaTime;
        float t = _pauseFadeTime > 0f
            ? Mathf.Clamp01(_pauseFadeElapsed / _pauseFadeTime) : 1f;
        _pauseVolume = Mathf.Lerp(_pauseFadeFrom, _pauseFadeTarget, t);
    }

    // 비활성 보이스를 재사용하고, 풀이 꽉 찼으면 가장 오래된 소리를 교체한다.
    private SoundVoice AcquireVoice()
    {
        SoundVoice oldest = null;
        foreach (SoundVoice voice in _voices)
        {
            if (!voice.Active) return voice;
            if (oldest == null || voice.StartedAt < oldest.StartedAt)
            {
                oldest = voice;
            }
        }

        if (_voices.Count < _maxVoices)
        {
            AudioSource source = _root.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = _group;
            var created = new SoundVoice(source);
            _voices.Add(created);
            return created;
        }

        oldest.Release();
        return oldest;
    }

    private static int GetMaxVoices(SoundChannel channel)
    {
        switch (channel)
        {
            case SoundChannel.Bgm: return 3;
            case SoundChannel.Ui: return 4;
            case SoundChannel.Player: return 8;
            case SoundChannel.Potion: return 8;
            default: return 16;
        }
    }
}
