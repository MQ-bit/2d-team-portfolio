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

    // 처음 정한 볼륨 대비 비율을 바꾼다. 멈추지는 않는다.
    public void FadeTo(float targetVolume, float fadeTime)
    {
        if (IsPlaying)
        {
            _voice.StartFade(targetVolume, fadeTime, false);
        }
    }
}
