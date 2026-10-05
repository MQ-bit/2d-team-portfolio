using UnityEngine;

public static class ConnectionCandidateFactory
{
    public static ConnectionCandidate Create(PlannedRoom currentRoom, HallwayDefinition hallway, RoomDefinition nextRoom, ConnectorDirection exitDirection)
    {
        if (currentRoom == null || hallway == null || nextRoom == null)
        {
            return null;
        }

        ConnectorDirection entryDirection = exitDirection.OppositeDirection();

        if (!currentRoom.Definition.TryGetConnector(exitDirection, out RoomConnector currentExit))
        {
            return null;
        }

        if (!hallway.TryGetConnector(entryDirection, out RoomConnector hallwayEntry))
        {
            return null;
        }

        if (!hallway.TryGetConnector(exitDirection, out RoomConnector hallwayExit))
        {
            return null;
        }

        if (!nextRoom.TryGetConnector(entryDirection, out RoomConnector roomEntry))
        {
            return null;
        }

        if (nextRoom.Type != RoomType.End && !HasExitOtherThan(nextRoom, entryDirection))
        {
            return null;
        }

        Vector3 currentExitPosition = StageLayoutGeometry.GetConnectorWorldPosition(currentRoom, currentExit);

        Vector3 hallwayEntryOffset = StageLayoutGeometry.GetConnectorLocalPosition(hallway, hallwayEntry);
        Vector3 hallwayPosition = currentExitPosition - hallwayEntryOffset;

        Bounds hallwayBounds = StageLayoutGeometry.TranslateBounds(hallway.LocalBounds, hallwayPosition);
        Vector3 hallwayExitPosition = hallwayPosition + StageLayoutGeometry.GetConnectorLocalPosition(hallway, hallwayExit);

        Vector3 roomEntryOffset = StageLayoutGeometry.GetConnectorLocalPosition(nextRoom, roomEntry);
        Vector3 roomPosition = hallwayExitPosition - roomEntryOffset;
        Bounds roomBounds = StageLayoutGeometry.TranslateBounds(nextRoom.LocalBounds, roomPosition);

        LayoutPlacement<HallwayDefinition> hallwayPlacement = new LayoutPlacement<HallwayDefinition>(hallway, hallwayPosition, hallwayBounds);
        LayoutPlacement<RoomDefinition> roomPlacement = new LayoutPlacement<RoomDefinition>(nextRoom, roomPosition, roomBounds);

        return new ConnectionCandidate(hallwayPlacement, roomPlacement, exitDirection);
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
}