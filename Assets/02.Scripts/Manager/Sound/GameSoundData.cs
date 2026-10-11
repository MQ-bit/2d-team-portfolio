using System;
using UnityEngine;

public enum GameBgmId { Title, InGame, GameEnd }

public enum GameSoundId
{
    GameStart, StageClear, RoomPortal, FloorMove,
    PlayerSwing, PlayerMeleeHit, PlayerDash, PlayerPickup, PlayerDrink, PlayerFootstep,
    TitleClick, TitleHover, PauseClick, PauseHover, VolumeFeedback,

    EnemyExplosion = 16, EnemyAttack,
    FireArea, PoisonArea, FrostArea, LifeArea, LightningArea, VaporizationArea,
    VaporizationReaction,
    PotionBoxOpen = 26,
    PotionCombination = 27,
    PlayerDeath = 28,
    Tomb = 29,
    ScoreCount = 30,
    ReturnToTitle = 31,
    GameExit = 32,
    PlayerFootstepB = 33,
    PlayerHurtA = 34,

    ReaperPotionSummon = 35,
    ReaperAttack = 36,
    ReaperDash = 37,
    ReaperDashCharge = 38,
    ReaperPotionSteal = 39,
    PotionBreak = 40,
    PotionBreakB = 41,
    PotionBreakC = 42
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
        [Tooltip("루프가 꺼져 있을 때 지정한 구간을 재생하는 횟수입니다. 1이면 한 번만 재생합니다.")]
        [SerializeField, Min(1)] private int _repeatCount = 1;
        [SerializeField, Min(0f)] private float _fadeInTime;
        [Tooltip("호버·확인음·피격음의 최소 간격 또는 발소리 반복 간격. 타이머는 재생 담당 객체가 관리합니다.")]
        [SerializeField, Min(0f)] private float _interval;

        public GameSoundId Id => _id;
        public AudioClip Clip => _clip;
        public float Volume => _volume;
        public float EndTime => _endTime;
        public float Interval => Mathf.Max(0f, _interval);

        public void Preload() => SoundManager.Preload(_clip, _gain);
        public SoundHandle Play()
        {
            SoundHandle handle = SoundManager.Play(_clip, _channel, _volume, _gain,
                _startOffset, _endTime, _fadeOutTime, _loop, _fadeInTime);
            if (!_loop) handle.SetRepeatCount(Mathf.Max(1, _repeatCount));
            return handle;
        }
    }

    [Serializable]
    public sealed class BgmEntry
    {
        [SerializeField] private GameBgmId _id;
        [Tooltip("비워두면 루프 곡부터 재생합니다.")]
        [SerializeField] private AudioClip _introClip;
        [SerializeField] private AudioClip _loopClip;
        [SerializeField] private bool _loop = true;
        [Tooltip("클립 재생 종료 지점(초). 0이면 끝까지 재생합니다.")]
        [SerializeField, Min(0f)] private float _endTime;
        [Tooltip("종료 지점 직전 볼륨을 줄이는 시간(초).")]
        [SerializeField, Min(0f)] private float _fadeOutTime;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [Tooltip("BGM 시작 페이드인과 인게임 곡 종료 전 크로스페이드 시간(초).")]
        [SerializeField, Min(0f)] private float _fadeTime = 1f;

        public GameBgmId Id => _id;
        public AudioClip IntroClip => _introClip;
        public AudioClip LoopClip => _loopClip;
        public bool Loop => _loop;
        public float EndTime => Mathf.Max(0f, _endTime);
        public float FadeOutTime => Mathf.Max(0f, _fadeOutTime);
        public float Volume => _volume;
        public float FadeTime => Mathf.Max(0f, _fadeTime);
    }

    [Header("BGM")]
    [SerializeField] private GameBgmData _bgm;
    [Header("씬 · 진행 · 상호작용")]
    [SerializeField] private SoundCategoryData _scene;
    [Header("플레이어")]
    [SerializeField] private SoundCategoryData _player;
    [Header("UI")]
    [SerializeField] private SoundCategoryData _ui;
    [Header("적 종류별 사운드")]
    [SerializeField] private SoundCategoryData[] _enemies = Array.Empty<SoundCategoryData>();
    [Header("포션 · 기화")]
    [SerializeField] private SoundCategoryData _potion;

    public Entry Get(GameSoundId id) => Find(_scene, id) ?? Find(_player, id) ?? Find(_ui, id)
        ?? FindEnemySound(id) ?? Find(_potion, id);

    private Entry FindEnemySound(GameSoundId id)
    {
        if (_enemies == null) return null;
        foreach (SoundCategoryData category in _enemies)
        {
            Entry entry = Find(category, id);
            if (entry != null) return entry;
        }
        return null;
    }

    private static Entry Find(SoundCategoryData category, GameSoundId id) => category != null ? category.Get(id) : null;

    public BgmEntry GetBgm(GameBgmId id) => _bgm != null ? _bgm.Get(id) : null;

    // 같은 ID로 등록된 곡들은 하나의 재생 목록입니다.
    public BgmEntry[] GetBgmTracks(GameBgmId id) => _bgm != null ? _bgm.GetTracks(id) : Array.Empty<BgmEntry>();

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
