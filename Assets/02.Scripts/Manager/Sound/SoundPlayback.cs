using UnityEngine;

// 채널들을 생성하고 재생/정지 요청과 전체 업데이트를 전달한다.
internal sealed class SoundPlayback
{
    private readonly SoundPlaybackChannel[] _channels;

    public bool IsPaused { get; private set; }

    public SoundPlayback(Transform rootTransform, SoundVolumeController volumeController)
    {
        int channelCount = System.Enum.GetNames(typeof(SoundChannel)).Length;
        _channels = new SoundPlaybackChannel[channelCount];
        for (int i = 0; i < channelCount; i++)
        {
            _channels[i] = new SoundPlaybackChannel((SoundChannel)i, rootTransform, volumeController);
        }
    }

    // 메뉴 Pause의 대상 채널만 선택한다. 히트스톱/슬로모션에는 반응하지 않는다.
    public void SetPaused(bool paused, float fadeTime)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;
        _channels[(int)SoundChannel.Player].SetPaused(paused, fadeTime);
        _channels[(int)SoundChannel.Potion].SetPaused(paused, fadeTime);
    }

    public SoundHandle Play(AudioClip clip, SoundChannel channelId, float volume,
        float startOffset, float endTime, float fadeOutTime, bool loop, float fadeInTime)
    {
        return _channels[(int)channelId].Play(clip, volume,
            startOffset, endTime, fadeOutTime, loop, fadeInTime);
    }

    public void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        foreach (SoundPlaybackChannel channel in _channels)
        {
            channel.Update(deltaTime);
        }
    }

    // 캐시 해제 전에 모든 채널의 보이스를 정리한다.
    public void StopAll()
    {
        foreach (SoundPlaybackChannel channel in _channels)
        {
            channel.StopAll();
        }
    }

    public void FadeOutChannel(SoundChannel channel, float fadeTime)
    {
        _channels[(int)channel].FadeOut(fadeTime);
    }
}
