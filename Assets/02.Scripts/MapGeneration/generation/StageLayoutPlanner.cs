using UnityEngine;
using System.Collections.Generic;
using Random = System.Random;

/// <summary>
/// StageLayoutRequest를 기반으로 StagePlan을 생성하는 정적 클래스. StageLayoutPlanner는 StageLayoutRequest의 유효성을 검증하고, StagePlan을 생성하며, 방과 복도의 배치를 계획함. 
/// </summary>
internal static class StageLayoutPlanner
{
    private const int Standard_Additional_RoomCount = 1;
    private const int Extended_Additional_RoomCount = 2;

    public static bool TryCreatePlan(StageLayoutRequest request, out StagePlan plan, out StageFailure failure)
    {
        plan = null;

        if (!ValidateRequest(request, out failure))
        {
            return false;
        }

        Random random = new Random(request.Seed);

        for (int attempt = 1; attempt <= request.Profile.MaxLayoutRetryCount; attempt++)
        {
            StagePlan candidate = CreateInitialPlan(request, attempt);

            if (!TryPopulatePlan(request, candidate, random))
            {
                continue;
            }

            plan = candidate;
            failure = StageFailure.None;
            return true;
        }

        failure = new StageFailure(StageFailureCode.RetryLimitExceeded, $"{request.Profile.MaxLayoutRetryCount}회의 배치 시도가 실패했습니다.");

        return false;
    }

    private static StagePlan CreateInitialPlan(StageLayoutRequest request, int attempt)
    {
        RoomDefinition startRoom = request.StartRoom;

        StagePlan plan = new StagePlan(request.Seed)
        {
            GenerationAttemptCount = attempt
        };

        plan.AddRoom(new PlannedRoom(startRoom, startRoom.transform.position, startRoom.WorldBounds, null));

        return plan;
    }

    private static bool TryPopulatePlan(StageLayoutRequest request, StagePlan plan, Random random)
    {
        // 기존 구현과 동일한 난수 호출 순서를 유지한다.
        int additionalRoomCount = (random.NextDouble() < request.Profile.AdditionalRoomChance) ? Extended_Additional_RoomCount : Standard_Additional_RoomCount;

        RoomType firstAdditionalRoom = SelectCombatRoomType(request.Profile, random);

        RoomType secondAdditionalRoom = default;

        if (additionalRoomCount == Extended_Additional_RoomCount)
        {
            secondAdditionalRoom = SelectCombatRoomType(request.Profile, random);
        }

        if (!TryAppendRoom(request, plan, RoomType.Normal, random))
        {
            return false;
        }

        if (!TryAppendRoom(request, plan, firstAdditionalRoom, random))
        {
            return false;
        }

        if (additionalRoomCount == Extended_Additional_RoomCount &&
            !TryAppendRoom(request, plan, secondAdditionalRoom, random))
        {
            return false;
        }

        return TryAppendRoom(request, plan, RoomType.End, random);
    }

    private static RoomType SelectCombatRoomType(StageGenerationProfileSO profile, Random random)
    {
        return (random.NextDouble() < profile.EliteRoomProbability) ? RoomType.Elite : RoomType.Normal;
    }

    private static bool TryAppendRoom(StageLayoutRequest request, StagePlan plan, RoomType nextRoomType, Random random)
    {
        int currentRoomIndex = plan.Rooms.Count - 1;
        PlannedRoom currentRoom = plan.Rooms[currentRoomIndex];

        List<ConnectorDirection> exitDirections = new List<ConnectorDirection>();

        CollectExitDirections(currentRoom, exitDirections);
        Shuffle(exitDirections, random);

        List<RoomDefinition> roomCandidates = new List<RoomDefinition>();
        request.Profile.CollectRoomPrefabs(nextRoomType, roomCandidates);

        Shuffle(roomCandidates, random);

        foreach (ConnectorDirection exitDirection in exitDirections)
        {
            HallwayDefinition hallway = request.Profile.GetHallway(exitDirection);

            foreach (RoomDefinition room in roomCandidates)
            {
                ConnectionCandidate? candidate = CreateCandidate(currentRoom, hallway, room, exitDirection);

                if (!candidate.HasValue)
                {
                    continue;
                }

                if (!StageCollisionValidator.CanPlace(request, plan, currentRoomIndex, candidate.Value))
                {
                    continue;
                }

                CommitCandidate(plan, candidate.Value);
                return true;
            }
        }

        return false;
    }

    private static ConnectionCandidate? CreateCandidate(PlannedRoom currentRoom, HallwayDefinition hallway, RoomDefinition nextRoom, ConnectorDirection exitDirection)
    {
        if (currentRoom == null || hallway == null || nextRoom == null)
        {
            return null;
        }

        ConnectorDirection entryDirection = exitDirection.OppositeDirection();

        if (!currentRoom.Definition.TryGetConnector(exitDirection, out RoomConnector currentExit) ||
            !hallway.TryGetConnector(entryDirection, out RoomConnector hallwayEntry) ||
            !hallway.TryGetConnector(exitDirection, out RoomConnector hallwayExit) ||
            !nextRoom.TryGetConnector(entryDirection, out RoomConnector roomEntry))
        {
            return null;
        }

        if (nextRoom.Type != RoomType.End && !HasExitOtherThan(nextRoom, entryDirection))
        {
            return null;
        }

        Vector3 currentExitPosition = GetConnectorWorldPosition(currentRoom, currentExit);

        Vector3 hallwayEntryOffset = GetConnectorLocalPosition(hallway, hallwayEntry);
        Vector3 hallwayPosition = currentExitPosition - hallwayEntryOffset;
        Bounds hallwayBounds = TranslateBounds(hallway.LocalBounds, hallwayPosition);

        Vector3 hallwayExitPosition = hallwayPosition + GetConnectorLocalPosition(hallway, hallwayExit);

        Vector3 roomEntryOffset = GetConnectorLocalPosition(nextRoom, roomEntry);

        Vector3 roomPosition = hallwayExitPosition - roomEntryOffset;

        Bounds roomBounds = TranslateBounds(nextRoom.LocalBounds, roomPosition);

        PlannedHallway plannedHallway = new PlannedHallway(hallway, hallwayPosition, hallwayBounds, exitDirection);

        PlannedRoom plannedRoom = new PlannedRoom(nextRoom, roomPosition, roomBounds, entryDirection);

        return new ConnectionCandidate(plannedHallway, plannedRoom);
    }

    private static void CommitCandidate(StagePlan plan, ConnectionCandidate candidate)
    {
        PlannedRoom currentRoom = plan.Rooms[plan.Rooms.Count - 1];

        currentRoom.ExitDirection = candidate.ExitDirection;

        plan.AddHallway(candidate.Hallway);
        plan.AddRoom(candidate.Room);
    }

    private static void CollectExitDirections(PlannedRoom room, List<ConnectorDirection> destination)
    {
        destination.Clear();

        foreach (RoomConnector connector in room.Definition.Connectors)
        {
            if (connector == null)
            {
                continue;
            }

            if (room.EntryDirection.HasValue && connector.Direction == room.EntryDirection.Value)
            {
                continue;
            }

            if (!destination.Contains(connector.Direction))
            {
                destination.Add(connector.Direction);
            }
        }
    }

    private static bool HasExitOtherThan(RoomDefinition room, ConnectorDirection entryDirection)
    {
        foreach (RoomConnector connector in room.Connectors)
        {
            if (connector != null && connector.Direction != entryDirection)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector3 GetConnectorLocalPosition(Component owner, RoomConnector connector) //TODO: owner는 RoomDefinition 또는 HallwayDefinition이어야 함. Component 대신 이를 위한 무언가 추가하기.
    {
        return owner.transform.InverseTransformPoint(connector.WorldPosition);
    }

    private static Vector3 GetConnectorWorldPosition(PlannedRoom room, RoomConnector connector)
    {
        Vector3 offset = GetConnectorLocalPosition(room.Definition, connector);

        return room.WorldPosition + offset;
    }

    private static Bounds TranslateBounds(Bounds localBounds, Vector3 worldPosition)
    {
        return new Bounds(worldPosition + localBounds.center, localBounds.size);
    }

    private static bool ValidateRequest(StageLayoutRequest request, out StageFailure failure)
    {
        if (request == null)
        {
            failure = new StageFailure(StageFailureCode.InvalidRequest, "StageLayoutRequest가 null입니다.");

            return false;
        }

        if (request.StartRoom == null || request.StartRoom.Type != RoomType.Start)
        {
            failure = new StageFailure(StageFailureCode.InvalidRequest, "StartRoom 참조가 유효하지 않습니다.");

            return false;
        }

        if (request.Profile == null)
        {
            failure = new StageFailure(StageFailureCode.InvalidProfile, "StageGenerationProfile이 없습니다.");

            return false;
        }

        if (!request.Profile.TryValidate(out string profileError))
        {
            failure = new StageFailure(StageFailureCode.InvalidProfile, profileError);

            return false;
        }

        failure = StageFailure.None;
        return true;
    }

    private static void Shuffle<T>(IList<T> values, Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int targetIndex = random.Next(i + 1);

            (values[i], values[targetIndex]) = (values[targetIndex], values[i]);
        }
    }
}