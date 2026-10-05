
/// <summary>
/// StageLayoutPlanner에서 발생한 실패 정보를 나타내는 구조체. 각각 Code는 실패 원인을 나타내며, Message는 실패에 대한 상세 설명을 제공함.
/// </summary>
public readonly struct StageFailure
{
    public StageFailureCode Code { get; }
    public string Message { get; }
    public bool HasError => Code != StageFailureCode.None;

    public StageFailure(StageFailureCode code, string message)
    {
        Code = code;
        Message = message;
    }

    public static StageFailure None => new StageFailure(StageFailureCode.None, string.Empty);
}