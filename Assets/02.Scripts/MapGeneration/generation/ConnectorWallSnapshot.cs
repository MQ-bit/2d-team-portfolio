using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ConnectorWallSnapshot
{
    private readonly RoomDefinition _room;
    private readonly Dictionary<Vector3Int, TileBase> _tiles = new Dictionary<Vector3Int, TileBase>();

    public ConnectorWallSnapshot(RoomDefinition room)
    {
        _room = room;
        Capture();
    }

    public void Restore()
    {
        if (_room == null || _room.WallTilemap == null)
        {
            return;
        }

        foreach (KeyValuePair<Vector3Int, TileBase> tile in _tiles)
        {
            _room.WallTilemap.SetTile(tile.Key, tile.Value);
        }

        _room.ResetConnectorUsage();
        _room.RefreshWallCollider();
    }

    private void Capture()
    {
        if (_room == null || _room.WallTilemap == null)
        {
            return;
        }

        foreach (RoomConnector connector in _room.Connectors)
        {
            if (connector == null || !connector.HasValidCarveArea)
            {
                continue;
            }

            foreach (Vector3Int cell in connector.CarveArea.allPositionsWithin)
            {
                if (_tiles.ContainsKey(cell))
                {
                    continue;
                }

                _tiles.Add(cell, _room.WallTilemap.GetTile(cell));
            }
        }
    }
}