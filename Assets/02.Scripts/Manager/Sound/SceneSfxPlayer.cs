using UnityEngine;

[DisallowMultipleComponent]
public class SceneSfxPlayer : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private GameBgmId _bgmId = GameBgmId.Title;
    [SerializeField] private bool _playOnStart = true;

    public bool IsPlaying => SoundManager.IsBgmIdPlaying(_bgmId);
    private void Start()
    {
        if (_playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        GameSoundData.BgmEntry bgm = SoundManager.Data != null ? SoundManager.Data.GetBgm(_bgmId) : null;
        if (bgm == null || (bgm.IntroClip == null && bgm.LoopClip == null))
        {
            Debug.LogWarning($"{name}: GameSoundData의 {_bgmId} BGM 클립이 할당되지 않았습니다.", this);
            return;
        }

        SoundManager.PlayBgm(_bgmId);
    }

    public void Stop()
    {
        // 이 오브젝트가 재생한 곡만 정지한다.
        SoundManager.StopBgm(_bgmId);
    }

    // 버튼/씬 진행 UnityEvent에도 연결할 수 있다.
    public void PlayGameStart() => SoundManager.Play(GameSoundId.GameStart);
    public void PlayStageClear() => SoundManager.Play(GameSoundId.StageClear);
    public void PlayRoomPortal() => SoundManager.Play(GameSoundId.RoomPortal);
    public void PlayFloorMove() => SoundManager.Play(GameSoundId.FloorMove);

    // 씬이 사라져도 곡은 유지한다. 다음 씬의 Play()가 곡을 전환한다.
}
