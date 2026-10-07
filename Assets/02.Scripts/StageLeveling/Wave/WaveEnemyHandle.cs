using System;
using UnityEngine;

public class WaveEnemyHandle : MonoBehaviour
{
    [SerializeField] private EnemyStatus _status;

    private bool _released;
    public event Action<WaveEnemyHandle> Released;

    private void Awake()
    {
        if (_status == null)
        {
            _status = GetComponent<EnemyStatus>();
        }
    }

    private void OnEnable()
    {
        _released = false;

        if (_status != null)
        {
            _status.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (_status != null)
        {
            _status.OnDeath -= HandleDeath;
        }

        NotifyReleased();

    }

    public void NotifyReleased()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        Released?.Invoke(this);
    }

    private void HandleDeath()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning($"{name} : ScoreManager 가 존재하지 않습니다!. 점수 기록 실패");
            return;
        }
        ScoreManager.Instance.TryRecordEnemyKill(_status.EnemyType);
    }
}