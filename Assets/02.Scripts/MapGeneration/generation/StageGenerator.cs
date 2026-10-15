using UnityEngine;

/// <summary>
///  StageLayoutPlanner와 StageMaterializer를 사용하여 Stage를 생성하는 컴포넌트.
/// </summary>
public class StageGenerator : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private RoomDefinition _startRoom;
    [SerializeField] private Transform _generatedStageParent;

    [Header("Profile")]
    [SerializeField] private StageGenerationProfileSO _profile;

    [Header("Runtime")]
    [SerializeField] private bool _generateOnStart = true;

    private ConnectorWallSnapshot _currentStartWallSnapshot;

    public GeneratedStage CurrentStage { get; private set; }

    private void Start()
    {
        if (_generateOnStart)
        {
            GenerateConfiguredStage();
        }
    }

    [ContextMenu("Generate Configured Stage")]
    public void GenerateConfiguredStage()
    {
        if (!TryGenerateConfigured(out GeneratedStage stage, out StageFailure failure))
        {
            Debug.LogError($"Stage 생성 실패 [{failure.Code}]: {failure.Message}");

            return;
        }

        Debug.Log($"Stage 생성 성공: Rooms={stage.Rooms.Count}, Hallways={stage.Hallways.Count}");
    }

    public bool TryGenerate(out GeneratedStage stage, out StageFailure failure)
    {
        stage = null;

        if (!Application.isPlaying)
        {
            failure = new StageFailure(StageFailureCode.BuildFailed, "Stage 생성은 Play Mode에서 실행해야 합니다.");

            return false;
        }

        if (CurrentStage != null)
        {
            failure = new StageFailure(StageFailureCode.AlreadyGenerated, "이미 생성된 Stage가 있습니다.");

            return false;
        }

        StageLayoutRequest layoutRequest = new StageLayoutRequest(_startRoom, _profile);

        if (!StageLayoutPlanner.TryCreatePlan(layoutRequest, out StagePlan plan, out failure))
        {
            return false;
        }

        ConnectorWallSnapshot startWallSnapshot = new ConnectorWallSnapshot(_startRoom);
        StageBuildContext buildContext = new StageBuildContext(plan, _startRoom, _generatedStageParent, _profile);

        if (!StageMaterializer.TryBuild(buildContext, out stage, out failure))
        {
            startWallSnapshot.Restore();
            return false;
        }

        CurrentStage = stage;
        _currentStartWallSnapshot = startWallSnapshot;
        failure = StageFailure.None;
        return true;
    }

    public bool TryGenerateConfigured(out GeneratedStage stage, out StageFailure failure)
    {
        return TryGenerate(out stage, out failure);
    }

    public bool TryRegenerateConfigured(out GeneratedStage stage, out StageFailure failure)
    {
        ClearCurrentStage();
        return TryGenerateConfigured(out stage, out failure);
    }

    private void ClearCurrentStage()
    {
        GeneratedStage previousStage = CurrentStage;

        CurrentStage = null;

        _currentStartWallSnapshot?.Restore();
        _currentStartWallSnapshot = null;

        if (previousStage?.Root == null)
        {
            return;
        }

        previousStage.Root.SetActive(false); // 비활성화 후 삭제하여, Stage 재생성 시 이전 Stage가 잠시 보이지 않도록 함.
        Destroy(previousStage.Root);
    }
}
