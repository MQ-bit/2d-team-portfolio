using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stage를 생성하기 위한 설정을 나타내는 ScriptableObject.
/// 방 프리팹, 복도 프리팹, 방 선택 확률, 레이아웃 설정 등의 정보를 포함하며, StageLayoutPlanner가 Stage를 계획할 때 사용됨.
/// </summary>
[CreateAssetMenu(fileName = "StageGenerationProfile", menuName = "Map Generation/Stage Generation Profile")]
public class StageGenerationProfileSO : ScriptableObject
{
    [Header("Room Prefabs")]
    [SerializeField] private RoomDefinition[] _normalRooms = Array.Empty<RoomDefinition>();
    [SerializeField] private RoomDefinition[] _eliteRooms = Array.Empty<RoomDefinition>();
    [SerializeField] private RoomDefinition _endRoom;

    [Header("Stage Objects")]
    [SerializeField] private InteractablePortal _portalPrefab;

    [Header("RewardBox")]
    [SerializeField] private InteractablePotionBox _goldRewardBoxPrefab;
    [SerializeField] private InteractablePotionBox _silverRewardBoxPrefab;

    [Header("Reward Count")]
    [SerializeField] private Vector2Int _normalRoomRewardMinMax = new Vector2Int(1, 3);
    [SerializeField] private Vector2Int _eliteRoomRewardMinMax = new Vector2Int(2, 4);

    [SerializeField, Min(0f)] private float _rewardSpawnRadius = 2f;
    [SerializeField, Min(1f)] private int _rewardPlacementAttempts = 20;
    [SerializeField] private LayerMask _rewardBlockingLayers;

    [Header("Hallway Prefabs")]
    [SerializeField] private HallwayDefinition _horizontalHallway;
    [SerializeField] private HallwayDefinition _verticalHallway;

    [Header("Room Selection")]
    [SerializeField, Range(0f, 1f)] private float _additionalRoomChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _eliteRoomProbability = 0.25f;

    [Header("Layout")]
    [SerializeField, Min(1)] private int _maxLayoutRetryCount = 100;
    [SerializeField, Min(0f)] private float _overlapClearance;
    [SerializeField, Min(0.0001f)] private float _connectorAlignmentEpsilon = 0.001f;

    public IReadOnlyList<RoomDefinition> NormalRooms => _normalRooms;
    public IReadOnlyList<RoomDefinition> EliteRooms => _eliteRooms;
    public RoomDefinition EndRoom => _endRoom;
    public InteractablePortal PortalPrefab => _portalPrefab;
    public float AdditionalRoomChance => _additionalRoomChance;
    public float EliteRoomProbability => _eliteRoomProbability;
    public int MaxLayoutRetryCount => _maxLayoutRetryCount;
    public float OverlapClearance => _overlapClearance;
    public float ConnectorAlignmentEpsilon => _connectorAlignmentEpsilon;
    public float RewardSpawnRadius => _rewardSpawnRadius;
    public int RewardPlacementAttempts => _rewardPlacementAttempts;
    public LayerMask RewardBlockingLayers => _rewardBlockingLayers;
    public Vector2Int NormalRoomRewardMinMax => _normalRoomRewardMinMax;
    public Vector2Int EliteRoomRewardMinMax => _eliteRoomRewardMinMax;


    public HallwayDefinition GetHallway(ConnectorDirection direction)
    {
        return direction.IsHorizontal() ? _horizontalHallway : _verticalHallway;
    }

    public void CollectRoomPrefabs(RoomType roomType, List<RoomDefinition> destination)
    {
        destination.Clear();

        switch (roomType)
        {
            case RoomType.Normal:
                {
                    destination.AddRange(_normalRooms);
                    break;
                }

            case RoomType.Elite:
                {
                    destination.AddRange(_eliteRooms);
                    break;
                }

            case RoomType.End:
                {
                    if (_endRoom != null)
                    {
                        destination.Add(_endRoom);
                    }
                    break;
                }
        }
    }

    public bool TryValidate(out string error)
    {
        if (!ValidateRooms(_normalRooms, RoomType.Normal, out error) ||
            !ValidateRooms(_eliteRooms, RoomType.Elite, out error))
        {
            return false;
        }
        if (_endRoom == null || _endRoom.Type != RoomType.End)
        {
            error = "EndRoom 프리팹이 없거나 RoomType이 End가 아닙니다.";
            return false;
        }

        if (_portalPrefab == null)
        {
            error = "Portal 프리팹이 없습니다.";
            return false;
        }

        if (_endRoom.PortalSpawnPoint == null)
        {
            error = "EndRoom 프리팹에 PortalSpawnPoint가 없습니다.";
            return false;
        }

        if (_horizontalHallway == null || _horizontalHallway.Orientation != HallwayOrientation.Horizontal)
        {
            error = "HorizontalHallway 설정이 유효하지 않습니다.";
            return false;
        }

        if (_verticalHallway == null || _verticalHallway.Orientation != HallwayOrientation.Vertical)
        {
            error = "VerticalHallway 설정이 유효하지 않습니다.";
            return false;
        }

        if (!ValidateRewardPrefab(_goldRewardBoxPrefab, "Gold", out error) ||
            !ValidateRewardPrefab(_silverRewardBoxPrefab, "Silver", out error))
        {
            return false;
        }

        if (_rewardSpawnRadius < 0f)
        {
            error = "Reward Spawn Radius는 0 이상이어야 합니다.";
            return false;
        }

        if (_rewardPlacementAttempts <= 0)
        {
            error = "Reward Placement Attempts는 1 이상이어야 합니다.";
            return false;
        }

        int requiredRewardLayers = LayerMask.GetMask("Wall", "FieldItem");

        if ((_rewardBlockingLayers.value & requiredRewardLayers) != requiredRewardLayers)
        {
            error = "Reward Blocking Layers에 Wall과 FieldItem이 모두 포함되어야 합니다.";
            return false;
        }


        error = null;
        return true;
    }

    private static bool ValidateRewardPrefab(InteractablePotionBox prefab, string label, out string error)
    {
        if (prefab == null)
        {
            error = $"{label} Reward Box 프리팹이 없습니다.";
            return false;
        }

        BoxCollider2D collider = prefab.GetComponent<BoxCollider2D>();

        if (collider == null || collider.isTrigger)
        {
            error = $"{label} Reward Box에 비트리거 BoxCollider2D가 없습니다.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ValidateRooms(RoomDefinition[] rooms, RoomType expectedType, out string error)
    {
        if (rooms == null || rooms.Length == 0)
        {
            error = $"{expectedType} Room 프리팹 배열이 비어 있습니다.";
            return false;
        }

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] == null || rooms[i].Type != expectedType)
            {
                error = $"{expectedType} Room 배열의 {i}번 프리팹이 유효하지 않습니다.";
                return false;
            }
        }

        error = null;
        return true;
    }


    public InteractablePotionBox GetRewardBoxPrefab(RoomType roomType)
    {
        return roomType switch
        {
            RoomType.Start => _goldRewardBoxPrefab,
            RoomType.Normal => _silverRewardBoxPrefab,
            RoomType.Elite => _goldRewardBoxPrefab,
            RoomType.End => _goldRewardBoxPrefab,
            _ => null
        };
    }
}