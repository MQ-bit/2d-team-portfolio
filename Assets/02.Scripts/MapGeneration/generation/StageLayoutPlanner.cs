using System;
using System.Collections.Generic;

public class StageLayoutPlanner
{
    public bool TryCreatePlan(StageLayoutRequest request, out StagePlan plan, out StageFailure failure)
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
            List<RoomType> sequence = CreateRoomSequence(request.Profile, random);

            if (!TryBuildSequence(request, candidate, sequence, random))
            {
                continue;
            }

            if (!StagePlanValidator.TryValidate(request, candidate, out failure))
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

        LayoutPlacement<RoomDefinition> placement = new(startRoom, startRoom.transform.position, startRoom.WorldBounds);

        StagePlan plan = new StagePlan(request.Seed)
        {
            GenerationAttemptCount = attempt
        };

        plan.AddRoom(new PlannedRoom(placement, null, true));
        return plan;
    }

    private static List<RoomType> CreateRoomSequence(StageGenerationProfileSO profile, Random random)
    {
        List<RoomType> sequence = new() { RoomType.Normal };

        int additionalRoomCount = (random.NextDouble() < profile.AdditionalRoomChance) ? 2 : 1; // TODO: 1 또는 2개의 추가 전투방을 배치. 이는 매직 넘버이므로 수정 요망

        for (int i = 0; i < additionalRoomCount; i++)
        {
            RoomType roomType = (random.NextDouble() < profile.EliteRoomProbability) ? RoomType.Elite : RoomType.Normal;

            sequence.Add(roomType);
        }

        sequence.Add(RoomType.End);
        return sequence;
    }

    private static bool TryBuildSequence(StageLayoutRequest request, StagePlan plan, IReadOnlyList<RoomType> sequence, Random random)
    {
        foreach (RoomType roomType in sequence)
        {
            if (!TryAppendRoom(request, plan, roomType, random))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryAppendRoom(StageLayoutRequest request, StagePlan plan, RoomType nextRoomType, Random random)
    {
        int currentRoomIndex = plan.Rooms.Count - 1;
        PlannedRoom currentRoom = plan.Rooms[currentRoomIndex];

        List<ConnectorDirection> exitDirections = new();
        CollectExitDirections(currentRoom, exitDirections);
        Shuffle(exitDirections, random);

        List<RoomDefinition> roomCandidates = new();
        request.Profile.CollectRoomPrefabs(nextRoomType, roomCandidates);

        Shuffle(roomCandidates, random);

        foreach (ConnectorDirection exitDirection in exitDirections)
        {
            HallwayDefinition hallway = request.Profile.GetHallway(exitDirection);

            foreach (RoomDefinition room in roomCandidates)
            {
                ConnectionCandidate candidate = ConnectionCandidateFactory.Create(currentRoom, hallway, room, exitDirection);

                if (candidate == null)
                {
                    continue;
                }

                if (!StageCollisionValidator.CanPlace(request, plan, currentRoomIndex, candidate))
                {
                    continue;
                }

                CommitCandidate(plan, candidate);
                return true;
            }
        }

        return false;
    }

    private static void CommitCandidate(StagePlan plan, ConnectionCandidate candidate)
    {
        int currentRoomIndex = plan.Rooms.Count - 1;
        int nextRoomIndex = plan.Rooms.Count;

        PlannedRoom currentRoom = plan.Rooms[currentRoomIndex];
        currentRoom.ExitDirection = candidate.ExitDirection;

        StageConnection connection = new(currentRoomIndex, nextRoomIndex, candidate.ExitDirection);

        plan.AddHallway(new PlannedHallway(candidate.Hallway, connection));
        plan.AddRoom(new PlannedRoom(candidate.Room, candidate.EntryDirection, false));
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