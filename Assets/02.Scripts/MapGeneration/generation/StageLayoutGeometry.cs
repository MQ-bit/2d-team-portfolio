using UnityEngine;

/// <summary>
/// StageLayoutGeometry는 StageLayoutPlanner에서 계획된 방과 복도의 배치 및 연결을 처리하는 정적 유틸리티 클래스입니다.
/// </summary>
public static class StageLayoutGeometry
{
    public static Vector3 GetConnectorLocalPosition(Component owner, RoomConnector connector)
    {
        return owner.transform.InverseTransformPoint(connector.WorldPosition);
    }

    public static Vector3 GetConnectorWorldPosition(PlannedRoom room, RoomConnector connector)
    {
        Vector3 offset = GetConnectorLocalPosition(room.Definition, connector);
        return room.WorldPosition + offset;
    }

    public static Bounds TranslateBounds(Bounds localBounds, Vector3 worldPosition)
    {
        return new Bounds(worldPosition + localBounds.center, localBounds.size);
    }
}