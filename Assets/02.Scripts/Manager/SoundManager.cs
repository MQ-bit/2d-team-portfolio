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
}

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

// 전역 사운드 관리자. 모든 소리는 여기서 채널을 골라 재생한다.
// 씬에 없으면 처음 쓸 때 스스로 만들어진다.
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
    private sealed class Channel
    {
        public Transform Root;
        public AudioMixerGroup Group;
        public bool IsBgm;
        public int MaxVoices;
        public readonly List<SoundVoice> Voices = new List<SoundVoice>();
    }

    private static SoundManager _instance;
    private static bool _isQuitting;

    private const string MixerResourcePath = "MainAudioMixer";
    private const string MasterVolumeParameter = "MasterVolume";
    private const string BgmVolumeParameter = "BgmVolume";
    private const string SfxVolumeParameter = "SfxVolume";
    private const float MinDecibel = -80f;

    private static SoundManager Instance
    {
        get
        {
            if (_instance == null && !_isQuitting)
            {
                _instance = FindAnyObjectByType<SoundManager>();
                if (_instance == null)
                {
                    new GameObject("SoundManager").AddComponent<SoundManager>();
                }
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

    private Channel[] _channels;
    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _sfxVolume = 1f;

    private SoundHandle _bgmHandle;
    private AudioClip _bgmClip;

    // Gain으로 키운 클립은 런타임에 새로 만든 것이라 직접 정리한다.
    private readonly Dictionary<(AudioClip clip, float gain), AudioClip> _amplifiedClips =
        new Dictionary<(AudioClip clip, float gain), AudioClip>();

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

        if (_mixer == null)
        {
            _mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        }
        if (_mixer == null)
        {
            Debug.LogWarning($"Resources/{MixerResourcePath} AudioMixer를 찾을 수 없어 AudioSource 볼륨으로 대신 조절함.");
        }

        string[] names = System.Enum.GetNames(typeof(SoundChannel));
        _channels = new Channel[names.Length];
        for (int i = 0; i < _channels.Length; i++)
        {
            var root = new GameObject(names[i]).transform;
            root.SetParent(transform, false);
            _channels[i] = new Channel
            {
                Root = root,
                Group = FindGroup((SoundChannel)i),
                IsBgm = (SoundChannel)i == SoundChannel.Bgm,
                MaxVoices = GetMaxVoices((SoundChannel)i),
            };
        }

        ApplyMixerVolumes();
    }

    // Bgm과 Sfx는 Master 바로 아래, 나머지 효과음 채널은 Sfx 아래에 있다.
    private AudioMixerGroup FindGroup(SoundChannel channel)
    {
        if (_mixer == null)
        {
            return null;
        }

        string path = channel == SoundChannel.Bgm || channel == SoundChannel.Sfx
            ? $"Master/{channel}"
            : $"Master/Sfx/{channel}";
        AudioMixerGroup[] groups = _mixer.FindMatchingGroups(path);
        if (groups.Length == 0)
        {
            Debug.LogWarning($"{_mixer.name} : {path} 그룹이 없어 Master 그룹으로 출력함.");
            return null;
        }

        return groups[0];
    }

    // 0~1 볼륨을 믹서의 데시벨 값으로 바꿔 넣는다. 믹서가 없으면 아무것도 하지 않는다(소리 볼륨에 직접 곱한다).
    private static float ToDecibel(float volume)
    {
        return volume <= 0.0001f ? MinDecibel : Mathf.Log10(volume) * 20f;
    }

    private void ApplyMixerVolumes()
    {
        if (_mixer == null)
        {
            return;
        }

        _mixer.SetFloat(MasterVolumeParameter, ToDecibel(_masterVolume));
        _mixer.SetFloat(BgmVolumeParameter, ToDecibel(_bgmVolume));
        _mixer.SetFloat(SfxVolumeParameter, ToDecibel(_sfxVolume));
    }

    // 믹서가 볼륨을 맡으면 소리 자체에는 곱하지 않는다.
    private float GetSoftwareVolume(Channel channel)
    {
        return _mixer != null ? 1f : (channel.IsBgm ? _bgmVolume : _sfxVolume) * _masterVolume;
    }

    private void OnDestroy()
    {
        if (_instance != this)
        {
            return;
        }

        _instance = null;
        foreach (AudioClip amplified in _amplifiedClips.Values)
        {
            if (amplified != null)
            {
                Destroy(amplified);
            }
        }
        _amplifiedClips.Clear();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
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

    private void Update()
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

        voice.Source.volume = voice.BaseVolume * fade * voice.FadeValue * GetSoftwareVolume(channel);
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

        return manager.PlayInternal(clip, channel, volume, gain, startOffset, endTime, fadeOutTime, loop, fadeInTime);
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

        if (clip == null)
        {
            FadeOutBgm(fadeTime);
            return default;
        }

        if (manager._bgmHandle.IsPlaying && manager._bgmClip == clip)
        {
            return manager._bgmHandle;
        }

        if (fadeTime > 0f)
        {
            manager._bgmHandle.FadeOut(fadeTime);
        }
        else
        {
            manager._bgmHandle.Stop();
        }

        manager._bgmClip = clip;
        manager._bgmHandle = Play(clip, SoundChannel.Bgm, volume, loop: true, fadeInTime: fadeTime);
        return manager._bgmHandle;
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

        manager._bgmHandle.FadeOut(fadeTime);
        manager._bgmClip = null;
    }

    // 나오던 BGM을 바로 끈다.
    public static void StopBgm()
    {
        FadeOutBgm(0f);
    }

    public static bool IsBgmPlaying
    {
        get
        {
            SoundManager manager = Instance;
            return manager != null && manager._bgmHandle.IsPlaying;
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

        clip.LoadAudioData();
        manager.GetClip(clip, gain);
    }

    public static void StopChannel(SoundChannel channel)
    {
        FadeOutChannel(channel, 0f);
    }

    // 채널에서 나오는 소리 전부를 fadeTime초에 걸쳐 줄이면서 끈다.
    public static void FadeOutChannel(SoundChannel channel, float fadeTime)
    {
        SoundManager manager = Instance;
        if (manager == null)
        {
            return;
        }

        foreach (SoundVoice voice in manager._channels[(int)channel].Voices)
        {
            if (voice.Active)
            {
                voice.StartFade(0f, fadeTime, true);
            }
        }
    }

    public static void SetMasterVolume(float volume)
    {
        SoundManager manager = Instance;
        if (manager != null)
        {
            manager._masterVolume = Mathf.Clamp01(volume);
            manager.ApplyMixerVolumes();
        }
    }

    public static float GetMasterVolume()
    {
        SoundManager manager = Instance;
        return manager != null ? manager._masterVolume : 1f;
    }

    public static void SetBgmVolume(float volume)
    {
        SoundManager manager = Instance;
        if (manager != null)
        {
            manager._bgmVolume = Mathf.Clamp01(volume);
            manager.ApplyMixerVolumes();
        }
    }

    public static float GetBgmVolume()
    {
        SoundManager manager = Instance;
        return manager != null ? manager._bgmVolume : 1f;
    }

    // Ui, Player, Potion 같은 BGM이 아닌 모든 채널의 볼륨.
    public static void SetSfxVolume(float volume)
    {
        SoundManager manager = Instance;
        if (manager != null)
        {
            manager._sfxVolume = Mathf.Clamp01(volume);
            manager.ApplyMixerVolumes();
        }
    }

    public static float GetSfxVolume()
    {
        SoundManager manager = Instance;
        return manager != null ? manager._sfxVolume : 1f;
    }

    private SoundHandle PlayInternal(AudioClip clip, SoundChannel channelId, float volume, float gain,
        float startOffset, float endTime, float fadeOutTime, bool loop, float fadeInTime)
    {
        Channel channel = _channels[(int)channelId];
        SoundVoice voice = AcquireVoice(channel);
        AudioClip playClip = GetClip(clip, gain);

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
        source.volume = voice.BaseVolume * voice.FadeValue * GetSoftwareVolume(channel);
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

    private AudioClip GetClip(AudioClip clip, float gain)
    {
        if (gain <= 1f)
        {
            return clip;
        }

        var key = (clip, gain);
        if (_amplifiedClips.TryGetValue(key, out AudioClip cached))
        {
            return cached != null ? cached : clip;
        }

        AudioClip amplified = CreateAmplifiedClip(clip, gain);
        _amplifiedClips[key] = amplified;
        return amplified != null ? amplified : clip;
    }

    // 샘플 값을 gain배로 키운 복사본을 만든다. 샘플을 읽을 수 없으면 null.
    private static AudioClip CreateAmplifiedClip(AudioClip clip, float gain)
    {
        float[] samples = new float[clip.samples * clip.channels];
        if (!clip.GetData(samples, 0))
        {
            Debug.LogWarning($"{clip.name} : 샘플을 읽을 수 없어 Gain을 적용하지 못함. (Load Type이 Decompress On Load여야 함)");
            return null;
        }

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Mathf.Clamp(samples[i] * gain, -1f, 1f);
        }

        AudioClip amplified = AudioClip.Create($"{clip.name}_Amplified", clip.samples, clip.channels, clip.frequency, false);
        amplified.SetData(samples, 0);
        return amplified;
    }
}
