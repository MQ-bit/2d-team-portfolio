using UnityEngine;

public class StageConnectionTestController : MonoBehaviour
{
    private const float ConnectionTolerance = 0.001f;

    [Header("Scene References")]
    [SerializeField] private RoomDefinition _startRoom;
    [SerializeField] private Transform _generatedStageRoot;

    [Header("Test Prefabs")]
    [SerializeField] private HallwayDefinition _horizontalHallwayPrefab;
    [SerializeField] private RoomDefinition _normalRoomPrefab;

    [Header("Test Settings")]
    [SerializeField] private bool _generateOnStart = true;

    private bool _hasGenerated;

    private void Start()
    {
        if (_generateOnStart)
        {
            GenerateTestConnection();
        }
    }

    [ContextMenu("Generate Test Connection")]
    public void GenerateTestConnection()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("단일 연결 테스트는 Play Mode에서 실행해야 합니다.", this);

            return;
        }

        if (_hasGenerated)
        {
            Debug.LogWarning("테스트 연결이 이미 생성되었습니다.", this);
            return;
        }

        if (!ValidateReferences())
        {
            return;
        }

        if (!_startRoom.TryGetConnector(ConnectorDirection.East, out RoomConnector startEast))
        {
            Debug.LogError("StartRoom에 East Connector가 없습니다.", _startRoom);
            return;
        }

        Transform root = _generatedStageRoot != null ? _generatedStageRoot : transform;
        HallwayDefinition hallway = Instantiate( _horizontalHallwayPrefab, Vector3.zero, Quaternion.identity, root);

        if (!hallway.TryGetConnector(ConnectorDirection.West, out RoomConnector hallwayWest) ||
            !hallway.TryGetConnector(ConnectorDirection.East, out RoomConnector hallwayEast))
        {
            Debug.LogError("HorizontalHallway의 East/West Connector가 올바르지 않습니다.", hallway);
            Destroy(hallway.gameObject);
            return;
        }

        AlignRootToTarget(hallway.transform, hallwayWest.transform, startEast.WorldPosition);

        RoomDefinition normalRoom = Instantiate(_normalRoomPrefab, Vector3.zero, Quaternion.identity, root);

        if (!normalRoom.TryGetConnector(ConnectorDirection.West, out RoomConnector normalWest))
        {
            Debug.LogError("NormalRoom에 West Connector가 없습니다.", normalRoom);

            Destroy(hallway.gameObject);
            Destroy(normalRoom.gameObject);
            return;
        }

        AlignRootToTarget(normalRoom.transform, normalWest.transform, hallwayEast.WorldPosition);

        bool carvedStart = _startRoom.TryCarveConnector(ConnectorDirection.East, out int startRemovedTiles);

        bool carvedNormal = normalRoom.TryCarveConnector(ConnectorDirection.West, out int normalRemovedTiles);

        if (!carvedStart || !carvedNormal)
        {
            Debug.LogError("Room Connector 벽 개방에 실패했습니다.", this);
            return;
        }

        _startRoom.RefreshWallCollider();
        normalRoom.RefreshWallCollider();

        Physics2D.SyncTransforms();

        bool startConnected = ValidateConnection(startEast, hallwayWest);

        bool normalConnected = ValidateConnection(hallwayEast, normalWest);

        if (!startConnected || !normalConnected)
        {
            Debug.LogError("Connector 위치 정렬 검증에 실패했습니다.", this);
            return;
        }

        if (startRemovedTiles == 0 || normalRemovedTiles == 0)
        {
            Debug.LogWarning($"연결은 생성됐지만 제거된 타일 수가 0입니다. Start={startRemovedTiles}, Normal={normalRemovedTiles}", this);
        }

        _hasGenerated = true;

        Debug.Log($"단일 연결 생성 성공. Start 제거 타일={startRemovedTiles}, Normal 제거 타일={normalRemovedTiles}", this);
    }

    private bool ValidateReferences()
    {
        if (_startRoom == null)
        {
            Debug.LogError("StartRoom 참조가 없습니다.", this);
            return false;
        }

        if (_horizontalHallwayPrefab == null)
        {
            Debug.LogError("HorizontalHallway 프리팹 참조가 없습니다.", this);
            return false;
        }

        if (_normalRoomPrefab == null)
        {
            Debug.LogError("NormalRoom 프리팹 참조가 없습니다.", this);
            return false;
        }

        if (_startRoom.Type != RoomType.Start)
        {
            Debug.LogError("StartRoom 참조의 RoomType이 Start가 아닙니다.", _startRoom);
            return false;
        }

        if (_normalRoomPrefab.Type != RoomType.Normal)
        {
            Debug.LogError("NormalRoom 프리팹의 RoomType이 Normal이 아닙니다.", _normalRoomPrefab);
            return false;
        }

        if (_horizontalHallwayPrefab.Orientation != HallwayOrientation.Horizontal)
        {
            Debug.LogError("Hallway 프리팹이 Horizontal로 설정되지 않았습니다.", _horizontalHallwayPrefab);
            return false;
        }

        return true;
    }

    private static void AlignRootToTarget(Transform targetRoot, Transform connector, Vector3 targetWorldPosition)
    {
        targetRoot.position += targetWorldPosition - connector.position;
    }

    private static bool ValidateConnection(RoomConnector first, RoomConnector second)
    {
        if (first.Direction.OppositeDirection() != second.Direction)
        {
            return false;
        }

        return Vector3.Distance(first.WorldPosition, second.WorldPosition) <= ConnectionTolerance;
    }
}