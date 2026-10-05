/// <summary>
/// StageLayoutPlanner에서 계획된 복도와 방의 연결 후보를 나타내는 구조체. Hallway는 연결될 복도의 정보를, Room은 연결될 방의 정보를 담고 있음.
/// </summary>
internal readonly struct ConnectionCandidate
{
    public PlannedHallway Hallway { get; }
    public PlannedRoom Room { get; }

    public ConnectorDirection ExitDirection => Hallway.FromDirection;

    public ConnectionCandidate(PlannedHallway hallway, PlannedRoom room)
    {
        Hallway = hallway;
        Room = room;
    }
}