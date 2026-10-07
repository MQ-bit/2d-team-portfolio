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

    public void Stop()
    {
        if (IsPlaying)
        {
            _voice.Release();
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

    public void Release()
    {
        if (Source != null)
        {
            Source.Stop();
        }
        Active = false;
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
            case SoundChannel.Bgm: return 2;
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

        voice.Source.volume = voice.BaseVolume * fade * GetSoftwareVolume(channel);
    }

    // 클립을 채널로 재생한다. 재생을 멈추거나 상태를 볼 수 있는 손잡이를 돌려준다.
    // gain: 작게 녹음된 소리를 키우는 배율(volume은 1이 최대라서). startOffset/endTime: 클립에서 재생할 구간(초, endTime 0이면 끝까지).
    // fadeOutTime: endTime 직전에 줄이는 시간. loop: 멈출 때까지 반복.
    public static SoundHandle Play(AudioClip clip, SoundChannel channel, float volume = 1f, float gain = 1f,
        float startOffset = 0f, float endTime = 0f, float fadeOutTime = 0f, bool loop = false)
    {
        SoundManager manager = Instance;
        if (clip == null || manager == null)
        {
            return default;
        }

        return manager.PlayInternal(clip, channel, volume, gain, startOffset, endTime, fadeOutTime, loop);
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
        SoundManager manager = Instance;
        if (manager == null)
        {
            return;
        }

        foreach (SoundVoice voice in manager._channels[(int)channel].Voices)
        {
            if (voice.Active)
            {
                voice.Release();
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
        float startOffset, float endTime, float fadeOutTime, bool loop)
    {
        Channel channel = _channels[(int)channelId];
        SoundVoice voice = AcquireVoice(channel);
        AudioClip playClip = GetClip(clip, gain);

        voice.BaseVolume = Mathf.Clamp01(volume);
        voice.EndTime = endTime;
        voice.FadeOutTime = fadeOutTime;
        voice.StartedAt = Time.unscaledTime;
        voice.Active = true;

        AudioSource source = voice.Source;
        source.clip = playClip;
        source.loop = loop;
        source.volume = voice.BaseVolume * GetSoftwareVolume(channel);
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
