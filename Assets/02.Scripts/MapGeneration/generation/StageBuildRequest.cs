using UnityEngine;

public class StageBuildRequest
{
    public StagePlan Plan { get; }
    public RoomDefinition StartRoom { get; }
    public Transform RootParent { get; }
    public StageGenerationProfileSO Profile { get; }

    public StageBuildRequest(StagePlan plan, RoomDefinition startRoom, Transform rootParent, StageGenerationProfileSO profile)
    {
        Plan = plan;
        StartRoom = startRoom;
        RootParent = rootParent;
        Profile = profile;
    }
}