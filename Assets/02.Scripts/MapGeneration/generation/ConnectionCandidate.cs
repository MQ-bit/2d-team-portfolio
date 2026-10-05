/// <summary>
/// StageLayoutPlanner에서 계획된 연결 후보의 정보를 나타내는 클래스.
/// </summary>
public class ConnectionCandidate
{
    public LayoutPlacement<HallwayDefinition> Hallway { get; }
    public LayoutPlacement<RoomDefinition> Room { get; }
    public ConnectorDirection ExitDirection { get; }
    public ConnectorDirection EntryDirection => ExitDirection.OppositeDirection();

    /// <summary>
    /// ConnectionCandidate 생성자. 복도와 방의 배치 및 연결 방향 정보를 초기화함.
    /// </summary>
    /// <param name="hallway"> 복도의 배치 정보 </param>
    /// <param name="room"> 방의 배치 정보 </param>
    /// <param name="exitDirection"> 연결 방향 </param>
    public ConnectionCandidate(LayoutPlacement<HallwayDefinition> hallway, LayoutPlacement<RoomDefinition> room, ConnectorDirection exitDirection)
    {
        Hallway = hallway;
        Room = room;
        ExitDirection = exitDirection;
    }
}