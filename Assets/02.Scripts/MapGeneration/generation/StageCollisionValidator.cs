using UnityEngine;

/// <summary>
/// StageLayoutPlanner에서 계획된 방과 복도의 배치가 서로 충돌하는지 여부를 검증하는 정적 클래스.
/// </summary>
internal static class StageCollisionValidator
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

            if (Intersects2D(candidate.Hallway.WorldBounds, plan.Rooms[i].WorldBounds, profile.OverlapClearance, profile.ConnectorAlignmentEpsilon))
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

    private static bool Intersects2D(Bounds first, Bounds second, float clearance, float epsilon)
    {
        float overlapX = Mathf.Min(first.max.x, second.max.x) - Mathf.Max(first.min.x, second.min.x);
        float overlapY = Mathf.Min(first.max.y, second.max.y) - Mathf.Max(first.min.y, second.min.y);

        float threshold = epsilon - clearance;

        return overlapX > threshold && overlapY > threshold;
    }
}