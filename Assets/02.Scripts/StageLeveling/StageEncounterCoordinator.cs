using System;
using UnityEngine;

public class StageEncounterCoordinator : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private CombatRoomProgressController _roomProgress;

    [Header("Wave")]
    [SerializeField] private WavePlanSO _wavePlan;
    [SerializeField] private WaveController _waveController;
    [SerializeField] private RoomWaveStartTrigger _waveStartTrigger;

    private GeneratedStage _currentStage;
    private bool _isSubscribed;

    public GeneratedRoomNode CurrentRoom => _roomProgress != null ? _roomProgress.CurrentRoom : null;

    public bool IsCompleted => _roomProgress != null && _roomProgress.IsCompleted;
    public int CombatRoomCount => _roomProgress != null ? _roomProgress.CombatRoomCount : 0;

    public StageState State { get; private set; } = StageState.Initializing;

    public event Action<StageState> StateChanged;
    public event Action<GeneratedRoomNode> RoomEntered;
    public event Action<GeneratedRoomNode> RoomCleared;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeWaveEvents();
    }

    private void OnDisable()
    {
        UnsubscribeWaveEvents();
        Suspend();
    }

    public bool TryValidateConfiguration(out string error)
    {
        ResolveReferences();
        SubscribeWaveEvents();

        if (_roomProgress == null)
        {
            error = "CombatRoomProgressController 참조가 없습니다.";
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

    public void BeginInitialization()
    {
        SetState(StageState.Initializing);
    }

    public bool TryBind(GeneratedStage stage, out string error)
    {
        if (!TryValidateConfiguration(out error))
        {
            return false;
        }

        if (stage == null)
        {
            error = "GeneratedStage가 null입니다.";
            return false;
        }

        ClearRuntime(true);
        _currentStage = stage;

        if (!_roomProgress.TryInitialize(stage, out GeneratedRoomNode firstRoom, out error))
        {
            return false;
        }

        BindWaveTrigger(firstRoom);
        SetState(StageState.Exploring);

        error = null;
        return true;
    }

    public void ClearRuntime(bool destroyTrackedEnemies)
    {
        _waveController?.CancelEncounter(destroyTrackedEnemies);
        _waveStartTrigger?.Unbind();
        _roomProgress?.Clear();

        _currentStage = null;

        SetState(StageState.Initializing);
    }

    public void Suspend()
    {
        _waveController?.CancelEncounter(false);
        _waveStartTrigger?.Disarm();
    }

    public void TryEnterCombatRoom(GeneratedRoomNode node)
    {
        if (State != StageState.Exploring || node == null ||
            !_roomProgress.TryGetRoomProgress(node,out RoomProgressState progress) ||
            progress != RoomProgressState.Prepared)
        {
            return;
        }

        if (!_wavePlan.TryResolve(node.Room.Type, CreateEncounterSeed(node), out ResolvedWavePlan plan, out string resolveError))
        {
            Debug.LogError($"{node.Room.name}: Wave 데이터 확정 실패: {resolveError}");

            CompleteRoom(node);
            return;
        }

        if (node.Room.EnemySpawnPoints == null || node.Room.EnemySpawnPoints.Count == 0)
        {
            Debug.LogError($"{node.Room.name}: EnemySpawnPoint가 없습니다.");

            CompleteRoom(node);
            return;
        }

        if (!_roomProgress.TryBeginCombat(node))
        {
            return;
        }

        SetState(StageState.InCombat);
        RoomEntered?.Invoke(node);

        bool started = _waveController.TryStartEncounter(plan, node.Room.EnemySpawnPoints, _currentStage.EnemiesRoot);

        if (!started)
        {
            HandleCombatFailed($"{node.Room.name}: WaveController가 전투 시작 요청을 거부했습니다.");
        }
    }

    public bool TryGetRoomProgress(GeneratedRoomNode node, out RoomProgressState progress)
    {
        if (_roomProgress != null)
        {
            return _roomProgress.TryGetRoomProgress(node, out progress);
        }

        progress = RoomProgressState.Unvisited;
        return false;
    }

    private void HandleCombatCompleted()
    {
        if (State != StageState.InCombat || _roomProgress.ActiveRoom == null) return;
        
        CompleteRoom(_roomProgress.ActiveRoom);
    }

    private void HandleCombatFailed(string reason)
    {
        Debug.LogError($"전투 진행 중 오류가 발생했습니다: {reason}");

        GeneratedRoomNode activeRoom = _roomProgress.ActiveRoom;

        if (activeRoom == null)
        {
            SetState(StageState.Exploring);
            return;
        }

        CompleteRoom(activeRoom);
    }

    private void CompleteRoom(GeneratedRoomNode node)
    {
        if (!_roomProgress.TryClearRoom(node, out GeneratedRoomNode nextRoom)) return;

        SetState(StageState.RoomCleared);
        RoomCleared?.Invoke(node);

        if (nextRoom != null)
        {
            BindWaveTrigger(nextRoom);
        }
        else
        {
            _waveStartTrigger.Unbind();
            Debug.Log("Stage의 모든 전투 방이 완료됐습니다.");
        }

        SetState(StageState.Exploring);
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

    private int CreateEncounterSeed(GeneratedRoomNode node)
    {
        unchecked //Overflow를 무시하고 계산하여 동일한 시드값을 생성
        {
            int seed = _currentStage.Seed;
            return seed * 397 ^ node.Index;
        }
    }

    private void SetState(StageState nextState)
    {
        if (State == nextState) return;

        State = nextState;
        StateChanged?.Invoke(State);
    }

    private void ResolveReferences()
    {
        if (_roomProgress == null)
        {
            _roomProgress = GetComponent<CombatRoomProgressController>();
        }

        if (_waveController == null)
        {
            _waveController = GetComponent<WaveController>();
        }
    }

    private void SubscribeWaveEvents()
    {
        if (_isSubscribed || _waveController == null || !isActiveAndEnabled)
        {
            return;
        }

        _waveController.CombatCompleted += HandleCombatCompleted;
        _waveController.CombatFailed += HandleCombatFailed;

        _isSubscribed = true;
    }

    private void UnsubscribeWaveEvents()
    {
        if (!_isSubscribed) return;
        if (_waveController != null)
        {
            _waveController.CombatCompleted -= HandleCombatCompleted;
            _waveController.CombatFailed -= HandleCombatFailed;
        }

        _isSubscribed = false;
    }
}