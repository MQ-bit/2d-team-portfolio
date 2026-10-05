
/// <summary>
/// StageFailureCode는 StageLayoutPlanner에서 발생할 수 있는 실패 원인을 나타내는 열거형.
/// </summary>
public enum StageFailureCode
{
    None,
    InvalidRequest,
    InvalidProfile,
    RetryLimitExceeded,
    InvalidPlan,
    MissingConnector,
    OverlapDetected,
    AlignmentFailed,
    BuildFailed,
    CarveFailed,
    AlreadyGenerated
}