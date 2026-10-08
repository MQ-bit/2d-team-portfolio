using UnityEngine;

[DisallowMultipleComponent]
public class SceneSfxPlayer : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    [SerializeField, Min(0f)] private float _fadeTime = 1f;
    [SerializeField] private bool _playOnStart = true;

    private SoundHandle _bgmHandle;

    public bool IsPlaying => _bgmHandle.IsPlaying;

    private void Start()
    {
        if (_playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (_bgmClip == null)
        {
            Debug.LogWarning($"{name}: BGM 클립이 할당되지 않았습니다.", this);
            return;
        }

        SoundManager.Preload(_bgmClip);
        _bgmHandle = SoundManager.PlayBgm(_bgmClip, _volume, _fadeTime);
    }

    public void Stop()
    {
        // 이 오브젝트가 재생한 곡만 정지한다.
        _bgmHandle.FadeOut(_fadeTime);
    }

    // 씬이 사라져도 곡은 유지한다. 다음 씬의 Play()가 곡을 전환한다.
}
