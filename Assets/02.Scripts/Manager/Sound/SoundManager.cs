using UnityEngine;
using UnityEngine.Audio;

// 소리가 나가는 채널. 채널마다 볼륨을 따로 조절하고, 동시에 낼 수 있는 소리 수(보이스)가 정해져 있다.
public enum SoundChannel
{
    Bgm,
    Sfx,
    Ui,
    Player,
    Potion,
}

// 전역 사운드 진입점. 담당 객체를 생성하고 재생/볼륨/Pause 요청을 전달한다.
// 씬에 배치하고 GameSoundData를 인스펙터에서 직접 연결한다.
//
// 볼륨은 AudioMixer로 관리한다. Resources/MainAudioMixer 에셋을 불러오며, 구조는 다음과 같다.
//   Master 아래에 Bgm, Sfx 그룹. Sfx 아래에 Ui, Player, Potion 그룹(채널 이름과 같은 이름).
//   조절하는 볼륨은 전체/BGM/효과음 3개뿐이다. Master, Bgm, Sfx 그룹의 볼륨을
//   각각 "MasterVolume", "BgmVolume", "SfxVolume"으로 Expose. (Sfx 아래 그룹은 Sfx 볼륨을 따라간다.)
// 에셋이 없으면 경고를 내고 AudioSource 볼륨에 직접 곱하는 방식으로 대신한다.
//
// ── 사용법 (전부 static이라 SoundManager.함수() 로 바로 부른다) ──
//  효과음   SoundManager.PlaySfx(clip);                         // 일반 효과음
//           SoundManager.PlayUi(clip);                          // 버튼 같은 UI 소리
//           SoundManager.Play(clip, SoundChannel.Player, volume: 0.8f, gain: 2f, fadeInTime: 0.2f);  // 세부 옵션
//  BGM      SoundManager.PlayBgm(clip);                         // BGM 시작. 이미 나오던 BGM은 바로 끊김
//           SoundManager.PlayBgm(clip, fadeTime: 1.5f);         // 새 BGM은 서서히 커지고 이전 BGM은 서서히 줄어듦(크로스페이드)
//           SoundManager.CrossFadeBgm(clip, 2f);                // 위와 같음(이름만 명확하게)
//           SoundManager.FadeOutBgm(2f);                        // BGM을 서서히 줄이며 끄기
//           SoundManager.StopBgm();                             // BGM 바로 끄기
//  핸들     SoundHandle h = SoundManager.PlaySfx(clip);         // 반환값을 들고 있으면 나중에 제어 가능
//           h.Stop();  h.FadeOut(1f);  h.FadeTo(0.3f, 0.5f);  h.IsPlaying;  h.Time;
//  채널     SoundManager.StopChannel(SoundChannel.Sfx);  SoundManager.FadeOutChannel(SoundChannel.Sfx, 1f);
//  볼륨     SoundManager.SetMasterVolume(v); SetBgmVolume(v); SetSfxVolume(v);  (v는 0~1, 설정창 슬라이더용)
//  로딩     SoundManager.Preload(clip);                         // 처음 재생이 늦지 않게 미리 읽어두기
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    private static bool _isQuitting;

    private static SoundManager Instance
    {
        get
        {
            if (_instance == null && !_isQuitting)
            {
                _instance = FindAnyObjectByType<SoundManager>();
            }

            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _isQuitting = false;
    }

    [Tooltip("비워두면 Resources/MainAudioMixer를 불러온다.")]
    [SerializeField] private AudioMixer _mixer;
    [Tooltip("Assets/07.Datas/Sound/GameSoundData.asset을 직접 연결해주세요.")]
    [SerializeField] private GameSoundData _soundData;

    public static GameSoundData Data => Instance != null ? Instance._soundData : null;
    public static GameSoundData.Entry GetSound(GameSoundId id) => Data != null ? Data.Get(id) : null;
    public static SoundHandle Play(GameSoundId id) => GetSound(id)?.Play() ?? default;
    public static void Preload(GameSoundId id) => GetSound(id)?.Preload();

    public static SoundHandle PlayBgm(GameBgmId id)
    {
        SoundManager manager = Instance;
        if (manager == null || manager._soundData == null) return default;
        if (id == GameBgmId.InGame)
        {
            GameSoundData.BgmEntry[] tracks = manager._soundData.GetBgmTracks(id);
            foreach (GameSoundData.BgmEntry track in tracks)
                Preload(track.LoopClip != null ? track.LoopClip : track.IntroClip);
            return manager._bgmController.PlayShuffled(tracks);
        }
        GameSoundData.BgmEntry entry = manager != null && manager._soundData != null
            ? manager._soundData.GetBgm(id) : null;
        if (entry == null) return default;
        Preload(entry.IntroClip);
        Preload(entry.LoopClip);
        return manager._bgmController.Play(entry);
    }

    // 특정 BGM이 재생 중인지 확인
    public static bool IsBgmIdPlaying(GameBgmId id)
    {
        return Instance != null
               && Instance._bgmController.IsPlayingId(id);
    }
    public static void StopBgm(GameBgmId id)
    {
        if (Instance != null) Instance._bgmController.StopId(id);
    }

    private SoundPlayback _playback;
    private SoundVolumeController _volumeController;
    private SoundBgmController _bgmController;
    private SoundClipCache _clipCache;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        if (_soundData == null)
            Debug.LogWarning("SoundManager의 Sound Data에 GameSoundData 에셋을 연결해주세요.", this);

        _volumeController = new SoundVolumeController(_mixer);
        _playback = new SoundPlayback(transform, _volumeController);
        _bgmController = new SoundBgmController(_playback);
        _clipCache = new SoundClipCache();
    }

    private void OnDestroy()
    {
        if (_instance != this)
        {
            return;
        }

        _instance = null;
        // 생성 클립을 해제하기 전에 이를 참조하는 보이스부터 정리한다.
        _playback.StopAll();
        _clipCache.Dispose();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void Update()
    {
        _playback.Update();
        _bgmController.Update();
    }

    // 클립을 채널로 재생한다. 재생을 멈추거나 상태를 볼 수 있는 손잡이를 돌려준다.
    // gain: 작게 녹음된 소리를 키우는 배율(volume은 1이 최대라서). startOffset/endTime: 클립에서 재생할 구간(초, endTime 0이면 끝까지).
    // fadeOutTime: endTime 직전에 줄이는 시간. loop: 멈출 때까지 반복. fadeInTime: 시작할 때 서서히 커지는 시간.
    public static SoundHandle Play(AudioClip clip, SoundChannel channel, float volume = 1f, float gain = 1f,
        float startOffset = 0f, float endTime = 0f, float fadeOutTime = 0f, bool loop = false, float fadeInTime = 0f)
    {
        SoundManager manager = Instance;
        if (clip == null || manager == null)
        {
            return default;
        }

        return manager._playback.Play(manager._clipCache.GetClip(clip, gain), channel, volume,
            startOffset, endTime, fadeOutTime, loop, fadeInTime);
    }

    // 일반 효과음(Sfx 채널).
    public static SoundHandle PlaySfx(AudioClip clip, float volume = 1f, float fadeInTime = 0f)
    {
        return Play(clip, SoundChannel.Sfx, volume, fadeInTime: fadeInTime);
    }

    // UI 소리(Ui 채널). 버튼 클릭 같은 것.
    public static SoundHandle PlayUi(AudioClip clip, float volume = 1f)
    {
        return Play(clip, SoundChannel.Ui, volume);
    }

    // BGM을 반복 재생한다. 이미 같은 곡이 나오고 있으면 다시 시작하지 않는다.
    // fadeTime이 0이면 이전 BGM을 바로 끄고 새 곡을 바로 틀고, 0보다 크면 이전 곡은 줄어들고 새 곡은 커지는 크로스페이드가 된다.
    public static SoundHandle PlayBgm(AudioClip clip, float volume = 1f, float fadeTime = 0f)
    {
        SoundManager manager = Instance;
        if (manager == null)
        {
            return default;
        }

        return manager._bgmController.Play(clip, volume, fadeTime);
    }

    // 이전 BGM을 줄이면서 새 BGM을 키운다. PlayBgm(clip, volume, fadeTime)과 같다.
    public static SoundHandle CrossFadeBgm(AudioClip clip, float fadeTime = 1f, float volume = 1f)
    {
        return PlayBgm(clip, volume, Mathf.Max(0.01f, fadeTime));
    }

    // 나오던 BGM을 fadeTime초에 걸쳐 줄이면서 끈다.
    public static void FadeOutBgm(float fadeTime = 1f)
    {
        SoundManager manager = Instance;
        if (manager == null)
        {
            return;
        }

        manager._bgmController.FadeOut(fadeTime);
    }

    // 나오던 BGM을 바로 끈다.
    public static void StopBgm()
    {
        FadeOutBgm(0f);
    }

    // BGM이 하나라도 재생 중인지 확인
    public static bool IsBgmPlaying
    {
        get
        {
            SoundManager manager = Instance;
            return manager != null && manager._bgmController.IsPlaying;
        }
    }

    // 처음 재생할 때 로딩하느라 소리가 늦지 않도록 미리 읽어둔다. gain을 쓸 소리면 키운 클립도 미리 만든다.
    public static void Preload(AudioClip clip, float gain = 1f)
    {
        SoundManager manager = Instance;
        if (clip == null || manager == null)
        {
            return;
        }

        manager._clipCache.Preload(clip, gain);
    }

    public static void StopChannel(SoundChannel channel)
    {
        FadeOutChannel(channel, 0f);
    }

    // BGM/UI/Sfx는 유지하고 Player/Potion만 페이드 후 일시정지한다.
    // Resume은 보존한 재생 위치에서 페이드 인한다. 사용자 볼륨 설정은 바꾸지 않는다.
    public static void SetPaused(bool paused, float fadeTime = 0.3f)
    {
        // 해제만 요청할 때는 새 매니저를 만들지 않는다(씬 종료 정리 포함).
        SoundManager manager = paused ? Instance : _instance;
        if (manager != null)
        {
            manager._playback.SetPaused(paused, fadeTime);
        }
    }

    public static bool IsPaused => _instance != null && _instance._playback.IsPaused;

    // 채널에서 나오는 소리 전부를 fadeTime초에 걸쳐 줄이면서 끈다.
    public static void FadeOutChannel(SoundChannel channel, float fadeTime)
    {
        SoundManager manager = Instance;
        if (manager == null)
        {
            return;
        }

        manager._playback.FadeOutChannel(channel, fadeTime);
    }

    public static void SetMasterVolume(float volume)
    {
        SoundManager manager = Instance;
        if (manager != null)
        {
            manager._volumeController.SetMasterVolume(volume);
        }
    }

    public static float GetMasterVolume()
    {
        SoundManager manager = Instance;
        return manager != null ? manager._volumeController.MasterVolume : 1f;
    }

    public static void SetBgmVolume(float volume)
    {
        SoundManager manager = Instance;
        if (manager != null)
        {
            manager._volumeController.SetBgmVolume(volume);
        }
    }

    public static float GetBgmVolume()
    {
        SoundManager manager = Instance;
        return manager != null ? manager._volumeController.BgmVolume : 1f;
    }

    // Ui, Player, Potion 같은 BGM이 아닌 모든 채널의 볼륨.
    public static void SetSfxVolume(float volume)
    {
        SoundManager manager = Instance;
        if (manager != null)
        {
            manager._volumeController.SetSfxVolume(volume);
        }
    }

    public static float GetSfxVolume()
    {
        SoundManager manager = Instance;
        return manager != null ? manager._volumeController.SfxVolume : 1f;
    }

}
