using System;
using System.Collections.Generic;
using UnityEngine;

public class GameTimeController : MonoBehaviour
{
    private static GameTimeController _instance;

    // 씬에 배치한 컨트롤러를 공유하고, 없으면 처음 사용할 때 생성한다.
    public static GameTimeController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameTimeController>();
                if (_instance == null)
                {
                    _instance = new GameObject(nameof(GameTimeController)).AddComponent<GameTimeController>();
                }
            }

            return _instance;
        }
    }

    [Header("Time Scale")]
    [SerializeField, Range(0f, 1f)] private float _normalTimeScale = 1f;

    private readonly Dictionary<object, float> _timeScaleRequests = new();

    public int RequestCount => _timeScaleRequests.Count;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("GameTimeController는 하나만 사용합니다. 중복 컴포넌트를 제거합니다.", this);
            Destroy(this);
            return;
        }

        _instance = this;
    }

    // 같은 owner가 다시 요청하면 기존 요청의 배율을 변경한다.
    public void RequestTimeScale(object owner, float timeScale)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        if (float.IsNaN(timeScale) || float.IsInfinity(timeScale) || timeScale < 0f || timeScale > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(timeScale), "Time scale must be between 0 and 1.");
        }

        _timeScaleRequests[owner] = timeScale;
        ApplyTimeScale();
    }

    // 다른 owner의 요청은 유지하고, 전달받은 owner의 요청만 해제한다.
    public bool ReleaseTimeScale(object owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        if (!_timeScaleRequests.Remove(owner))
        {
            return false;
        }

        ApplyTimeScale();
        return true;
    }

    private void ApplyTimeScale()
    {
        float timeScale = _normalTimeScale;
        foreach (float requestedTimeScale in _timeScaleRequests.Values)
        {
            timeScale = Mathf.Min(timeScale, requestedTimeScale);
        }

        Time.timeScale = timeScale;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            // 비활성화 시에는 요청을 유지하고, 씬 종료 등 실제 파괴 시에만 정리한다.
            _timeScaleRequests.Clear();
            Time.timeScale = _normalTimeScale;
            _instance = null;
        }
    }
}
