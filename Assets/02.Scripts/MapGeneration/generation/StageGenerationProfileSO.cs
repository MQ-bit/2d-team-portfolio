using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageGenerationProfile", menuName = "Map Generation/Stage Generation Profile")]
public class StageGenerationProfileSO : ScriptableObject
{
    [Header("Room Prefabs")]
    [SerializeField] private RoomDefinition[] _normalRooms = Array.Empty<RoomDefinition>();
    [SerializeField] private RoomDefinition[] _eliteRooms = Array.Empty<RoomDefinition>();
    [SerializeField] private RoomDefinition _endRoom;

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
    public float AdditionalRoomChance => _additionalRoomChance;
    public float EliteRoomProbability => _eliteRoomProbability;
    public int MaxLayoutRetryCount => _maxLayoutRetryCount;
    public float OverlapClearance => _overlapClearance;
    public float ConnectorAlignmentEpsilon => _connectorAlignmentEpsilon;

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
        if (!ValidateRooms(_normalRooms, RoomType.Normal, out error))
        {
            return false;
        }

        if (!ValidateRooms(_eliteRooms, RoomType.Elite, out error))
        {
            return false;
        }

        if (_endRoom == null || _endRoom.Type != RoomType.End)
        {
            error = "EndRoom 프리팹이 없거나 RoomType이 End가 아닙니다.";
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
}