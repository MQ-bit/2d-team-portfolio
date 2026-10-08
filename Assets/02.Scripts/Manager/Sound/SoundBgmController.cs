using UnityEngine;

// 곡 선택은 씬/게임 진행 코드가 맡는다. 현재 곡과 전환만 관리한다.
// 효과음과 같은 Playback을 사용해 채널 제어와 볼륨 설정을 공유한다.
internal sealed class SoundBgmController
{
    private readonly SoundPlayback _playback;
    private SoundHandle _bgmHandle;
    private AudioClip _bgmClip;

    public bool IsPlaying => _bgmHandle.IsPlaying;

    public SoundBgmController(SoundPlayback playback)
    {
        _playback = playback;
    }

    public SoundHandle Play(AudioClip clip, float volume, float fadeTime)
    {
        if (clip == null)
        {
            FadeOut(fadeTime);
            return default;
        }

        if (_bgmHandle.IsPlaying && _bgmClip == clip)
        {
            return _bgmHandle;
        }

        if (fadeTime > 0f)
        {
            _bgmHandle.FadeOut(fadeTime);
        }
        else
        {
            _bgmHandle.Stop();
        }

        _bgmClip = clip;
        _bgmHandle = _playback.Play(clip, SoundChannel.Bgm, volume,
            startOffset: 0f, endTime: 0f, fadeOutTime: 0f, loop: true, fadeInTime: fadeTime);
        return _bgmHandle;
    }

    public void FadeOut(float fadeTime)
    {
        _bgmHandle.FadeOut(fadeTime);
        _bgmClip = null;
    }
}
