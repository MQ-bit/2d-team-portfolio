using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StageLayoutPlanner에서 계획된 Stage의 생성 결과를 나타내는 클래스.
/// 생성된 Stage의 루트 GameObject, 방 및 복도의 정의, 그리고 Stage의 월드 경계와 같은 정보를 포함하며, StageMaterializer가 Stage를 생성한 후 반환함.
/// </summary>
public class GeneratedStage
{
    public GameObject Root { get; }
    public IReadOnlyList<RoomDefinition> Rooms { get; }
    public IReadOnlyList<GeneratedRoomNode> RoomNodes { get; }
    public IReadOnlyList<HallwayDefinition> Hallways { get; }

    public Transform EnemiesRoot { get; }
    public Transform TransientsRoot { get; }
    public Transform PortalRoot { get; }

    public Bounds WorldBounds { get; }

    public GeneratedStage(
        GameObject root,
        IReadOnlyList<RoomDefinition> rooms,
        IReadOnlyList<GeneratedRoomNode> roomNodes,
        IReadOnlyList<HallwayDefinition> hallways,
        Transform enemiesRoot,
        Transform transientsRoot,
        Transform portalRoot,
        Bounds worldBounds)
    {
        Root = root;
        Rooms = rooms;
        RoomNodes = roomNodes;
        Hallways = hallways;
        EnemiesRoot = enemiesRoot;
        TransientsRoot = transientsRoot;
        PortalRoot = portalRoot;
        WorldBounds = worldBounds;
    }

    public bool TryGetRoomContaining(Vector3 worldPosition, out GeneratedRoomNode node)
    {
        foreach (GeneratedRoomNode candidate in RoomNodes)
        {
            if (candidate?.Room != null && candidate.Room.WorldBounds.Contains(worldPosition))
            {
                node = candidate;
                return true;
            }
        }

        node = null;
        return false;
    }
}
