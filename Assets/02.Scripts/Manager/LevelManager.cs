using UnityEngine;

public class LevelManager : MonoBehaviour
{
    private static LevelManager _instance;
    public static LevelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<LevelManager>();
            }

            return _instance;
        }
    }

    [Header("Level")]
    [SerializeField] private StageManager _stageManager;

    [Header("Transition")]
    [SerializeField] private SceneVisualEffectManager _visualEffectManager;

    [Header("Player")]
    [SerializeField] private PlayerController _player;


    private Rigidbody2D _playerRigidbody;
    private bool _isTransitioning;
    public bool CanCompleteStage => isActiveAndEnabled && !_isTransitioning && _stageManager != null && _stageManager.IsStageCleared;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            enabled = false;
            return;
        }

        _instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        if (!ValidateReferences())
        {
            return;
        }

        if (!_stageManager.TryInitializeStage())
        {
            Debug.LogError($"{nameof(LevelManager)}: 최초 Stage 생성에 실패했습니다.", this);
        }
    }

    public bool TryCompleteStage()
    {
        if (!CanCompleteStage)
        {
            return false;
        }

        _isTransitioning = true;

        bool accepted = _visualEffectManager.TryFadeTransition(HandleScreenCovered, -1, 0, -1, HandleTransitionCompleted);

        if (!accepted)
        {
            _isTransitioning = false;
        }

        return accepted;
    }

    private void HandleScreenCovered()
    {
        if (!TryMovePlayerToStart())
        {
            Debug.LogError($"{nameof(LevelManager)}: 플레이어를 StartMap으로 이동하지 못했습니다.");
            return;
        }

        if (!_stageManager.TryRegenerateStage())
        {
            Debug.LogError($"{nameof(LevelManager)}: Stage 재생성에 실패했습니다.");
        }

        ScoreManager.Instance?.TryRecordFloorCleared();
    }

    private void HandleTransitionCompleted()
    {
        _isTransitioning = false;
    }

    private bool TryMovePlayerToStart()
    {
        Transform spawnPoint = _stageManager.PlayerSpawnPoint;

        if (_player == null || spawnPoint == null)
        {
            return false;
        }

        _player.CancelAllActions();

        if (_playerRigidbody != null)
        {
            _playerRigidbody.linearVelocity = Vector2.zero;
            _playerRigidbody.angularVelocity = 0f;
            _playerRigidbody.position = spawnPoint.position;
        }
        else
        {
            _player.transform.position = spawnPoint.position;
        }

        return true;
    }

    private void ResolveReferences()
    {
        if (_stageManager == null)
        {
            _stageManager = FindAnyObjectByType<StageManager>();

            if (_stageManager == null)
            {
                Debug.LogError($"{nameof(LevelManager)}: {nameof(StageManager)}를 찾지 못했습니다");
            }
        }

        if (_visualEffectManager == null)
        {
            _visualEffectManager = FindAnyObjectByType<SceneVisualEffectManager>();
        }

        if (_player == null)
        {
            _player = FindAnyObjectByType<PlayerController>();
        }

        if (_player != null)
        {
            _playerRigidbody = _player.GetComponent<Rigidbody2D>();
        }
    }

    private bool ValidateReferences()
    {
        if (_stageManager == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: {nameof(StageManager)}가 없습니다.");

            return false;
        }

        if (_visualEffectManager == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: {nameof(SceneVisualEffectManager)}가 없습니다.");
            return false;
        }

        if (_player == null)
        {
            Debug.LogError($"{nameof(LevelManager)}: {nameof(PlayerController)}가 없습니다.");

            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}