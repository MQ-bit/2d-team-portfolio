using UnityEngine;

public static class StageCollisionValidator
{
    public static bool CanPlace(StageLayoutRequest request, StagePlan plan, int currentRoomIndex, ConnectionCandidate candidate)
    {
        StageGenerationProfileSO profile = request.Profile;

        for (int i = 0; i < plan.Rooms.Count; i++)
        {
            if (i == currentRoomIndex)
            {
                continue;
            }
            
            float clearance = i == currentRoomIndex ? 0f : profile.OverlapClearance;

            if (Intersects2D(candidate.Hallway.WorldBounds, plan.Rooms[i].WorldBounds, clearance, profile.ConnectorAlignmentEpsilon))
            {
                return false;
            }
        }

        foreach (PlannedHallway hallway in plan.Hallways)
        {
            if (Intersects2D(candidate.Hallway.WorldBounds, hallway.WorldBounds, profile.OverlapClearance, profile.ConnectorAlignmentEpsilon))
            {
                return false;
            }
        }

        foreach (PlannedRoom room in plan.Rooms)
        {
            if (Intersects2D(candidate.Room.WorldBounds, room.WorldBounds, profile.OverlapClearance, profile.ConnectorAlignmentEpsilon))
            {
                return false;
            }
        }

        foreach (PlannedHallway hallway in plan.Hallways)
        {
            if (Intersects2D(candidate.Room.WorldBounds, hallway.WorldBounds, profile.OverlapClearance, profile.ConnectorAlignmentEpsilon))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryValidatePlan(StageLayoutRequest request, StagePlan plan, out StageFailure failure)
    {
        StageGenerationProfileSO profile = request.Profile;

        for (int i = 0; i < plan.Rooms.Count; i++)
        {
            for (int j = i + 1; j < plan.Rooms.Count; j++)
            {
                if (Intersects2D(plan.Rooms[i].WorldBounds, plan.Rooms[j].WorldBounds, profile.OverlapClearance, profile.ConnectorAlignmentEpsilon))
                {
                    failure = new StageFailure(StageFailureCode.OverlapDetected, $"Room {i}와 Room {j}가 겹칩니다.");
                    return false;
                }
            }
        }

        for (int i = 0; i < plan.Hallways.Count; i++)
        {
            for (int j = i + 1; j < plan.Hallways.Count; j++)
            {
                if (Intersects2D(plan.Hallways[i].WorldBounds, plan.Hallways[j].WorldBounds, profile.OverlapClearance, profile.ConnectorAlignmentEpsilon))
                {
                    failure = new StageFailure(StageFailureCode.OverlapDetected, $"Hallway {i}와 Hallway {j}가 겹칩니다.");
                    return false;
                }
            }
        }

        return ValidateRoomHallwayOverlaps(request, plan, out failure);
    }

    private static bool ValidateRoomHallwayOverlaps(StageLayoutRequest request, StagePlan plan, out StageFailure failure)
    {
        StageGenerationProfileSO profile = request.Profile;

        for (int roomIndex = 0; roomIndex < plan.Rooms.Count; roomIndex++)
        {
            for (int hallwayIndex = 0; hallwayIndex < plan.Hallways.Count; hallwayIndex++)
            {
                PlannedHallway hallway = plan.Hallways[hallwayIndex];
                bool directlyConnected = hallway.Connection.FromRoomIndex == roomIndex || hallway.Connection.ToRoomIndex == roomIndex;

                if (directlyConnected) continue;
                if (!Intersects2D(plan.Rooms[roomIndex].WorldBounds, hallway.WorldBounds, profile.OverlapClearance, profile.ConnectorAlignmentEpsilon))
                {
                    continue;
                }

                failure = new StageFailure(StageFailureCode.OverlapDetected, $"Room {roomIndex}와 Hallway {hallwayIndex}가 겹칩니다.");
                return false;
            }
        }

        failure = StageFailure.None;
        return true;
    }

    private static bool Intersects2D(Bounds first, Bounds second, float clearance, float epsilon)
    {
        float overlapX = Mathf.Min(first.max.x, second.max.x) - Mathf.Max(first.min.x, second.min.x);
        float overlapY = Mathf.Min(first.max.y, second.max.y) - Mathf.Max(first.min.y, second.min.y);
 
        float threshold = epsilon - clearance;

        return overlapX > threshold && overlapY > threshold;
    }
}