using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// 재생 중인 소리 하나를 가리키는 손잡이. 소리가 끝났거나 다른 소리가 그 자리를 차지했으면 무효가 된다.
public readonly struct SoundHandle
{
    private readonly SoundVoice _voice;
    private readonly int _id;

    internal SoundHandle(SoundVoice voice)
    {
        _voice = voice;
        _id = voice.Id;
    }

    public bool IsPlaying => _voice != null && _voice.Id == _id && _voice.Active;

    // 재생 위치(초). 재생 중이 아니면 0.
    public float Time => IsPlaying ? _voice.Source.time : 0f;

    // 바로 멈춘다.
    public void Stop()
    {
        if (IsPlaying)
        {
            _voice.Release();
        }
    }

    // fadeTime초에 걸쳐 서서히 줄이다가 멈춘다.
    public void FadeOut(float fadeTime)
    {
        if (IsPlaying)
        {
            _voice.StartFade(0f, fadeTime, true);
        }
    }

    // fadeTime초에 걸쳐 볼륨을 targetVolume(0~1, 처음 정한 볼륨 대비 비율)으로 바꾼다. 멈추지는 않는다.
    public void FadeTo(float targetVolume, float fadeTime)
    {
        if (IsPlaying)
        {
            _voice.StartFade(targetVolume, fadeTime, false);
        }
    }
}

internal sealed class SoundVoice
{
    public AudioSource Source;
    public int Id;
    public bool Active;
    public float BaseVolume;
    public float EndTime;
    public float FadeOutTime;
    public float StartedAt;

    // 페이드 인/아웃. FadeValue는 현재 비율(0~1), 페이드 중이면 FadeTarget으로 FadeTime초에 걸쳐 바뀐다.
    public float FadeValue = 1f;
    public float FadeFrom;
    public float FadeTarget;
    public float FadeTime;
    public float FadeElapsed;
    public bool Fading;
    public bool StopWhenFaded;

    public void StartFade(float target, float time, bool stopWhenFaded)
    {
        target = Mathf.Clamp01(target);
        StopWhenFaded = stopWhenFaded;
        if (time <= 0f)
        {
            FadeValue = target;
            Fading = false;
            if (stopWhenFaded)
            {
                Release();
            }
            return;
        }

        FadeFrom = FadeValue;
        FadeTarget = target;
        FadeTime = time;
        FadeElapsed = 0f;
        Fading = true;
    }

    // 페이드를 한 단계 진행한다. 페이드가 끝나서 멈춰야 하면 true.
    public bool AdvanceFade(float deltaTime)
    {
        if (!Fading)
        {
            return false;
        }

        FadeElapsed += deltaTime;
        float t = Mathf.Clamp01(FadeElapsed / FadeTime);
        FadeValue = Mathf.Lerp(FadeFrom, FadeTarget, t);
        if (t < 1f)
        {
            return false;
        }

        Fading = false;
        return StopWhenFaded;
    }

    public void Release()
    {
        if (Source != null)
        {
            Source.Stop();
        }
        Active = false;
        Fading = false;
        FadeValue = 1f;
        Id++;
    }
}

// 채널별 재생, AudioSource 재사용과 페이드를 담당한다.
internal sealed class SoundPlayback
{
    private sealed class Channel
    {
        public Transform Root;
        public AudioMixerGroup Group;
        public bool IsBgm;
        public int MaxVoices;
        public readonly List<SoundVoice> Voices = new List<SoundVoice>();
    }

    private readonly Channel[] _channels;
    private readonly SoundVolumeController _volumeController;

    public SoundPlayback(Transform rootTransform, SoundVolumeController volumeController)
    {
        _volumeController = volumeController;
        string[] names = System.Enum.GetNames(typeof(SoundChannel));
        _channels = new Channel[names.Length];
        for (int i = 0; i < _channels.Length; i++)
        {
            var root = new GameObject(names[i]).transform;
            root.SetParent(rootTransform, false);
            _channels[i] = new Channel
            {
                Root = root,
                Group = _volumeController.FindGroup((SoundChannel)i),
                IsBgm = (SoundChannel)i == SoundChannel.Bgm,
                MaxVoices = GetMaxVoices((SoundChannel)i),
            };
        }

    }

    private static int GetMaxVoices(SoundChannel channel)
    {
        switch (channel)
        {
            case SoundChannel.Bgm: return 3; // 크로스페이드 중에는 두세 곡이 겹친다
            case SoundChannel.Ui: return 4;
            case SoundChannel.Player: return 8;
            case SoundChannel.Potion: return 8;
            default: return 16;
        }
    }

    public void Update()
    {
        foreach (Channel channel in _channels)
        {
            foreach (SoundVoice voice in channel.Voices)
            {
                if (voice.Active)
                {
                    UpdateVoice(voice, channel);
                }
            }
        }
    }

    // 끝난 소리는 반납하고, 끝 지점이 있는 소리는 거기서 서서히 줄여 끝낸다.
    private void UpdateVoice(SoundVoice voice, Channel channel)
    {
        if (!voice.Source.isPlaying)
        {
            voice.Release();
            return;
        }

        // 일시정지(timeScale 0) 중에도 페이드는 진행되게 한다.
        if (voice.AdvanceFade(Time.unscaledDeltaTime))
        {
            voice.Release();
            return;
        }

        float fade = 1f;
        if (voice.EndTime > 0f)
        {
            float remaining = voice.EndTime - voice.Source.time;
            if (remaining <= 0f)
            {
                voice.Release();
                return;
            }

            fade = voice.FadeOutTime > 0f ? Mathf.Clamp01(remaining / voice.FadeOutTime) : 1f;
        }

        voice.Source.volume = voice.BaseVolume * fade * voice.FadeValue * _volumeController.GetSoftwareVolume(channel.IsBgm);
    }

    public SoundHandle Play(AudioClip clip, SoundChannel channelId, float volume,
        float startOffset, float endTime, float fadeOutTime, bool loop, float fadeInTime)
    {
        Channel channel = _channels[(int)channelId];
        SoundVoice voice = AcquireVoice(channel);
        AudioClip playClip = clip;

        voice.BaseVolume = Mathf.Clamp01(volume);
        voice.EndTime = endTime;
        voice.FadeOutTime = fadeOutTime;
        voice.StartedAt = Time.unscaledTime;
        voice.Active = true;
        voice.FadeValue = 1f;
        voice.Fading = false;
        if (fadeInTime > 0f)
        {
            voice.FadeValue = 0f;
            voice.StartFade(1f, fadeInTime, false);
        }

        AudioSource source = voice.Source;
        source.clip = playClip;
        source.loop = loop;
        source.volume = voice.BaseVolume * voice.FadeValue * _volumeController.GetSoftwareVolume(channel.IsBgm);
        source.time = Mathf.Min(startOffset, Mathf.Max(0f, playClip.length - 0.01f));
        source.Play();

        return new SoundHandle(voice);
    }

    // 놀고 있는 보이스를 쓰고, 없으면 새로 만들고, 꽉 찼으면 가장 오래된 소리를 끊고 그 자리를 쓴다.
    private SoundVoice AcquireVoice(Channel channel)
    {
        SoundVoice oldest = null;
        foreach (SoundVoice voice in channel.Voices)
        {
            if (!voice.Active)
            {
                return voice;
            }
            if (oldest == null || voice.StartedAt < oldest.StartedAt)
            {
                oldest = voice;
            }
        }

        if (channel.Voices.Count < channel.MaxVoices)
        {
            AudioSource source = channel.Root.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = channel.Group;
            var created = new SoundVoice { Source = source };
            channel.Voices.Add(created);
            return created;
        }

        oldest.Release();
        return oldest;
    }

    public void FadeOutChannel(SoundChannel channel, float fadeTime)
    {
        foreach (SoundVoice voice in _channels[(int)channel].Voices)
        {
            if (voice.Active)
            {
                voice.StartFade(0f, fadeTime, true);
            }
        }
    }
}
