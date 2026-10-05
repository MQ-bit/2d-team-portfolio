using System.Collections.Generic;

/// <summary>
/// StagePlan는 StageLayoutPlanner에서 계획된 방과 복도의 배치, 연결 및 속성과 같은 정보를 나타내는 클래스.
/// </summary>
public class StagePlan
{
    private readonly List<PlannedRoom> _rooms = new List<PlannedRoom>();
    private readonly List<PlannedHallway> _hallways = new List<PlannedHallway>();

    public int Seed { get; }
    public int GenerationAttemptCount { get; set; }
    public IReadOnlyList<PlannedRoom> Rooms => _rooms;
    public IReadOnlyList<PlannedHallway> Hallways => _hallways;

    public StagePlan(int seed)
    {
        Seed = seed;
    }

    public void AddRoom(PlannedRoom room)
    {
        _rooms.Add(room);
    }

    public void AddHallway(PlannedHallway hallway)
    {
        _hallways.Add(hallway);
    }
}