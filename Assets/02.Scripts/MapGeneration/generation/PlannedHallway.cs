using UnityEngine;

/// <summary>
/// StageLayoutPlanner에서 계획된 복도의 배치, 연결 및 속성과 같은 정보를 나타내는 클래스.
/// </summary>
internal sealed class PlannedHallway
{
    public HallwayDefinition Definition { get; }
    public Vector3 WorldPosition { get; }
    public Bounds WorldBounds { get; }
    public ConnectorDirection FromDirection { get; }
    public ConnectorDirection ToDirection => FromDirection.OppositeDirection();

    public PlannedHallway(HallwayDefinition definition, Vector3 worldPosition, Bounds worldBounds, ConnectorDirection fromDirection)
    {
        Definition = definition;
        WorldPosition = worldPosition;
        WorldBounds = worldBounds;
        FromDirection = fromDirection;
    }
}