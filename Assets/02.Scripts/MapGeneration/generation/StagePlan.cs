using System.Collections.Generic;

/// <summary>
/// Stage를 생성하기 위한 계획을 나타내는 클래스.
/// Room과 Hallway의 배치, 연결, 그리고 기타 필요한 정보를 포함하고 있으며, StageMaterializer가 실제 Stage를 생성할 때 사용됨.
/// </summary>
internal class StagePlan
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