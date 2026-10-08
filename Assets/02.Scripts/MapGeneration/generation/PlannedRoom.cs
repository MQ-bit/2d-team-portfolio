using UnityEngine;

/// <summary>
/// StageLayoutPlanner에서 계획된 방의 배치, 연결 및 속성과 같은 정보를 나타내는 클래스.
/// </summary>
internal class PlannedRoom
{
    public RoomDefinition Definition { get; }
    public Vector3 WorldPosition { get; }
    public Bounds WorldBounds { get; }
    public RoomType RoomType => Definition.Type;
    public ConnectorDirection? EntryDirection { get; }
    public ConnectorDirection? ExitDirection { get; set; }

    public PlannedRoom(RoomDefinition definition, Vector3 worldPosition, Bounds worldBounds, ConnectorDirection? entryDirection)
    {
        Definition = definition;
        WorldPosition = worldPosition;
        WorldBounds = worldBounds;
        EntryDirection = entryDirection;
    }
}