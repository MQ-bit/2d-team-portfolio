using UnityEngine;

// 타격 순간 게임 시간을 잠깐 멈추거나 느리게 해서 타격감을 준다(히트스톱).
// 씬에 따로 배치하지 않아도 처음 Request가 호출될 때 스스로 생성된다.
// 히트스톱이 겹치면 더 늦게 끝나는 시간, 더 느린 배속을 따른다.
public class HitStop : MonoBehaviour
{
    private static HitStop _instance;

    private bool _isStopping;
    private float _endTime;
    private float _requestedTimeScale = 1f;
    private GameTimeController _gameTimeController;

    // duration은 실제 시간(초) 기준. timeScale이 0이면 완전히 멈춘다.
    public static void Request(float duration, float timeScale = 0f)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (_instance == null)
        {
            _instance = new GameObject(nameof(HitStop)).AddComponent<HitStop>();
        }

        _instance.Begin(duration, Mathf.Clamp01(timeScale));
    }

    private void Begin(float duration, float timeScale)
    {
        float endTime = Time.unscaledTime + duration;

        if (!_isStopping)
        {
            _isStopping = true;
            _gameTimeController = GameTimeController.Instance;
            _requestedTimeScale = timeScale;
            _endTime = endTime;
            _gameTimeController.RequestTimeScale(this, _requestedTimeScale);
            return;
        }

        _endTime = Mathf.Max(_endTime, endTime);
        // 다른 시간 변경 요청을 가져오지 않고 히트스톱 자신의 배율만 비교한다.
        _requestedTimeScale = Mathf.Min(_requestedTimeScale, timeScale);
        _gameTimeController.RequestTimeScale(this, _requestedTimeScale);
    }

    // Update는 timeScale이 0이어도 매 프레임 호출된다.
    private void Update()
    {
        if (_isStopping && Time.unscaledTime >= _endTime)
        {
            Restore();
        }
    }

    // 히트스톱 도중 씬이 바뀌어 파괴돼도 시간이 멈춘 채로 남지 않게 한다.
    private void OnDisable()
    {
        Restore();
    }

    private void Restore()
    {
        if (!_isStopping)
        {
            return;
        }

        _isStopping = false;
        if (_gameTimeController != null)
        {
            _gameTimeController.ReleaseTimeScale(this);
        }

        _requestedTimeScale = 1f;
    }
}
