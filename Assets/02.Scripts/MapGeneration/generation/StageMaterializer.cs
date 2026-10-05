using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StageBuildRequest를 기반으로 실제 Stage를 생성하는 클래스.
/// </summary>
public class StageMaterializer
{
    public bool TryBuild(StageBuildRequest request, out GeneratedStage stage, out StageFailure failure)
    {
        stage = null;

        if (!ValidateRequest(request, out failure))
        {
            return false;
        }

        GameObject root = new GameObject($"Stage_{request.Plan.Seed}_{request.Plan.GenerationAttemptCount}");

        root.transform.SetParent(request.RootParent, false);
        root.SetActive(false);

        StageBuildContext context = new StageBuildContext(request, root);
        try
        {
            CreateHierarchy(context);

            if (!TryInstantiateRooms(context, out failure) ||
                !TryInstantiateHallways(context, out failure) ||
                !TryValidateConnections(context, out failure) ||
                !TryCarveDynamicRooms(context, out failure) ||
                !TryCarveStartRoom(context, out failure))
            {
                Cleanup(context);
                return false;
            }

            root.SetActive(true);
            Physics2D.SyncTransforms();

            stage = new GeneratedStage(root, context.Rooms.AsReadOnly(), context.Hallways.AsReadOnly(), CalculateWorldBounds(request.Plan));

            failure = StageFailure.None;
            return true;
        }
        catch (Exception exception)
        {
            context.StartWallSnapshot.Restore();
            Cleanup(context);

            failure = new StageFailure(StageFailureCode.BuildFailed, $"Stage 생성 중 예외가 발생했습니다: {exception.Message}");

            return false;
        }
    }

    private static void CreateHierarchy(StageBuildContext context)
    {
        context.RoomsRoot = CreateChild(context.Root.transform, "Rooms");
        context.HallwaysRoot = CreateChild(context.Root.transform, "Hallways");
        CreateChild(context.Root.transform, "Enemies");
        CreateChild(context.Root.transform, "Portal");
    }

    private static bool TryInstantiateRooms(StageBuildContext context, out StageFailure failure)
    {
        StagePlan plan = context.Request.Plan;
        context.Rooms.Add(context.Request.StartRoom);

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
        foreach (PlannedHallway planned in context.Request.Plan.Hallways)
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
        for (int i = 0; i < context.Request.Plan.Hallways.Count; i++)
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
        PlannedHallway planned = context.Request.Plan.Hallways[hallwayIndex];

        StageConnection connection = planned.Connection;
        RoomDefinition fromRoom = context.Rooms[connection.FromRoomIndex];

        RoomDefinition toRoom = context.Rooms[connection.ToRoomIndex];

        HallwayDefinition hallway = context.Hallways[hallwayIndex];

        if (!fromRoom.TryGetConnector(connection.FromDirection, out RoomConnector fromConnector) ||
            !toRoom.TryGetConnector(connection.ToDirection, out RoomConnector toConnector) ||
            !hallway.TryGetConnector(connection.FromDirection.OppositeDirection(), out RoomConnector hallwayEntry) ||
            !hallway.TryGetConnector(connection.FromDirection, out RoomConnector hallwayExit))
        {
            failure = new StageFailure(StageFailureCode.MissingConnector, $"런타임 연결 {hallwayIndex}의 Connector가 없습니다.");
            return false;
        }

        float epsilon = context.Request.Profile.ConnectorAlignmentEpsilon;

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
            if (!TryCarveRoom(context.Rooms[i], context.Request.Plan.Rooms[i], out failure))
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
        PlannedRoom plannedStart = context.Request.Plan.Rooms[0];

        if (!plannedStart.ExitDirection.HasValue)
        {
            failure = new StageFailure(StageFailureCode.InvalidPlan, "StartRoom의 출구 방향이 없습니다.");
            return false;
        }

        ConnectorDirection direction = plannedStart.ExitDirection.Value;

        if (!context.Request.StartRoom.TryCarveConnector(direction, out int removedTileCount) || removedTileCount <= 0)
        {
            context.StartWallSnapshot.Restore();

            failure = new StageFailure(StageFailureCode.CarveFailed, $"StartRoom의 {direction} 벽 개방에 실패했습니다.");

            return false;
        }

        context.Request.StartRoom.RefreshWallCollider();
        failure = StageFailure.None;
        return true;
    }

    private static bool ValidateRequest(StageBuildRequest request, out StageFailure failure)
    {
        if (request == null || request.Plan == null || request.StartRoom == null || request.RootParent == null || request.Profile == null)
        {
            failure = new StageFailure(StageFailureCode.InvalidRequest, "StageBuildRequest 구성이 유효하지 않습니다.");

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