using System.Collections;
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

    [Header("BGM")]
    [SerializeField] private GameBgmId _bgmId = GameBgmId.InGame;
    [SerializeField, Min(0f)] private float _bgmDelay = 2f;

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
        ScoreManager.Instance?.BeginRun();

        if (!ValidateReferences())
        {
            return;
        }

        if (!_stageManager.TryInitializeStage())
        {
            Debug.LogError($"{nameof(LevelManager)}: 최초 Stage 생성에 실패했습니다.", this);
        }
        else
        {
            StartCoroutine(PlayBgmAfterDelay());
        }


    }

    private IEnumerator PlayBgmAfterDelay()
    {
        if (_bgmDelay > 0f)
            yield return new WaitForSecondsRealtime(_bgmDelay);

        PlayerStatus status = _player != null ? _player.GetComponent<PlayerStatus>() : null;
        if (isActiveAndEnabled && status != null && !status.IsDead)
            SoundManager.PlayBgm(_bgmId);
    }

    public bool TryCompleteStage()
    {
        if (!CanCompleteStage)
        {
            return false;
        }

        _isTransitioning = true;

        bool accepted = _visualEffectManager.TryFadeTransition(HandleScreenCovered,
            onCompleted: HandleTransitionCompleted, whileCovered: WaitForCameraAfterStageChange());

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
            Debug.LogError(
                $"{nameof(LevelManager)}: 플레이어를 StartMap으로 이동하지 못했습니다.",
                this);
            return;
        }

        if (!_stageManager.TryRegenerateStage())
        {
            Debug.LogError(
                $"{nameof(LevelManager)}: Stage 재생성에 실패했습니다.",
                this);
            return;
        }

        // 다음 층 생성에 성공한 경우에만 클리어 기록을 증가시킵니다.
        // 이 처리는 검은 화면에서 실행되므로 페이드 인 완료 시 새 층 번호를 읽습니다.
        ScoreManager.Instance?.TryRecordFloorCleared();

        SoundManager.Play(GameSoundId.FloorMove);
    }

    private IEnumerator WaitForCameraAfterStageChange()
    {
        CameraController cameraController = CameraController.Instance;
        if (cameraController == null)
        {
            Debug.LogWarning($"{nameof(LevelManager)}: 카메라를 찾지 못해 층 이동 카메라 대기를 생략합니다.", this);
            yield return null;
            yield break;
        }

        yield return cameraController.WaitForTrackingSettled();
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
