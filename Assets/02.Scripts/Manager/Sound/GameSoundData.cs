using System;
using UnityEngine;

public enum GameBgmId { Title, Dungeons, Catacombs, Caverns, Boss }

public enum GameSoundId
{
    GameStart, StageClear, RoomPortal, FloorMove,
    PlayerSwing, PlayerMeleeHit, PlayerDash, PlayerPickup, PlayerDrink, PlayerFootstep,
    TitleClick, TitleHover, PauseClick, PauseHover, VolumeFeedback, PausePrefabClick,
    EnemyExplosion, EnemyAttack,
    FireArea, PoisonArea, FrostArea, LifeArea, LightningArea, VaporizationArea,
    VaporizationReaction, VaporizationReactionAlternative,
    PotionBoxOpen
}

[CreateAssetMenu(fileName = "GameSoundData", menuName = "Sound/Game Sound Data")]
public sealed class GameSoundData : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private GameSoundId _id;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private SoundChannel _channel = SoundChannel.Sfx;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField, Min(1f)] private float _gain = 1f;
        [SerializeField, Min(0f)] private float _startOffset;
        [Tooltip("0이면 클립 끝까지 재생합니다.")]
        [SerializeField, Min(0f)] private float _endTime;
        [SerializeField, Min(0f)] private float _fadeOutTime;
        [SerializeField] private bool _loop;
        [SerializeField, Min(0f)] private float _fadeInTime;
        [Tooltip("호버·확인음의 최소 간격 또는 발소리 반복 간격. 타이머는 재생 담당 객체가 관리합니다.")]
        [SerializeField, Min(0f)] private float _interval;

        public GameSoundId Id => _id;
        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public float EndTime => _endTime;
        public float Interval => Mathf.Max(0f, _interval);

        public void Preload() => SoundManager.Preload(_clip, _gain);
        public SoundHandle Play() => SoundManager.Play(_clip, _channel, _volume, _gain,
            _startOffset, _endTime, _fadeOutTime, _loop, _fadeInTime);
    }

    [Serializable]
    public sealed class BgmEntry
    {
        [SerializeField] private GameBgmId _id;
        [Tooltip("비워두면 루프 곡부터 재생합니다.")]
        [SerializeField] private AudioClip _introClip;
        [SerializeField] private AudioClip _loopClip;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField, Min(0f)] private float _fadeTime = 1f;

        public GameBgmId Id => _id;
        public AudioClip IntroClip => _introClip;
        public AudioClip LoopClip => _loopClip;
        public float Volume => _volume;
        public float FadeTime => Mathf.Max(0f, _fadeTime);
    }

    [Header("BGM")]
    [SerializeField] private BgmEntry[] _bgm = Array.Empty<BgmEntry>();
    [Header("씬 · 진행")]
    [SerializeField] private Entry[] _scene = Array.Empty<Entry>();
    [Header("플레이어")]
    [SerializeField] private Entry[] _player = Array.Empty<Entry>();
    [Header("UI")]
    [SerializeField] private Entry[] _ui = Array.Empty<Entry>();
    [Header("적")]
    [SerializeField] private Entry[] _enemy = Array.Empty<Entry>();
    [Header("포션 · 기화")]
    [SerializeField] private Entry[] _potion = Array.Empty<Entry>();
    [Header("상호작용")]
    [SerializeField] private Entry[] _interaction = Array.Empty<Entry>();

    public Entry Get(GameSoundId id) => Find(_scene, id) ?? Find(_player, id) ?? Find(_ui, id)
        ?? Find(_enemy, id) ?? Find(_potion, id) ?? Find(_interaction, id);

    private static Entry Find(Entry[] entries, GameSoundId id)
    {
        if (entries == null) return null;
        foreach (Entry entry in entries)
            if (entry != null && entry.Id == id) return entry;
        return null;
    }

    public BgmEntry GetBgm(GameBgmId id)
    {
        if (_bgm == null) return null;
        foreach (BgmEntry entry in _bgm)
            if (entry != null && entry.Id == id) return entry;
        return null;
    }

    public static GameSoundId GetAreaSoundId(PotionElement element) => element switch
    {
        PotionElement.Fire => GameSoundId.FireArea,
        PotionElement.Poison => GameSoundId.PoisonArea,
        PotionElement.Ice => GameSoundId.FrostArea,
        PotionElement.Heal => GameSoundId.LifeArea,
        PotionElement.Lightning => GameSoundId.LightningArea,
        _ => GameSoundId.VaporizationArea
    };
}
