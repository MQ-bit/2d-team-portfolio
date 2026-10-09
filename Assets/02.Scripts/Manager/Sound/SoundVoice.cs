using UnityEngine;

// 하나의 AudioSource에 대한 재생 상태, 개별 페이드, 종료 구간을 관리한다.
internal sealed class SoundVoice
{
    public AudioSource Source { get; }
    public int Id { get; private set; }
    public bool Active { get; private set; }
    public bool Paused { get; private set; }
    public float StartedAt { get; private set; }

    private float _baseVolume;
    private float _endTime;
    private float _fadeOutTime;
    private float _fadeValue = 1f;
    private float _fadeFrom;
    private float _fadeTarget;
    private float _fadeTime;
    private float _fadeElapsed;
    private bool _fading;
    private bool _stopWhenFaded;
    private int _repeatsRemaining = 1;
    private float _repeatStartOffset;

    public SoundVoice(AudioSource source)
    {
        Source = source;
    }

    public void Play(AudioClip clip, float volume, float startOffset, float endTime,
        float fadeOutTime, bool loop, float fadeInTime, float channelVolume, bool paused)
    {
        _baseVolume = Mathf.Clamp01(volume);
        _endTime = endTime;
        _fadeOutTime = fadeOutTime;
        StartedAt = Time.unscaledTime;
        Active = true;
        Paused = false;
        _fadeValue = 1f;
        _fading = false;
        _repeatsRemaining = 1;
        if (fadeInTime > 0f)
        {
            _fadeValue = 0f;
            StartFade(1f, fadeInTime, false);
        }

        Source.clip = clip;
        Source.loop = loop;
        Source.volume = paused ? 0f : _baseVolume * _fadeValue * channelVolume;
        Source.time = Mathf.Min(startOffset, Mathf.Max(0f, clip.length - 0.01f));
        _repeatStartOffset = Source.time;
        Source.Play();
        if (paused) Pause();
    }

    public void Update(float deltaTime, float channelVolume, bool shouldPause)
    {
        // Pause도 isPlaying을 false로 만들므로 종료 판정보다 먼저 구분한다.
        if (!Active || Paused) return;
        // 명시적인 FadeOut 종료는 반복 재생을 다시 시작하지 않습니다.
        if (AdvanceFade(deltaTime))
        {
            Release();
            return;
        }
        if (!Source.isPlaying || (_endTime > 0f && Source.time >= _endTime))
        {
            if (_repeatsRemaining <= 1)
            {
                Release();
                return;
            }
            _repeatsRemaining--;
            Source.time = _repeatStartOffset;
            Source.Play();
        }

        if (shouldPause)
        {
            Pause();
            return;
        }

        ApplyVolume(channelVolume);
    }

    public void Pause()
    {
        if (!Active || Paused) return;
        if (!Source.isPlaying)
        {
            Release();
            return;
        }

        Source.volume = 0f;
        Source.Pause();
        Paused = true;
    }

    public void SetRepeatCount(int count)
    {
        if (Active && !Source.loop) _repeatsRemaining = Mathf.Max(1, count);
    }

    public void Resume(float channelVolume)
    {
        if (!Active || !Paused) return;
        // 즉시 Resume일 때도 이전 프레임의 0 볼륨을 사용하지 않는다.
        ApplyVolume(channelVolume);
        Source.UnPause();
        Paused = false;
    }

    public void StartFade(float target, float time, bool stopWhenFaded)
    {
        // 이미 들리지 않는 일시정지 보이스는 Resume을 기다리지 않고 정리한다.
        if (Paused && stopWhenFaded)
        {
            Release();
            return;
        }

        target = Mathf.Clamp01(target);
        _stopWhenFaded = stopWhenFaded;
        if (time <= 0f)
        {
            _fadeValue = target;
            _fading = false;
            if (stopWhenFaded) Release();
            return;
        }

        _fadeFrom = _fadeValue;
        _fadeTarget = target;
        _fadeTime = time;
        _fadeElapsed = 0f;
        _fading = true;
    }

    public void FadeOutScaled(float fadeTime, float volumeScale)
    {
        // 현재 혼합 음량과 재생 위치를 유지한 채 잔향의 크기만 줄인다.
        _fadeValue *= Mathf.Clamp01(volumeScale);
        StartFade(0f, fadeTime, true);
    }

    public void Release()
    {
        if (Source != null)
        {
            Source.Stop();
        }
        Active = false;
        Paused = false;
        _fading = false;
        _fadeValue = 1f;
        Id++;
    }

    public void StopAndClearClip()
    {
        Release();
        if (Source != null)
        {
            Source.clip = null;
        }
    }

    private void ApplyVolume(float channelVolume)
    {
        float endFade = _endTime > 0f && _fadeOutTime > 0f
            ? Mathf.Clamp01((_endTime - Source.time) / _fadeOutTime) : 1f;
        Source.volume = _baseVolume * endFade * _fadeValue * channelVolume;
    }

    private bool AdvanceFade(float deltaTime)
    {
        if (!_fading) return false;
        _fadeElapsed += deltaTime;
        float t = Mathf.Clamp01(_fadeElapsed / _fadeTime);
        _fadeValue = Mathf.Lerp(_fadeFrom, _fadeTarget, t);
        if (t < 1f) return false;

        _fading = false;
        return _stopWhenFaded;
    }
}

// 재생 중인 소리 하나를 가리키는 손잡이. 보이스가 재사용되면 이전 핸들은 무효가 된다.
public readonly struct SoundHandle
{
    private readonly SoundVoice _voice;
    private readonly int _id;

    internal SoundHandle(SoundVoice voice)
    {
        _voice = voice;
        _id = voice.Id;
    }

    // Pause 중에도 핸들은 유효하다. Stop/FadeOut으로 정리할 수 있다.
    public bool IsPlaying => _voice != null && _voice.Id == _id && _voice.Active;
    public bool IsPaused => IsPlaying && _voice.Paused;

    // 재생 위치(초). 재생 중이 아니면 0.
    public float Time => IsPlaying ? _voice.Source.time : 0f;

    public void SetRepeatCount(int count)
    {
        if (IsPlaying) _voice.SetRepeatCount(count);
    }

    public void Stop()
    {
        if (IsPlaying)
        {
            _voice.Release();
        }
    }

    // fadeTime초에 걸쳐 줄이다가 멈춘다. 이미 일시정지 중이면 바로 정리한다.
    public void FadeOut(float fadeTime)
    {
        if (IsPlaying)
        {
            _voice.StartFade(0f, fadeTime, true);
        }
    }

    // 현재 음량을 volumeScale배로 낮춘 뒤 페이드아웃한다.
    public void FadeOut(float fadeTime, float volumeScale)
    {
        if (IsPlaying)
        {
            _voice.FadeOutScaled(fadeTime, volumeScale);
        }
    }

    // 처음 정한 볼륨 대비 비율을 바꾼다. 멈추지는 않는다.
    public void FadeTo(float targetVolume, float fadeTime)
    {
        if (IsPlaying)
        {
            _voice.StartFade(targetVolume, fadeTime, false);
        }
    }
}
