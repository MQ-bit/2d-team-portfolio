using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StagePlan을 기반으로 Stage를 생성하는 과정에서 필요한 컨텍스트 정보를 나타내는 클래스.
/// StagePlan, 시작 방, 루트 부모 Transform, StageGenerationProfileSO 등의 정보를 포함하며, StageMaterializer가 Stage를 생성할 때 사용됨.
/// </summary>
internal class StageBuildContext
{
    public StagePlan Plan { get; }
    public RoomDefinition StartRoom { get; }
    public Transform RootParent { get; }
    public StageGenerationProfileSO Profile { get; }

    public GameObject Root { get; private set; }
    public Transform RoomsRoot { get; set; }
    public Transform HallwaysRoot { get; set; }

    public List<RoomDefinition> Rooms { get; } = new List<RoomDefinition>();
    public List<HallwayDefinition> Hallways { get; } = new List<HallwayDefinition>();

    public ConnectorWallSnapshot StartWallSnapshot { get; private set; }

    public Transform EnemiesRoot { get; set; }
    public Transform TransientsRoot { get; set; }
    public Transform PortalRoot { get; set; }

    public List<GeneratedRoomNode> RoomNodes { get; } = new List<GeneratedRoomNode>();

    public StageBuildContext(StagePlan plan, RoomDefinition startRoom, Transform rootParent, StageGenerationProfileSO profile)
    {
        Plan = plan;
        StartRoom = startRoom;
        RootParent = rootParent;
        Profile = profile;
    }

    public void BeginBuild()
    {
        StartWallSnapshot = new ConnectorWallSnapshot(StartRoom);

        Root = new GameObject($"Stage_{Plan.Seed}_{Plan.GenerationAttemptCount}");

        Root.transform.SetParent(RootParent, false);
        Root.SetActive(false);
    }
}