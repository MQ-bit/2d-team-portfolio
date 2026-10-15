using System.Collections.Generic;
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
    EnemyDeath,
}

// 전역 사운드 진입점. 씬에 배치하고 GameSoundData를 연결하며, 씬 전환 시 유지한다.
// 재생·BGM·볼륨·클립 가공은 각 담당 클래스에 위임한다.
// 믹서 미연결 시 Resources/MainAudioMixer를 사용하고, 없으면 AudioSource 볼륨으로 조절한다.
// 전체/BGM/효과음 볼륨은 변경 시 저장하고 재실행 시 복원한다.
// Pause는 Player/Potion만 멈추고 복귀 시 이어서 재생한다. BGM/Ui/Sfx는 유지한다.
//
// 주요 사용법
// 효과음: SoundManager.Play(GameSoundId.PlayerDash);
// BGM:    SoundManager.PlayBgm(GameBgmId.Title);  // 인트로 → 루프 (InGame은 셔플·크로스페이드)
// 종료:   SoundManager.FadeOutBgm(1f); SoundManager.StopBgm();
// Pause:  SoundManager.SetPaused(true); SoundManager.SetPaused(false);
// 볼륨:   SoundManager.SetMasterVolume(v); SoundManager.SetBgmVolume(v); SoundManager.SetSfxVolume(v); // 0~1
// 개별 제어: 반환된 SoundHandle로 Stop/FadeOut/FadeTo 및 재생 상태 확인.
// 다른 컴포넌트의 Awake에서 사운드를 요청하기 전에 초기화한다.
[DefaultExecutionOrder(-1000)]
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
    // 종류별 간격은 모든 적이 공유한다. 재생을 건너뛰어도 이미 재생 중인 소리는 유지한다.
    private readonly Dictionary<GameSoundId, float> _nextEnemyDeathSoundTimes = new();

    public static SoundHandle Play(GameSoundId id)
    {
        GameSoundData.Entry entry = GetSound(id);
        if (entry == null || entry.Clip == null) return default;
        if (id != GameSoundId.ExplodingEnemyDeath && id != GameSoundId.RangedEnemyDeath
            && id != GameSoundId.ReaperDeath)
        {
            return entry.Play();
        }

        SoundManager manager = Instance;
        if (manager == null) return default;
        float now = Time.unscaledTime;
        if (manager._nextEnemyDeathSoundTimes.TryGetValue(id, out float nextTime) && now < nextTime)
            return default;

        SoundHandle handle = entry.Play();
        if (handle.IsPlaying) manager._nextEnemyDeathSoundTimes[id] = now + entry.Interval;
        return handle;
    }
    public static void Preload(GameSoundId id) => GetSound(id)?.Preload();

    private static readonly GameSoundId[] PotionBreakSounds =
    {
        GameSoundId.PotionBreak, GameSoundId.PotionBreakB, GameSoundId.PotionBreakC
    };

    // 모든 포션의 파괴 소리는 A → B → C 순서를 공유한다. 미할당된 소리는 건너뛴다.
    public static SoundHandle PlayPotionBreak()
    {
        SoundManager manager = Instance;
        if (manager == null) return default;

        for (int i = 0; i < PotionBreakSounds.Length; i++)
        {
            GameSoundId id = PotionBreakSounds[manager._nextPotionBreakIndex];
            manager._nextPotionBreakIndex = (manager._nextPotionBreakIndex + 1) % PotionBreakSounds.Length;
            GameSoundData.Entry entry = GetSound(id);
            if (entry != null && entry.Clip != null) return entry.Play();
        }
        return default;
    }

    public static SoundHandle PlayBgm(GameBgmId id)
    {
        SoundManager manager = Instance;
        if (manager == null || manager._soundData == null) return default;
        manager.CancelPendingBgm();
        if (id == GameBgmId.InGame)
        {
            GameSoundData.BgmEntry[] tracks = manager._soundData.GetBgmTracks(id);
            // 인게임 곡은 스트리밍으로 재생하므로 시작 시 전체 목록을 로딩하지 않는다.
            return manager._bgmController.PlayShuffled(tracks);
        }
        GameSoundData.BgmEntry entry = manager._soundData.GetBgm(id);
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
    public static bool IsBgmIdPending(GameBgmId id) => Instance != null && Instance._pendingBgmId == id;
    public static void StopBgm(GameBgmId id)
    {
        if (Instance != null) Instance._bgmController.StopId(id);
    }

    private SoundPlayback _playback;
    private SoundVolumeController _volumeController;
    private SoundBgmController _bgmController;
    private SoundClipCache _clipCache;
    private int _nextPotionBreakIndex;
    private GameBgmId? _pendingBgmId;
    private SoundHandle _pendingBgmSound;

    // SoundManager survives scene changes, so the sound can finish before the BGM starts.
    public static void PlayBgmAfterSound(GameSoundId soundId, GameBgmId bgmId)
    {
        SoundManager manager = Instance;
        if (manager == null) return;

        FadeOutBgm(0.35f);
        GameSoundData.BgmEntry bgm = Data != null ? Data.GetBgm(bgmId) : null;
        if (bgm != null)
        {
            Preload(bgm.IntroClip);
            Preload(bgm.LoopClip);
        }
        manager._pendingBgmSound = Play(soundId);
        manager._pendingBgmId = bgmId;
    }

    private void CancelPendingBgm()
    {
        _pendingBgmId = null;
        _pendingBgmSound.Stop();
        _pendingBgmSound = default;
    }

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

    private void Start()
    {
        // AudioMixer 초기화가 끝난 뒤 저장된 볼륨을 적용한다.
        _volumeController.ApplyMixerVolumes();
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
        if (_pendingBgmId.HasValue && !_pendingBgmSound.IsPlaying)
        {
            GameBgmId bgmId = _pendingBgmId.Value;
            _pendingBgmId = null;
            PlayBgm(bgmId);
        }
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

        manager.CancelPendingBgm();
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

        manager.CancelPendingBgm();
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
        // 씬 종료 시 해제 요청은 이미 등록된 매니저에만 전달한다.
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
