using System;
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

    [Header("Seed")]
    [SerializeField] private bool _useRandomSeed = true;
    [SerializeField] private int _seed = 12345;

    [Header("Runtime")]
    [SerializeField] private bool _generateOnStart = true;

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

        Debug.Log($"Stage 생성 성공: Seed={stage.Seed}, Rooms={stage.Rooms.Count}, Hallways={stage.Hallways.Count}");
    }

    public bool TryGenerate(int seed, out GeneratedStage stage, out StageFailure failure)
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

        StageLayoutRequest layoutRequest = new StageLayoutRequest(_startRoom, _profile, seed);

        if (!StageLayoutPlanner.TryCreatePlan(layoutRequest, out StagePlan plan, out failure))
        {
            return false;
        }

        StageBuildContext buildContext = new StageBuildContext(plan, _startRoom, _generatedStageParent, _profile);

        if (!StageMaterializer.TryBuild(buildContext, out stage, out failure))
        {
            return false;
        }

        CurrentStage = stage;
        failure = StageFailure.None;
        return true;
    }

    public bool TryGenerateConfigured(out GeneratedStage stage, out StageFailure failure)
    {
        int selectedSeed = _useRandomSeed ? Environment.TickCount : _seed;

        return TryGenerate(selectedSeed, out stage, out failure);
    }
}