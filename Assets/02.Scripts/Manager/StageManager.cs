using System;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    private static StageManager _instance;

    public static StageManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<StageManager>();
            }

            return _instance;
        }
    }

    [Header("Stage")]
    [SerializeField] private StageGenerator _stageGenerator;

    [Header("Encounter")]
    [SerializeField] private StageEncounterCoordinator _encounterCoordinator;

    [Header("Difficulty")]
    [Tooltip("X: 클리어한 층 수 (첫 층 = 0), Y: 몬스터 최대 체력에 곱해줄 증감치.")]
    [SerializeField] private AnimationCurve _enemyHealthMultiplierCurve = AnimationCurve.Linear(0f, 1f, 50f, 1000f);

    public GeneratedStage CurrentStage { get; private set; }

    public float EnemyHealthMultiplier
    {
        get
        {
            if (_enemyHealthMultiplierCurve == null || _enemyHealthMultiplierCurve.length == 0)
            {
                return 1f;
            }

            int floor = ScoreManager.Instance != null ? ScoreManager.Instance.ClearedFloorCount : 0;
            return Mathf.Max(0.1f, _enemyHealthMultiplierCurve.Evaluate(floor));
        }
    }

    public GeneratedRoomNode CurrentRoom => _encounterCoordinator != null ? _encounterCoordinator.CurrentRoom : null;

    public Transform PlayerSpawnPoint { get; private set; }
    public bool IsStageCleared => _encounterCoordinator != null && _encounterCoordinator.IsCompleted;
    public StageState State => _encounterCoordinator != null ? _encounterCoordinator.State : StageState.Initializing;

    public event Action<GeneratedRoomNode> CombatRoomCleared;
    public event Action<GeneratedStage> CurrentStageChanged;
    public event Action<GeneratedRoomNode> RoomEntered;
    public event Action<GeneratedRoomNode> RoomCleared;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        _instance = this;
        ResolveReferences();
    }

    private void OnEnable()
    {
        if (_instance == this)
        {
            SubscribeEncounterEvents();
        }
    }

    private void OnDisable()
    {
        if (_instance != this)
        {
            return;
        }

        UnsubscribeEncounterEvents();
        _encounterCoordinator?.Suspend();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public bool TryInitializeStage()
    {
        ResolveReferences();
        SubscribeEncounterEvents();
        _encounterCoordinator?.BeginInitialization();

        if (!ValidateReferences(out string referenceError))
        {
            Debug.LogError($"{nameof(StageManager)} 초기화 실패: {referenceError}");
            return false;
        }

        GeneratedStage stage = _stageGenerator.CurrentStage;

        if (stage == null && !_stageGenerator.TryGenerateConfigured(out stage, out StageFailure failure))
        {
            Debug.LogError($"Stage 생성 실패 [{failure.Code}]: {failure.Message}");
            return false;
        }

        if (!TryBindStage(stage, out string bindError))
        {
            Debug.LogError($"Stage 전투 시스템 초기화 실패: {bindError}");
            return false;
        }

        Debug.Log($"Stage 전투 시스템 초기화 완료: CombatRooms={_encounterCoordinator.CombatRoomCount}");
        return true;
    }

    public bool TryRegenerateStage()
    {
        ResolveReferences();
        SubscribeEncounterEvents();
        _encounterCoordinator?.BeginInitialization();

        if (!ValidateReferences(out string referenceError))
        {
            Debug.LogError($"{nameof(StageManager)} 재생성 실패: {referenceError}");
            return false;
        }

        ClearStageRuntime();

        if (!_stageGenerator.TryRegenerateConfigured(out GeneratedStage stage, out StageFailure failure))
        {
            Debug.LogError($"Stage 재생성 실패 [{failure.Code}]: {failure.Message}");
            return false;
        }

        if (!TryBindStage(stage, out string bindError))
        {
            Debug.LogError($"Stage 전투 시스템 재설정 실패: {bindError}");
            return false;
        }

        Debug.Log($"Stage 재생성 완료: Seed={stage.Seed}, CombatRooms={_encounterCoordinator.CombatRoomCount}");
        return true;
    }

    private bool TryBindStage(GeneratedStage stage, out string error)
    {
        if (!TryFindPlayerSpawnPoint(stage, out Transform spawnPoint, out error))
        {
            return false;
        }

        RoomRewardGenerator rewardGenerator = GetComponent<RoomRewardGenerator>();

        if (rewardGenerator == null || !rewardGenerator.isActiveAndEnabled)
        {
            error = "활성화된 RoomRewardGenerator가 없습니다.";
            return false;
        }

        CurrentStage = stage;
        PlayerSpawnPoint = spawnPoint;

        if (!_encounterCoordinator.TryBind(stage, out error) ||
            !rewardGenerator.TrySpawnInitialRewards(stage, out error))
        {
            return false;
        }

        CurrentStageChanged?.Invoke(stage);
        return true;
    }

    private static bool TryFindPlayerSpawnPoint(GeneratedStage stage, out Transform spawnPoint, out string error)
    {
        spawnPoint = null;

        if (stage?.RoomNodes == null || stage.RoomNodes.Count == 0)
        {
            error = "GeneratedStage에 RoomNodes가 없습니다.";
            return false;
        }

        foreach (GeneratedRoomNode node in stage.RoomNodes)
        {
            if (node?.Room == null || node.Room.Type != RoomType.Start)
            {
                continue;
            }

            spawnPoint = node.Room.PlayerSpawnPoint;
            break;
        }

        if (spawnPoint == null)
        {
            error = "StartRoom에 PlayerSpawnPoint가 없습니다.";
            return false;
        }

        error = null;
        return true;
    }

    private bool ValidateReferences(out string error)
    {
        if (_stageGenerator == null)
        {
            error = "StageGenerator 참조가 없습니다.";
            return false;
        }

        if (_encounterCoordinator == null)
        {
            error = "StageEncounterCoordinator 참조가 없습니다.";
            return false;
        }

        return _encounterCoordinator.TryValidateConfiguration(out error);
    }

    private void ResolveReferences()
    {
        if (_stageGenerator == null)
        {
            _stageGenerator = GetComponent<StageGenerator>();
        }

        if (_encounterCoordinator == null)
        {
            _encounterCoordinator = GetComponent<StageEncounterCoordinator>();
        }
    }

    private void ClearStageRuntime()
    {
        _encounterCoordinator?.ClearRuntime(true);

        CurrentStage = null;
        PlayerSpawnPoint = null;

        CurrentStageChanged?.Invoke(null);
    }

    private void SubscribeEncounterEvents()
    {
        if (_encounterCoordinator == null)
        {
            return;
        }

        UnsubscribeEncounterEvents();

        _encounterCoordinator.RoomEntered += HandleRoomEntered;
        _encounterCoordinator.RoomCleared += HandleRoomCleared;
        _encounterCoordinator.CombatRoomCleared += HandleCombatRoomCleared;
    }

    private void UnsubscribeEncounterEvents()
    {
        if (_encounterCoordinator == null)
        {
            return;
        }

        _encounterCoordinator.RoomEntered -= HandleRoomEntered;
        _encounterCoordinator.RoomCleared -= HandleRoomCleared;
        _encounterCoordinator.CombatRoomCleared -= HandleCombatRoomCleared;
    }

    private void HandleRoomEntered(GeneratedRoomNode node)
    {
        // 방 진입으로 출입구가 닫히고 전투가 시작될 때 재생한다.
        SoundManager.Play(GameSoundId.RoomPortal);
        RoomEntered?.Invoke(node);
    }

    private void HandleRoomCleared(GeneratedRoomNode node)
    {
        RoomCleared?.Invoke(node);
    }

    private void HandleCombatRoomCleared(GeneratedRoomNode node)
    {
        CombatRoomCleared?.Invoke(node);
    }
}
