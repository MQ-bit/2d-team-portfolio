using UnityEngine;

public class ScoreManager : MonoBehaviour
{
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

    [SerializeField] private ScoreData _bestScoreData;
    [SerializeField] private ScoreData _currentScoreData;

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
    }

    public void ReadBestScore()
    {
        //TODO : 최고기록점수 읽기 추가
    }

}
