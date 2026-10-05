using UnityEngine;

/// <summary>
/// PlannedRoom는 StageLayoutPlanner에서 계획된 방의 배치, 방향 및 유형과 같은 정보를 나타내는 클래스.
/// </summary>
public class PlannedRoom
{
    public LayoutPlacement<RoomDefinition> Placement { get; }
    public RoomDefinition Definition => Placement.Definition;
    public RoomType RoomType => Definition.Type;
    public Vector3 WorldPosition => Placement.WorldPosition;
    public Bounds WorldBounds => Placement.WorldBounds;
    public ConnectorDirection? EntryDirection { get; }
    public ConnectorDirection? ExitDirection { get; set; }
    public bool IsSceneStartRoom { get; }

    /// <summary>
    /// PlannedRoom 생성자. 방의 배치, 입구 방향 및 씬 시작 방 여부를 초기화함.
    /// </summary>
    /// <param name="placement"> 방의 배치 정보 </param>
    /// <param name="entryDirection"> 방의 입구 방향 </param>
    /// <param name="isSceneStartRoom"> 씬 시작 방 여부 </param>
    public PlannedRoom(LayoutPlacement<RoomDefinition> placement, ConnectorDirection? entryDirection, bool isSceneStartRoom)
    {
        Placement = placement;
        EntryDirection = entryDirection;
        IsSceneStartRoom = isSceneStartRoom;
    }
}