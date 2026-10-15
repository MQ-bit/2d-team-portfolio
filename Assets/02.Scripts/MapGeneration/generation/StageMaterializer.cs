using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StagePlan을 기반으로 Stage를 생성하는 과정에서 Stage를 구성하는 방과 복도를 실제로 생성하고 배치하는 정적 클래스.
/// StageBuildContext를 사용하여 Stage를 생성하며, 생성된 Stage의 루트 GameObject, 방 및 복도의 정의, 그리고 Stage의 월드 경계와 같은 정보를 반환함.
/// </summary>
internal static class StageMaterializer
{
    public static bool TryBuild(StageBuildContext context, out GeneratedStage stage, out StageFailure failure)
    {
        stage = null;

        if (!ValidateContext(context, out failure))
        {
            return false;
        }

        try
        {
            context.BeginBuild();
            CreateHierarchy(context);

            if (!TryInstantiateRooms(context, out failure) ||
                !TryInstantiateHallways(context, out failure) ||
                !TryValidateConnections(context, out failure) ||
                !TryCarveDynamicRooms(context, out failure) ||
                !TryCarveStartRoom(context, out failure) ||
                !TryCreateRoomNodes(context, out failure) ||
                !TryInstantiatePortal(context, out failure))
            {
                Cleanup(context);
                return false;
            }

            context.Root.SetActive(true);
            Physics2D.SyncTransforms();

            stage = new GeneratedStage(
                        context.Root,
                        context.Rooms.AsReadOnly(),
                        context.RoomNodes.AsReadOnly(),
                        context.Hallways.AsReadOnly(),
                        context.EnemiesRoot,
                        context.TransientsRoot,
                        context.PortalRoot,
                        CalculateWorldBounds(context.Plan));

            failure = StageFailure.None;
            return true;
        }
        catch (Exception exception)
        {
            context.StartWallSnapshot?.Restore();
            Cleanup(context);

            failure = new StageFailure(StageFailureCode.BuildFailed, $"Stage 생성 중 예외가 발생했습니다: {exception.Message}");

            return false;
        }
    }

    private static void CreateHierarchy(StageBuildContext context)
    {
        context.RoomsRoot = CreateChild(context.Root.transform, "Rooms");
        context.HallwaysRoot = CreateChild(context.Root.transform, "Hallways");
        context.EnemiesRoot = CreateChild(context.Root.transform, "Enemies");
        context.TransientsRoot = CreateChild(context.Root.transform, "Transients");
        context.PortalRoot = CreateChild(context.Root.transform, "Portal");
    }

    private static bool TryInstantiateRooms(StageBuildContext context, out StageFailure failure)
    {
        StagePlan plan = context.Plan;
        context.Rooms.Add(context.StartRoom);

        for (int i = 1; i < plan.Rooms.Count; i++)
        {
            PlannedRoom plannedRoom = plan.Rooms[i];

            RoomDefinition room = GameObject.Instantiate(plannedRoom.Definition, plannedRoom.WorldPosition, Quaternion.identity, context.RoomsRoot);

            if (room == null)
            {
                failure = new StageFailure(StageFailureCode.BuildFailed, $"Room {i} 생성에 실패했습니다.");
                return false;
            }

            room.name = plannedRoom.Definition.name;
            context.Rooms.Add(room);
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool TryInstantiateHallways(StageBuildContext context, out StageFailure failure)
    {
        foreach (PlannedHallway planned in context.Plan.Hallways)
        {
            HallwayDefinition hallway = GameObject.Instantiate(planned.Definition, planned.WorldPosition, Quaternion.identity, context.HallwaysRoot);

            if (hallway == null)
            {
                failure = new StageFailure(StageFailureCode.BuildFailed, "Hallway 생성에 실패했습니다.");

                return false;
            }

            hallway.name = planned.Definition.name;
            context.Hallways.Add(hallway);
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool TryValidateConnections(StageBuildContext context, out StageFailure failure)
    {
        for (int i = 0; i < context.Plan.Hallways.Count; i++)
        {
            if (!TryValidateConnection(context, i, out failure))
            {
                return false;
            }
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool TryValidateConnection(StageBuildContext context, int hallwayIndex, out StageFailure failure)
    {
        PlannedHallway planned = context.Plan.Hallways[hallwayIndex];

        RoomDefinition fromRoom = context.Rooms[hallwayIndex];
        RoomDefinition toRoom = context.Rooms[hallwayIndex + 1];
        HallwayDefinition hallway = context.Hallways[hallwayIndex];

        ConnectorDirection fromDirection = planned.FromDirection;
        ConnectorDirection toDirection = planned.ToDirection;

        if (!fromRoom.TryGetConnector(fromDirection, out RoomConnector fromConnector) ||
            !toRoom.TryGetConnector(toDirection, out RoomConnector toConnector) ||
            !hallway.TryGetConnector(fromDirection.OppositeDirection(), out RoomConnector hallwayEntry) ||
            !hallway.TryGetConnector(fromDirection, out RoomConnector hallwayExit))
        {
            failure = new StageFailure(StageFailureCode.MissingConnector, $"런타임 연결 {hallwayIndex}의 Connector가 없습니다.");
            return false;
        }

        float epsilon = context.Profile.ConnectorAlignmentEpsilon;

        if (Vector3.Distance(fromConnector.WorldPosition, hallwayEntry.WorldPosition) > epsilon ||
            Vector3.Distance(hallwayExit.WorldPosition, toConnector.WorldPosition) > epsilon)
        {
            failure = new StageFailure(StageFailureCode.AlignmentFailed, $"런타임 연결 {hallwayIndex}의 Anchor가 정렬되지 않았습니다.");
            return false;
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool TryCarveDynamicRooms(StageBuildContext context, out StageFailure failure)
    {
        for (int i = 1; i < context.Rooms.Count; i++)
        {
            if (!TryCarveRoom(context.Rooms[i], context.Plan.Rooms[i], out failure))
            {
                return false;
            }

            context.Rooms[i].RefreshWallCollider();
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool TryCarveRoom(RoomDefinition room, PlannedRoom plannedRoom, out StageFailure failure)
    {
        HashSet<ConnectorDirection> directions = new HashSet<ConnectorDirection>();

        if (plannedRoom.EntryDirection.HasValue)
        {
            directions.Add(plannedRoom.EntryDirection.Value);
        }

        if (plannedRoom.ExitDirection.HasValue)
        {
            directions.Add(plannedRoom.ExitDirection.Value);
        }

        foreach (ConnectorDirection direction in directions)
        {
            if (!room.TryCarveConnector(direction, out int removedTileCount) || removedTileCount <= 0)
            {
                failure = new StageFailure(StageFailureCode.CarveFailed, $"{room.name}의 {direction} 벽 개방에 실패했습니다.");
                return false;
            }
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool TryCarveStartRoom(StageBuildContext context, out StageFailure failure)
    {
        PlannedRoom plannedStart = context.Plan.Rooms[0];

        if (!plannedStart.ExitDirection.HasValue)
        {
            failure = new StageFailure(StageFailureCode.InvalidPlan, "StartRoom의 출구 방향이 없습니다.");
            return false;
        }

        ConnectorDirection direction = plannedStart.ExitDirection.Value;

        if (!context.StartRoom.TryCarveConnector(direction, out int removedTileCount) || removedTileCount <= 0)
        {
            context.StartWallSnapshot.Restore();

            failure = new StageFailure(StageFailureCode.CarveFailed, $"StartRoom의 {direction} 벽 개방에 실패했습니다.");

            return false;
        }

        context.StartRoom.RefreshWallCollider();
        failure = StageFailure.None;
        return true;
    }

    private static bool TryCreateRoomNodes(StageBuildContext context, out StageFailure failure)
    {
        context.RoomNodes.Clear();

        for (int i = 0; i < context.Rooms.Count; i++)
        {
            PlannedRoom planned = context.Plan.Rooms[i];
            RoomDefinition room = context.Rooms[i];

            RoomConnector entry = null;
            RoomConnector exit = null;

            if (planned.EntryDirection.HasValue && !room.TryGetConnector(planned.EntryDirection.Value, out entry))
            {
                failure = new StageFailure(StageFailureCode.MissingConnector, $"{room.name}: 생성된 Entry Connector를 찾을 수 없습니다.");
                return false;
            }

            if (planned.ExitDirection.HasValue && !room.TryGetConnector(planned.ExitDirection.Value, out exit))
            {
                failure = new StageFailure(StageFailureCode.MissingConnector, $"{room.name}: 생성된 Exit Connector를 찾을 수 없습니다.");
                return false;
            }

            bool valid = room.Type switch
            {
                RoomType.Start => entry == null && exit != null,
                RoomType.Normal => entry != null && exit != null,
                RoomType.Elite => entry != null && exit != null,
                RoomType.End => entry != null && exit == null,
                _ => false
            };

            if (!valid)
            {
                failure = new StageFailure(StageFailureCode.InvalidPlan, $"{room.name}: {room.Type} 방의 Entry/Exit 구성이 유효하지 않습니다.");
                return false;
            }

            context.RoomNodes.Add(new GeneratedRoomNode(room, entry, exit, i));
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool TryInstantiatePortal(StageBuildContext context, out StageFailure failure)
    {
        GeneratedRoomNode endRoomNode = null;

        foreach (GeneratedRoomNode node in context.RoomNodes)
        {
            if (node?.Room == null || node.Room.Type != RoomType.End)
            {
                continue;
            }

            if (endRoomNode != null)
            {
                failure = new StageFailure(StageFailureCode.InvalidPlan, "생성된 Stage에 EndRoom이 두 개 이상 존재합니다.");
                return false;
            }

            endRoomNode = node;
        }

        if (endRoomNode?.Room == null)
        {
            failure = new StageFailure(StageFailureCode.InvalidPlan, "생성된 Stage에서 EndRoom을 찾을 수 없습니다.");
            return false;
        }

        Transform spawnPoint = endRoomNode.Room.PortalSpawnPoint;
        if (spawnPoint == null)
        {
            failure = new StageFailure(StageFailureCode.InvalidPlan, $"{endRoomNode.Room.name}: PortalSpawnPoint가 없습니다.");
            return false;
        }

        InteractablePortal portalPrefab = context.Profile.PortalPrefab;
        if (portalPrefab == null)
        {
            failure = new StageFailure(StageFailureCode.InvalidProfile, "Portal 프리팹이 설정되지 않았습니다.");
            return false;
        }

        InteractablePortal portal = GameObject.Instantiate(portalPrefab, spawnPoint.position, spawnPoint.rotation, context.PortalRoot);

        if (portal == null)
        {
            failure = new StageFailure(StageFailureCode.BuildFailed, "Portal 생성에 실패했습니다.");
            return false;
        }

        portal.name = portalPrefab.name;
        failure = StageFailure.None;
        return true;
    }

    private static bool ValidateContext(StageBuildContext context, out StageFailure failure)
    {
        if (context == null ||
            context.Plan == null ||
            context.StartRoom == null ||
            context.RootParent == null ||
            context.Profile == null)
        {
            failure = new StageFailure(StageFailureCode.InvalidRequest, "StageBuildContext 구성이 유효하지 않습니다.");

            return false;
        }

        failure = StageFailure.None;
        return true;
    }

    private static Bounds CalculateWorldBounds(StagePlan plan)
    {
        Bounds result = plan.Rooms[0].WorldBounds;

        for (int i = 1; i < plan.Rooms.Count; i++)
        {
            result.Encapsulate(plan.Rooms[i].WorldBounds);
        }

        foreach (PlannedHallway hallway in plan.Hallways)
        {
            result.Encapsulate(hallway.WorldBounds);
        }

        return result;
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void Cleanup(StageBuildContext context)
    {
        if (context?.Root == null)
        {
            return;
        }

        context.Root.SetActive(false);
        UnityEngine.Object.Destroy(context.Root);
    }
}
