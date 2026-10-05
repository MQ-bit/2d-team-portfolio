using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// RoomConnector는 RoomDefinition에 연결된 Connector를 나타내며, 해당 Connector의 방향과 Carve 영역을 정의합니다.
/// </summary>
public class RoomConnector : MonoBehaviour
{
    [Header("Connector")]
    [SerializeField] private ConnectorDirection _direction;

    [SerializeField] private BoundsInt _carveArea; //Wall Tilemap의 셀 좌표. Hallway Connector에서는 비워두기.

    private bool _isUsed;

    public ConnectorDirection Direction => _direction;
    public BoundsInt CarveArea => _carveArea;

    public Vector3 WorldPosition => transform.position;
    public Vector2 WorldDirection => transform.up.normalized;
    
    public bool IsUsed => _isUsed;
    public bool HasValidCarveArea => _carveArea.size.x > 0 && _carveArea.size.y > 0 && _carveArea.size.z > 0;

    /// <summary>
    /// Carve 메서드는 지정된 Wall Tilemap에서 Carve 영역에 해당하는 타일을 제거합니다. 제거된 타일의 수를 반환하며, Carve 영역이 유효하지 않거나 Wall Tilemap이 null인 경우 0을 반환합니다.
    /// </summary>
    /// <param name="wallTilemap"></param>
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

        _isUsed = true;
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

    public void ResetUsage()
    {
        _isUsed = false;
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
