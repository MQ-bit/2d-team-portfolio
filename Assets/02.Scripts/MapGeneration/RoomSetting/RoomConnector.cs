using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// StageLayoutPlanner에서 계획된 방의 연결 지점을 나타내는 컴포넌트.
/// 연결 지점은 방의 벽 타일맵에서 Carve 영역을 정의하며, 연결 지점의 방향과 월드 위치를 제공함. Carve 메서드를 사용하여 지정된 Wall Tilemap에서 Carve 영역에 해당하는 타일을 제거할 수 있음. 또한, ContainsWallTile 메서드를 사용하여 Carve 영역에 벽 타일
/// </summary>
public class RoomConnector : MonoBehaviour
{
    [Header("Connector")]
    [SerializeField] private ConnectorDirection _direction;

    [SerializeField] private BoundsInt _carveArea; //Wall Tilemap의 셀 좌표. Hallway Connector에서는 비워둠.

    public ConnectorDirection Direction => _direction;
    public BoundsInt CarveArea => _carveArea;

    public Vector3 WorldPosition => transform.position;
    public Vector2 WorldDirection => transform.up.normalized;
    public bool HasValidCarveArea => _carveArea.size.x > 0 && _carveArea.size.y > 0 && _carveArea.size.z > 0;

    /// <summary>
    /// RoomConnector의 Carve 영역에 해당하는 타일을 지정된 Wall Tilemap에서 제거함. 제거된 타일의 수를 반환함. Carve 영역이 유효하지 않거나 Wall Tilemap이 null인 경우, 제거된 타일 수는 0임.
    /// </summary>
    /// <param name="wallTilemap"> 제거할 타일이 있는 Wall Tilemap </param>
    /// <returns></returns>
    public int Carve(Tilemap wallTilemap)
    {
        if(wallTilemap == null)
        {
            Debug.LogError("Wall Tilemap is null. Cannot carve.");
            return 0;
        }

        if(!HasValidCarveArea)
        {
            Debug.LogWarning("Carve area is not valid. Cannot carve.");
            return 0;
        }

        int removedTileCount = 0;

        foreach(Vector3Int cellCoordinate in _carveArea.allPositionsWithin)
        {
            if (!wallTilemap.HasTile(cellCoordinate))
            {
                continue;
            }

            wallTilemap.SetTile(cellCoordinate, null);
            removedTileCount++;
        }

        return removedTileCount;
    }

    public bool ContainsWallTile(Tilemap wallTilemap)
    {
        if(wallTilemap == null || !HasValidCarveArea)
        {
            return false;
        }

        foreach(Vector3Int cellCoordinate in _carveArea.allPositionsWithin)
        {
            if (wallTilemap.HasTile(cellCoordinate))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasValidCarveArea)
        {
            return;
        }

        RoomDefinition room = GetComponentInParent<RoomDefinition>();

        if (room == null || room.WallTilemap == null)
        {
            return;
        }

        Tilemap tilemap = room.WallTilemap;

        Vector3 localMin = tilemap.CellToLocal(_carveArea.min);
        Vector3 localMax = tilemap.CellToLocal(_carveArea.max);

        Gizmos.color = Color.red;
        Gizmos.matrix = tilemap.transform.localToWorldMatrix;
        Gizmos.DrawWireCube((localMin + localMax) * 0.5f, localMax - localMin);
    }
}
