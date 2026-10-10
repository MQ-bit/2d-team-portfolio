using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MinimapTerrainRasterizer
{
    private enum TerrainCell : byte
    {
        Ground,
        Wall
    }

    private const string Ground_Tile_mapName = "Tilemap_Ground";
    private const string Wall_Tile_mapName = "Tilemap_Wall";

    public static bool TryBuild(GeneratedStage stage, float viewWorldSize, Color32 groundColor, Color32 wallColor, out MinimapTerrainData terrainData)
    {
        terrainData = null;

        if (stage == null)
        {
            return false;
        }

        Dictionary<Vector2Int, TerrainCell> cells = new Dictionary<Vector2Int, TerrainCell>();
        HashSet<Tilemap> processedTilemaps = new HashSet<Tilemap>();

        if (stage.Rooms != null)
        {
            foreach (RoomDefinition room in stage.Rooms)
            {
                CollectTerrainCells(room, cells, processedTilemaps);
            }
        }

        if (stage.Hallways != null)
        {
            foreach (HallwayDefinition hallway in stage.Hallways)
            {
                CollectTerrainCells(hallway, cells, processedTilemaps);
            }
        }

        if (cells.Count == 0)
        {
            Debug.LogWarning($"{nameof(MinimapTerrainRasterizer)}: 표시할 Ground/Wall 타일이 없습니다.");

            return false;
        }

        Vector2Int minCell = new(int.MaxValue, int.MaxValue);

        Vector2Int maxCell = new(int.MinValue, int.MinValue);

        foreach (Vector2Int cell in cells.Keys)
        {
            minCell = Vector2Int.Min(minCell, cell);
            maxCell = Vector2Int.Max(maxCell, cell);
        }

        // 맵 가장자리에서도 플레이어를 중앙에 유지할 수 있도록
        // 표시 범위 절반만큼 투명 여백을 추가한다.
        int padding = Mathf.CeilToInt(viewWorldSize * 0.5f) + 2;

        minCell -= Vector2Int.one * padding;
        maxCell += Vector2Int.one * padding;

        int width = maxCell.x - minCell.x + 1;
        int height = maxCell.y - minCell.y + 1;

        int maxTextureSize = SystemInfo.maxTextureSize;

        if (width > maxTextureSize || height > maxTextureSize)
        {
            Debug.LogError($"{nameof(MinimapTerrainRasterizer)}: 미니맵 텍스처 크기({width}x{height})가 제한({maxTextureSize})을 초과했습니다.");

            return false;
        }

        Color32[] pixels = new Color32[width * height];

        foreach (KeyValuePair<Vector2Int, TerrainCell> pair in cells)
        {
            int x = pair.Key.x - minCell.x;
            int y = pair.Key.y - minCell.y;

            pixels[y * width + x] = pair.Value == TerrainCell.Wall ? wallColor : groundColor;
        }

        Texture2D texture = new(width, height, TextureFormat.RGBA32, false, false)
        {
            name = "Runtime Minimap Terrain",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        terrainData = new MinimapTerrainData(texture, minCell);

        return true;
    }

    private static void CollectTerrainCells(Component owner, Dictionary<Vector2Int, TerrainCell> cells, HashSet<Tilemap> processedTilemaps)
    {
        if (owner == null)
        {
            return;
        }

        Tilemap[] tilemaps = owner.GetComponentsInChildren<Tilemap>(true);

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null || !processedTilemaps.Add(tilemap))
            {
                continue;
            }

            bool isGround = tilemap.name == Ground_Tile_mapName;

            bool isWall = tilemap.name == Wall_Tile_mapName;

            // Tilemap_GroundDeco 등은 제외한다.
            if (!isGround && !isWall)
            {
                continue;
            }

            foreach (Vector3Int cellPosition in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cellPosition))
                {
                    continue;
                }

                Vector3 worldCenter = tilemap.GetCellCenterWorld(cellPosition);

                // 현재 프로젝트의 Grid는 회전되지 않은 1x1 셀이다.
                Vector2Int worldCell = new(Mathf.FloorToInt(worldCenter.x), Mathf.FloorToInt(worldCenter.y));

                if (isWall)
                {
                    // Ground와 Wall이 겹칠 경우 Wall이 우선한다.
                    cells[worldCell] = TerrainCell.Wall;
                }
                else if (!cells.ContainsKey(worldCell))
                {
                    cells.Add(worldCell, TerrainCell.Ground);
                }
            }
        }
    }
}