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
        if (fadeInTime > 0f)
        {
            _fadeValue = 0f;
            StartFade(1f, fadeInTime, false);
        }

        Source.clip = clip;
        Source.loop = loop;
        Source.volume = paused ? 0f : _baseVolume * _fadeValue * channelVolume;
        Source.time = Mathf.Min(startOffset, Mathf.Max(0f, clip.length - 0.01f));
        Source.Play();
        if (paused) Pause();
    }

    public void Update(float deltaTime, float channelVolume, bool shouldPause)
    {
        // Pause도 isPlaying을 false로 만들므로 종료 판정보다 먼저 구분한다.
        if (!Active || Paused) return;
        if (!Source.isPlaying || AdvanceFade(deltaTime)
            || (_endTime > 0f && Source.time >= _endTime))
        {
            Release();
            return;
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
