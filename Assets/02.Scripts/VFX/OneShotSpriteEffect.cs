using UnityEngine;

// 스프라이트 프레임 목록을 한 번만 재생하고 사라진다. (루프 재생은 SpriteSheetPlayer)
// 게임 시간 기준이라 히트스톱 중에는 같이 멈춘다.
[RequireComponent(typeof(SpriteRenderer))]
public class OneShotSpriteEffect : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private Sprite[] _frames;
    private float _fps;
    private float _elapsed;

    public SpriteRenderer Renderer => _renderer;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        Stop();
    }

    // 재생 중에 다시 호출하면 처음부터 다시 재생한다.
    public void Play(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0 || fps <= 0f)
        {
            Stop();
            return;
        }

        _frames = frames;
        _fps = fps;
        _elapsed = 0f;

        _renderer.sprite = _frames[0];
        _renderer.enabled = true;
        enabled = true;
    }

    public void Stop()
    {
        _frames = null;
        _renderer.enabled = false;
        enabled = false;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        int frameIndex = (int)(_elapsed * _fps);
        if (frameIndex >= _frames.Length)
        {
            Stop();
            return;
        }

        _renderer.sprite = _frames[frameIndex];
    }
}
