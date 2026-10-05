using UnityEngine;

public static class StagePlanValidator
{
    public static bool TryValidate(StageLayoutRequest request, StagePlan plan, out StageFailure failure)
    {
        if (!ValidateRoomSequence(plan, out failure))
        {
            return false;
        }

        if (!ValidateConnections(request, plan, out failure))
        {
            return false;
        }

        return StageCollisionValidator.TryValidatePlan(request, plan, out failure);
    }

    private static bool ValidateRoomSequence(StagePlan plan, out StageFailure failure)
    {
        if (plan.Rooms.Count < 4 || plan.Rooms.Count > 5)
        {
            failure = new StageFailure(StageFailureCode.InvalidPlan, $"전체 Room 수가 올바르지 않습니다: {plan.Rooms.Count}");

            return false;
        }

        if (plan.Hallways.Count != plan.Rooms.Count - 1)
        {
            failure = new StageFailure(StageFailureCode.InvalidPlan, "Hallway 수가 Room 수 - 1이 아닙니다.");

            return false;
        }

        if (plan.Rooms[0].RoomType != RoomType.Start || plan.Rooms[1].RoomType != RoomType.Normal ||
            plan.Rooms[plan.Rooms.Count - 1].RoomType != RoomType.End)
        {
            failure = new StageFailure( StageFailureCode.InvalidPlan, "Start, 필수 Normal, End 방들의 순서가 올바르지 않습니다.");
 
            return false;
        }

        for (int i = 2; i < plan.Rooms.Count - 1; i++)
        {
            RoomType type = plan.Rooms[i].RoomType;

            if (type != RoomType.Normal && type != RoomType.Elite)
            {
                failure = new StageFailure(StageFailureCode.InvalidPlan, $"전투방 {i}의 RoomType이 올바르지 않습니다.");

                return false;
            }
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool ValidateConnections(StageLayoutRequest request, StagePlan plan, out StageFailure failure)
    {
        for (int i = 0; i < plan.Hallways.Count; i++)
        {
            PlannedHallway hallway = plan.Hallways[i];
            StageConnection connection = hallway.Connection;

            if (connection.FromRoomIndex != i || connection.ToRoomIndex != i + 1)
            {
                failure = new StageFailure(StageFailureCode.InvalidPlan, $"Hallway {i}의 Room 인덱스가 올바르지 않습니다.");

                return false;
            }

            if (!ValidateAnchors(request, plan, hallway, out failure))
            {
                return false;
            }
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool ValidateAnchors(StageLayoutRequest request, StagePlan plan, PlannedHallway hallway, out StageFailure failure)
    {
        StageConnection connection = hallway.Connection;
        PlannedRoom fromRoom = plan.Rooms[connection.FromRoomIndex];
        PlannedRoom toRoom = plan.Rooms[connection.ToRoomIndex];

        if (!fromRoom.Definition.TryGetConnector(connection.FromDirection, out RoomConnector fromConnector) ||
            !toRoom.Definition.TryGetConnector(connection.ToDirection, out RoomConnector toConnector) ||
            !hallway.Definition.TryGetConnector(connection.FromDirection.OppositeDirection(), out RoomConnector hallwayEntry) ||
            !hallway.Definition.TryGetConnector(connection.FromDirection, out RoomConnector hallwayExit))
        {
            failure = new StageFailure(StageFailureCode.MissingConnector, "계획 검증 중 Connector를 찾지 못했습니다.");

            return false;
        }

        Vector3 fromPosition = StageLayoutGeometry.GetConnectorWorldPosition(fromRoom, fromConnector);

        Vector3 toPosition = StageLayoutGeometry.GetConnectorWorldPosition(toRoom, toConnector);

        Vector3 hallwayEntryLocalPosition = StageLayoutGeometry.GetConnectorLocalPosition(hallway.Definition, hallwayEntry);
        Vector3 hallwayEntryPosition = hallway.WorldPosition + hallwayEntryLocalPosition;

        Vector3 hallwayExitLocalPosition = StageLayoutGeometry.GetConnectorLocalPosition(hallway.Definition, hallwayExit);
        Vector3 hallwayExitPosition = hallway.WorldPosition + hallwayExitLocalPosition;

        float epsilon = request.Profile.ConnectorAlignmentEpsilon;

        if (Vector3.Distance(fromPosition, hallwayEntryPosition) > epsilon ||
            Vector3.Distance(toPosition, hallwayExitPosition) > epsilon)
        {
            failure = new StageFailure(StageFailureCode.AlignmentFailed, "계획의 Connector Anchor가 정렬되지 않았습니다.");

            return false;
        }

        failure = StageFailure.None;
        return true;
    }
}