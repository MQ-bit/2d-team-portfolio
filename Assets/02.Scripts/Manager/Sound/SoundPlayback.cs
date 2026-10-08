using UnityEngine;

using System.Collections.Generic;

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

// 곡 선택은 씬/게임 진행 코드가 맡는다. 현재 곡과 전환만 관리한다.
// 효과음과 같은 Playback을 사용해 채널 제어와 볼륨 설정을 공유한다.
internal sealed class SoundBgmController
{
    private readonly SoundPlayback _playback;
    private SoundHandle _bgmHandle;
    private AudioClip _bgmClip;
    private GameSoundData.BgmEntry _bgmEntry;
    private AudioClip _pendingLoop;
    private GameSoundData.BgmEntry[] _shuffledTracks;
    private int _nextTrackIndex;

    public bool IsPlaying => _bgmHandle.IsPlaying;

    public SoundBgmController(SoundPlayback playback)
    {
        _playback = playback;
    }

    public SoundHandle Play(AudioClip clip, float volume, float fadeTime)
    {
        _shuffledTracks = null;
        _bgmEntry = null;
        _pendingLoop = null;
        if (clip == null)
        {
            FadeOut(fadeTime);
            return default;
        }

        if (_bgmHandle.IsPlaying && _bgmClip == clip)
        {
            return _bgmHandle;
        }

        _playback.FadeOutChannel(SoundChannel.Bgm, fadeTime);

        _bgmClip = clip;
        _bgmHandle = _playback.Play(clip, SoundChannel.Bgm, volume,
            startOffset: 0f, endTime: 0f, fadeOutTime: 0f, loop: true, fadeInTime: fadeTime);
        return _bgmHandle;
    }

    public void FadeOut(float fadeTime)
    {
        _shuffledTracks = null;
        _bgmEntry = null;
        _pendingLoop = null;
        // 크로스페이드 중인 이전 곡도 함께 정리합니다.
        _playback.FadeOutChannel(SoundChannel.Bgm, fadeTime);
        _bgmClip = null;
    }

    public SoundHandle Play(GameSoundData.BgmEntry entry)
    {
        if (_bgmEntry == entry && (_bgmHandle.IsPlaying || _pendingLoop != null)) return _bgmHandle;
        if (entry.IntroClip == null && entry.LoopClip == null) return default;
        FadeOut(entry.FadeTime);
        _bgmEntry = entry;
        _pendingLoop = entry.IntroClip != null ? entry.LoopClip : null;
        _bgmClip = entry.IntroClip != null ? entry.IntroClip : entry.LoopClip;
        _bgmHandle = _playback.Play(_bgmClip, SoundChannel.Bgm, entry.Volume,
            0f, 0f, 0f, entry.IntroClip == null, entry.FadeTime);
        return _bgmHandle;
    }

    public void Update()
    {
        if (_shuffledTracks != null)
        {
            float transitionTime = Mathf.Min(_bgmEntry.FadeTime, _bgmClip.length * 0.5f);
            if (!_bgmHandle.IsPlaying || (!_bgmHandle.IsPaused &&
                _bgmClip.length - _bgmHandle.Time <= transitionTime))
            {
                PlayNextTrack();
            }

            return;
        }
        if (_pendingLoop == null || _bgmHandle.IsPlaying || _bgmEntry == null) return;
        _bgmClip = _pendingLoop;
        _pendingLoop = null;
        _bgmHandle = _playback.Play(_bgmClip, SoundChannel.Bgm, _bgmEntry.Volume,
            0f, 0f, 0f, true, 0f);
    }

    public SoundHandle PlayShuffled(GameSoundData.BgmEntry[] tracks)
    {
        if (_shuffledTracks != null) return _bgmHandle;

        var playable = new List<GameSoundData.BgmEntry>();
        var clips = new HashSet<AudioClip>();
        foreach (GameSoundData.BgmEntry track in tracks)
        {
            AudioClip clip = GetTrackClip(track);
            if (clip != null && clips.Add(clip)) playable.Add(track);
        }
        if (playable.Count == 0) return default;

        FadeOut(playable[0].FadeTime);
        _shuffledTracks = playable.ToArray();
        ShuffleTracks();
        PlayNextTrack();
        return _bgmHandle;
    }

    private void ShuffleTracks()
    {
        // 한 순환에서 모든 곡을 한 번씩 재생한 후에만 다시 섞습니다.
        for (int i = _shuffledTracks.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (_shuffledTracks[i], _shuffledTracks[swapIndex]) =
                (_shuffledTracks[swapIndex], _shuffledTracks[i]);
        }
        if (_shuffledTracks.Length > 1 && GetTrackClip(_shuffledTracks[0]) == _bgmClip)
        {
            int swapIndex = Random.Range(1, _shuffledTracks.Length);
            (_shuffledTracks[0], _shuffledTracks[swapIndex]) =
                (_shuffledTracks[swapIndex], _shuffledTracks[0]);
        }
        _nextTrackIndex = 0;
    }

    private void PlayNextTrack()
    {
        if (_nextTrackIndex >= _shuffledTracks.Length) ShuffleTracks();
        GameSoundData.BgmEntry nextEntry = _shuffledTracks[_nextTrackIndex++];
        AudioClip nextClip = GetTrackClip(nextEntry);
        float fadeTime = _bgmEntry != null ? _bgmEntry.FadeTime : nextEntry.FadeTime;
        // 짧은 곡에서도 전환이 연쇄적으로 겹치지 않도록 제한합니다.
        fadeTime = Mathf.Min(fadeTime, nextClip.length * 0.5f);
        if (_bgmHandle.IsPlaying && _bgmClip != null)
            fadeTime = Mathf.Min(fadeTime, Mathf.Max(0f, _bgmClip.length - _bgmHandle.Time));

        _bgmHandle.FadeOut(fadeTime);
        _bgmEntry = nextEntry;
        _bgmClip = nextClip;
        _bgmHandle = _playback.Play(_bgmClip, SoundChannel.Bgm, _bgmEntry.Volume,
            0f, 0f, 0f, false, fadeTime);
    }

    private static AudioClip GetTrackClip(GameSoundData.BgmEntry entry) =>
        entry.LoopClip != null ? entry.LoopClip : entry.IntroClip;

    public bool IsPlayingId(GameBgmId id) => _bgmEntry != null && _bgmEntry.Id == id
        && (_bgmHandle.IsPlaying || _pendingLoop != null || _shuffledTracks != null);
    public void StopId(GameBgmId id)
    {
        if (_bgmEntry != null && _bgmEntry.Id == id) FadeOut(_bgmEntry.FadeTime);
    }
}
