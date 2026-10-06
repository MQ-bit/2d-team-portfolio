using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageManager : MonoBehaviour
{
    private sealed class RoomRuntime
    {
        public GeneratedRoomNode Node { get; }
        public RoomEncounterController Encounter { get; }
        public RoomProgressState Progress { get; set; }

        public RoomRuntime(GeneratedRoomNode node, RoomEncounterController encounter)
        {
            Node = node;
            Encounter = encounter;
            Progress = RoomProgressState.Unvisited;
        }
    }

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
    [SerializeField] private WavePlanSO _wavePlan;

    [Header("Combat")]
    [SerializeField] private WaveController _waveController;
    [SerializeField] private RoomWaveStartTrigger _waveStartTrigger;

    private readonly Dictionary<GeneratedRoomNode, RoomRuntime> _roomRuntimeByNode = new Dictionary<GeneratedRoomNode, RoomRuntime>();
    private readonly List<RoomRuntime> _combatRooms = new List<RoomRuntime>();

    private RoomRuntime _currentCombatRoom;

    public GeneratedStage CurrentStage { get; private set; }
    public GeneratedRoomNode CurrentRoom { get; private set; }

    public StageState State { get; private set; } = StageState.Initializing;

    public event Action<StageState> StateChanged;
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
        if (_instance != this || _waveController == null)
        {
            return;
        }

        _waveController.CombatCompleted += HandleCombatCompleted;
        _waveController.CombatFailed += HandleCombatFailed;
    }

    private void Start()
    {
        if (_instance != this)
        {
            return;
        }

        InitializeStage();
    }

    private void OnDisable()
    {
        if (_instance != this)
        {
            return;
        }

        if (_waveController != null)
        {
            _waveController.CombatCompleted -= HandleCombatCompleted;
            _waveController.CombatFailed -= HandleCombatFailed;
            _waveController.CancelEncounter(false);
        }

        _waveStartTrigger?.Disarm();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void InitializeStage()
    {
        SetState(StageState.Initializing);

        if (!ValidateReferences(out string referenceError))
        {
            Debug.LogError($"{nameof(StageManager)} 초기화 실패: {referenceError}");
            return;
        }

        GeneratedStage stage = _stageGenerator.CurrentStage;

        if (stage == null && !_stageGenerator.TryGenerateConfigured(out stage, out StageFailure failure))
        {
            Debug.LogError($"Stage 생성 실패 [{failure.Code}]: {failure.Message}");
            return;
        }

        if (!TryBindGeneratedStage(stage, out string bindError))
        {
            Debug.LogError($"Stage 전투 시스템 초기화 실패: {bindError}");
            return;
        }

        SetState(StageState.Exploring);

        Debug.Log($"Stage 전투 시스템 초기화 완료: CombatRooms={_combatRooms.Count}");
    }

    private bool TryBindGeneratedStage(GeneratedStage stage, out string error)
    {
        if (stage == null)
        {
            error = "GeneratedStage가 null입니다.";
            return false;
        }

        if (stage.RoomNodes == null || stage.RoomNodes.Count == 0)
        {
            error = "GeneratedStage에 RoomNodes가 없습니다.";
            return false;
        }

        _roomRuntimeByNode.Clear();
        _combatRooms.Clear();
        _currentCombatRoom = null;

        CurrentStage = stage;
        CurrentRoom = null;

        foreach (GeneratedRoomNode node in stage.RoomNodes)
        {
            if (!IsCombatRoom(node))
            {
                continue;
            }

            RoomEncounterController encounter = node.Room.GetComponent<RoomEncounterController>();

            if (encounter == null)
            {
                error = $"{node.Room.name}: {nameof(RoomEncounterController)}가 없습니다.";
                return false;
            }

            if (!encounter.TryInitialize(node, out string encounterError))
            {
                error = encounterError;
                return false;
            }

            RoomRuntime runtime = new RoomRuntime(node, encounter);

            _roomRuntimeByNode.Add(node, runtime);
            _combatRooms.Add(runtime);
        }

        if (_combatRooms.Count == 0)
        {
            error = "생성된 Stage에 전투 방이 없습니다.";
            return false;
        }

        // 모든 전투 방의 초기 문 상태: 입구 Open, 출구 Closed
        foreach (RoomRuntime room in _combatRooms)
        {
            room.Progress = RoomProgressState.Unvisited;
            room.Encounter.SetWaiting();
        }

        RoomRuntime firstRoom = _combatRooms[0];
        firstRoom.Progress = RoomProgressState.Prepared;

        BindWaveTrigger(firstRoom.Node);

        error = null;
        return true;
    }

    public void TryEnterCombatRoom(GeneratedRoomNode node)
    {
        if (State != StageState.Exploring || node == null ||
            !_roomRuntimeByNode.TryGetValue(node, out RoomRuntime runtime) ||
            runtime.Progress != RoomProgressState.Prepared)
        {
            return;
        }

        if (!_wavePlan.TryResolve(node.Room.Type, CreateEncounterSeed(node), out ResolvedWavePlan resolvedPlan, out string resolveError))
        {
            Debug.LogError($"{node.Room.name}: Wave 데이터 확정 실패: {resolveError}");
            FailOpenRoom(runtime);
            return;
        }

        if (node.Room.EnemySpawnPoints == null || node.Room.EnemySpawnPoints.Count == 0)
        {
            Debug.LogError($"{node.Room.name}: EnemySpawnPoint가 없습니다.");
            FailOpenRoom(runtime);
            return;
        }

        CurrentRoom = node;
        _currentCombatRoom = runtime;

        runtime.Progress = RoomProgressState.InCombat;
        SetState(StageState.InCombat);

        RoomEntered?.Invoke(node);

        runtime.Encounter.SetCombat();

        bool started = _waveController.TryStartEncounter(resolvedPlan, node.Room.EnemySpawnPoints, CurrentStage.EnemiesRoot);

        if (!started)
        {
            HandleCombatFailed($"{node.Room.name}: WaveController가 전투 시작 요청을 거부했습니다.");
        }
    }

    private void HandleCombatCompleted()
    {
        if (State != StageState.InCombat || _currentCombatRoom == null)
        {
            return;
        }

        CompleteCurrentRoom();
    }

    private void HandleCombatFailed(string reason)
    {
        Debug.LogError($"전투 진행 중 오류가 발생했습니다: {reason}");

        if (_currentCombatRoom == null)
        {
            SetState(StageState.Exploring);
            return;
        }

        CompleteCurrentRoom();
    }

    private void CompleteCurrentRoom()
    {
        RoomRuntime completedRoom = _currentCombatRoom;
        _currentCombatRoom = null;

        completedRoom.Progress = RoomProgressState.Cleared;
        SetState(StageState.RoomCleared);

        completedRoom.Encounter.SetCleared();

        RoomCleared?.Invoke(completedRoom.Node);

        PrepareNextCombatRoom();
        SetState(StageState.Exploring);
    }

    private void PrepareNextCombatRoom()
    {
        foreach (RoomRuntime room in _combatRooms)
        {
            if (room.Progress != RoomProgressState.Unvisited)
            {
                continue;
            }

            room.Progress = RoomProgressState.Prepared;
            BindWaveTrigger(room.Node);

            return;
        }

        _waveStartTrigger.Unbind();
        Debug.Log("Stage의 모든 전투 방이 완료됐습니다.");
    }

    private void BindWaveTrigger(GeneratedRoomNode node)
    {
        if (node?.Entry == null)
        {
            Debug.LogError($"{node?.Room?.name}: Entry Connector가 없습니다.");
            _waveStartTrigger.Unbind();
            return;
        }

        RoomEncounterSocket socket = node.Entry.GetComponent<RoomEncounterSocket>();

        if (socket == null)
        {
            Debug.LogError($"{node.Room.name}/{node.Entry.name}: RoomEncounterSocket이 없습니다.");

            _waveStartTrigger.Unbind();
            return;
        }

        _waveStartTrigger.Bind(node, socket, TryEnterCombatRoom);
        _waveStartTrigger.Arm();
    }

    private void FailOpenRoom(RoomRuntime room)
    {
        room.Progress = RoomProgressState.Cleared;
        room.Encounter.SetCleared();

        RoomCleared?.Invoke(room.Node);

        PrepareNextCombatRoom();
        SetState(StageState.Exploring);
    }

    private int CreateEncounterSeed(GeneratedRoomNode node)
    {
        unchecked //오버플로우 무시
        {
            int seed = CurrentStage.Seed;
            seed = seed * 397 ^ node.Index;
            return seed;
        }
    }

    public bool TryGetRoomProgress(GeneratedRoomNode node, out RoomProgressState progress)
    {
        if (node != null && _roomRuntimeByNode.TryGetValue(node, out RoomRuntime runtime))
        {
            progress = runtime.Progress;
            return true;
        }

        progress = RoomProgressState.Unvisited;
        return false;
    }

    private void SetState(StageState nextState)
    {
        if (State == nextState)
        {
            return;
        }

        State = nextState;
        StateChanged?.Invoke(State);
    }

    private bool ValidateReferences(out string error)
    {
        if (_stageGenerator == null)
        {
            error = "StageGenerator 참조가 없습니다.";
            return false;
        }

        if (_wavePlan == null)
        {
            error = "WavePlanSO 참조가 없습니다.";
            return false;
        }

        if (_waveController == null)
        {
            error = "WaveController 참조가 없습니다.";
            return false;
        }

        if (_waveStartTrigger == null)
        {
            error = "RoomWaveStartTrigger 참조가 없습니다.";
            return false;
        }

        error = null;
        return true;
    }

    private void ResolveReferences()
    {
        if (_stageGenerator == null)
        {
            _stageGenerator = FindAnyObjectByType<StageGenerator>();
        }

        if (_waveController == null)
        {
            _waveController = FindAnyObjectByType<WaveController>();
        }

        if (_waveStartTrigger == null)
        {
            _waveStartTrigger = FindAnyObjectByType<RoomWaveStartTrigger>();
        }
    }

    private static bool IsCombatRoom(GeneratedRoomNode node)
    {
        if (node?.Room == null)
        {
            return false;
        }

        return node.Room.Type == RoomType.Normal || node.Room.Type == RoomType.Elite;
    }
}