using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private const string Best_Score_Save_Key = "ScoreManager.Best.v1";
    private static ScoreManager _instance;
    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<ScoreManager>();
            }

            return _instance;
        }
    }

    [Header("Runtime Score")]
    [SerializeField] private ScoreData _bestScoreData;
    [SerializeField] private ScoreData _currentScoreData;

    public bool IsRunActive = false;

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

        ReadBestScore();
        //BeginRun();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void BeginRun()
    {
        _currentScoreData = new ScoreData();
        IsRunActive = true;

    }

    public bool EndRun()
    {
        if (!IsRunActive) return false;
        IsRunActive = false;

        if (!CheckBreakBestScore()) return false;

        _bestScoreData = _currentScoreData.Clone();
        SaveBestScore();

        return true;
    }

    private bool CheckBreakBestScore()
    {
        if (_currentScoreData == null)
        {
            return false;
        }

        if (_bestScoreData == null)
        {
            return true;
        }

        return _currentScoreData.ClearedFloorCount > _bestScoreData.ClearedFloorCount
            || _currentScoreData.EliteKillCount > _bestScoreData.EliteKillCount
            || _currentScoreData.NormalKillCount > _bestScoreData.NormalKillCount
            || _currentScoreData.ConsumedPotionCount > _bestScoreData.ConsumedPotionCount;
    }

    public bool TryRecordEnemyKill(EnemyKind kind)
    {
        Debug.Log($"{name} : KillRecored Func called");
        if (!IsRunActive) return false;
        Debug.Log($"{name} : Record Start");
        return _currentScoreData.TryAddEnemyKill(kind, 1);
    }

    public bool TryRecordPotionConsumed()
    {
        if (!IsRunActive) return false;
        return _currentScoreData.TryAddConsumedPotion(1);
    }

    public bool TryRecordFloorCleared()
    {
        if (!IsRunActive) return false;
        return _currentScoreData.TryAddClearedFloor(1);
    }

    public void ReadBestScore()
    {
        _bestScoreData = new ScoreData();

        string json = PlayerPrefs.GetString(Best_Score_Save_Key, string.Empty);

        ScoreData loadedData = JsonUtility.FromJson<ScoreData>(json);

        if (loadedData != null)
        {
            _bestScoreData = loadedData;
        }

    }

    private void SaveBestScore()
    {
        string json = JsonUtility.ToJson(_bestScoreData);
        PlayerPrefs.SetString(Best_Score_Save_Key, json);
        PlayerPrefs.Save();
    }
}
