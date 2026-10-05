using System.Collections.Generic;
using UnityEngine;

public class GeneratedStage
{
    public GameObject Root { get; }
    public IReadOnlyList<RoomDefinition> Rooms { get; }
    public IReadOnlyList<HallwayDefinition> Hallways { get; }
    public Bounds WorldBounds { get; }

    public GeneratedStage(GameObject root, IReadOnlyList<RoomDefinition> rooms, IReadOnlyList<HallwayDefinition> hallways, Bounds worldBounds)
    {
        Root = root;
        Rooms = rooms;
        Hallways = hallways;
        WorldBounds = worldBounds;
    }
}