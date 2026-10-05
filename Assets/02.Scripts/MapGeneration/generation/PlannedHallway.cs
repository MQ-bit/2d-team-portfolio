using UnityEngine;

/// <summary>
/// PlannedHallway는 StageLayoutPlanner에서 계획된 복도의 배치, 방향 및 연결과 같은 정보를 나타내는 클래스.
/// </summary>
public class PlannedHallway
{
    public LayoutPlacement<HallwayDefinition> Placement { get; }
    public HallwayDefinition Definition => Placement.Definition;
    public Vector3 WorldPosition => Placement.WorldPosition;
    public Bounds WorldBounds => Placement.WorldBounds;
    public HallwayOrientation Orientation => Definition.Orientation;
    public StageConnection Connection { get; }

    /// <summary>
    /// PlannedHallway 생성자. 복도의 배치 및 연결 정보를 초기화함.
    /// </summary>
    /// <param name="placement"> 복도의 배치 정보 </param>
    /// <param name="connection"> 복도의 연결 정보 </param>
    public PlannedHallway(LayoutPlacement<HallwayDefinition> placement, StageConnection connection)
    {
        Placement = placement;
        Connection = connection;
    }
}