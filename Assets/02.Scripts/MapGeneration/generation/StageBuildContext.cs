using System.Collections.Generic;
using UnityEngine;

public class StageBuildContext
{
    public StageBuildRequest Request { get; }
    public GameObject Root { get; }
    public Transform RoomsRoot { get; internal set; }
    public Transform HallwaysRoot { get; internal set; }
    public List<RoomDefinition> Rooms { get; } = new();
    public List<HallwayDefinition> Hallways { get; } = new();
    public ConnectorWallSnapshot StartWallSnapshot { get; }

    public StageBuildContext(StageBuildRequest request, GameObject root)
    {
        Request = request;
        Root = root;
        StartWallSnapshot = new ConnectorWallSnapshot(request.StartRoom);
    }
}