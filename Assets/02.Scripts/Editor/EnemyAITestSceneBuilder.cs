using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 몬스터 길찾기 테스트용 미로 씬(EnemyAIScene)을 만든다. 메뉴: Tools > Enemy AI > Create EnemyAIScene
// Level_1 맵 프리팹의 타일맵 구성(정렬, 벽 콜라이더)과 타일을 그대로 가져와 미로를 칠하고,
// 맵 소품(기둥, 무덤 등)을 통로 곳곳에 깔고, 플레이어와 몬스터 4종을 미로 반대편에 배치한다.
// 미로는 고정 시드로 만들어서 다시 생성해도 같은 모양이 나온다.
public static class EnemyAITestSceneBuilder
{
    private const string ScenePath = "Assets/01.Scenes/EnemyAIScene.unity";
    private const string LevelPrefabPath = "Assets/03.Prefabs/Map/Level_1.prefab";
    private const string PlayerPrefabPath = "Assets/03.Prefabs/Player/Player.prefab";

    private static readonly string[] EnemyPrefabPaths =
    {
        "Assets/03.Prefabs/Enemy/BombMonster.prefab",
        "Assets/03.Prefabs/Enemy/EliteBombMonster.prefab",
        "Assets/03.Prefabs/Enemy/RangedMonster.prefab",
        "Assets/03.Prefabs/Enemy/EliteRangedMonster.prefab"
    };

    private static readonly string[] PropPrefabPaths =
    {
        "Assets/03.Prefabs/MapProbs/Pillar_1.prefab",
        "Assets/03.Prefabs/MapProbs/Pod1.prefab",
        "Assets/03.Prefabs/MapProbs/Poll_1.prefab",
        "Assets/03.Prefabs/MapProbs/DestoyedBoxes.prefab",
        "Assets/03.Prefabs/MapProbs/Grave_Horizontal.prefab",
        "Assets/03.Prefabs/MapProbs/Grave_Vertical.prefab"
    };

    // 미로 방 개수와 크기. 방 하나는 CorridorWidth x CorridorWidth 타일, 방 사이 벽은 1타일.
    private const int RoomsX = 9;
    private const int RoomsY = 6;
    private const int CorridorWidth = 3;
    private const int Seed = 20261002;
    // 막다른 길만 있으면 경로가 하나뿐이라, 벽 일부를 더 뚫어 우회로를 만든다.
    private const float ExtraOpeningChance = 0.12f;
    private const float PropChance = 0.45f;
    private const int EnemiesPerType = 2;

    [MenuItem("Tools/Enemy AI/Create EnemyAIScene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("EnemyAIScene", $"{ScenePath} 가 이미 있습니다. 덮어쓸까요?", "덮어쓰기", "취소"))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        System.Random random = new System.Random(Seed);

        bool[,] isWall = GenerateMaze(random);
        int width = isWall.GetLength(0);
        int height = isWall.GetLength(1);

        Tilemap wallTilemap = CreateTilemaps(isWall);
        if (wallTilemap == null)
        {
            return;
        }

        Vector2Int playerRoom = new Vector2Int(0, 0);
        PlaceProps(wallTilemap, random, playerRoom);
        PlacePlayer(wallTilemap, playerRoom);
        PlaceEnemies(wallTilemap, random);
        CreateCamera(wallTilemap, width, height);
        new GameObject(nameof(EnemyPathfinder)).AddComponent<EnemyPathfinder>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"EnemyAIScene 생성 완료 : {ScenePath}");
    }

    // 방 격자에서 무작위 깊이 우선 탐색으로 미로를 판 뒤, 벽 일부를 더 뚫는다. true가 벽 타일.
    private static bool[,] GenerateMaze(System.Random random)
    {
        int width = RoomsX * (CorridorWidth + 1) + 1;
        int height = RoomsY * (CorridorWidth + 1) + 1;
        bool[,] isWall = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                isWall[x, y] = true;
            }
        }

        bool[,] visited = new bool[RoomsX, RoomsY];
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        stack.Push(Vector2Int.zero);
        visited[0, 0] = true;
        CarveRoom(isWall, Vector2Int.zero);

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> candidates = new List<Vector2Int>();
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;
                if (IsRoomInside(next) && !visited[next.x, next.y])
                {
                    candidates.Add(next);
                }
            }

            if (candidates.Count == 0)
            {
                stack.Pop();
                continue;
            }

            Vector2Int chosen = candidates[random.Next(candidates.Count)];
            visited[chosen.x, chosen.y] = true;
            CarveRoom(isWall, chosen);
            CarvePassage(isWall, current, chosen);
            stack.Push(chosen);
        }

        for (int x = 0; x < RoomsX; x++)
        {
            for (int y = 0; y < RoomsY; y++)
            {
                Vector2Int room = new Vector2Int(x, y);
                if (x + 1 < RoomsX && random.NextDouble() < ExtraOpeningChance)
                {
                    CarvePassage(isWall, room, room + Vector2Int.right);
                }
                if (y + 1 < RoomsY && random.NextDouble() < ExtraOpeningChance)
                {
                    CarvePassage(isWall, room, room + Vector2Int.up);
                }
            }
        }

        return isWall;
    }

    private static bool IsRoomInside(Vector2Int room)
    {
        return room.x >= 0 && room.y >= 0 && room.x < RoomsX && room.y < RoomsY;
    }

    // 방의 왼쪽 아래 타일 좌표.
    private static Vector2Int GetRoomOrigin(Vector2Int room)
    {
        return new Vector2Int(1 + room.x * (CorridorWidth + 1), 1 + room.y * (CorridorWidth + 1));
    }

    private static void CarveRoom(bool[,] isWall, Vector2Int room)
    {
        Vector2Int origin = GetRoomOrigin(room);
        for (int dx = 0; dx < CorridorWidth; dx++)
        {
            for (int dy = 0; dy < CorridorWidth; dy++)
            {
                isWall[origin.x + dx, origin.y + dy] = false;
            }
        }
    }

    // 이웃한 두 방 사이의 벽을 통로 폭만큼 뚫는다.
    private static void CarvePassage(bool[,] isWall, Vector2Int a, Vector2Int b)
    {
        Vector2Int origin = GetRoomOrigin(a);
        Vector2Int direction = b - a;
        for (int i = 0; i < CorridorWidth; i++)
        {
            if (direction.x != 0)
            {
                int wallX = direction.x > 0 ? origin.x + CorridorWidth : origin.x - 1;
                isWall[wallX, origin.y + i] = false;
            }
            else
            {
                int wallY = direction.y > 0 ? origin.y + CorridorWidth : origin.y - 1;
                isWall[origin.x + i, wallY] = false;
            }
        }
    }

    // Level_1의 타일맵 구성을 그대로 가져와 비우고, 바닥과 벽을 칠한다. 벽 타일맵(콜라이더 포함)을 돌려준다.
    private static Tilemap CreateTilemaps(bool[,] isWall)
    {
        GameObject levelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabPath);
        if (levelPrefab == null)
        {
            Debug.LogError($"{LevelPrefabPath} 를 찾을 수 없음");
            return null;
        }

        GameObject level = (GameObject)PrefabUtility.InstantiatePrefab(levelPrefab);
        PrefabUtility.UnpackPrefabInstance(level, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        level.name = "Maze";
        level.transform.position = Vector3.zero;

        Tilemap groundTilemap = null;
        Tilemap wallTilemap = null;
        foreach (Tilemap tilemap in level.GetComponentsInChildren<Tilemap>(true))
        {
            if (tilemap.name == "Tilemap_Ground")
            {
                groundTilemap = tilemap;
            }
            else if (tilemap.name == "Tilemap_Wall")
            {
                wallTilemap = tilemap;
            }
        }

        if (groundTilemap == null || wallTilemap == null)
        {
            Debug.LogError("Level_1 에서 Tilemap_Ground / Tilemap_Wall 을 찾을 수 없음");
            return null;
        }

        TileBase groundTile = GetMostUsedTile(groundTilemap);
        TileBase wallTile = GetMostUsedTile(wallTilemap);

        foreach (Tilemap tilemap in level.GetComponentsInChildren<Tilemap>(true))
        {
            tilemap.ClearAllTiles();
        }

        // 원래 맵의 소품은 지우고 미로용 소품을 따로 깐다.
        Transform probs = level.transform.Find("Probs");
        if (probs != null)
        {
            Object.DestroyImmediate(probs.gameObject);
        }

        int width = isWall.GetLength(0);
        int height = isWall.GetLength(1);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                groundTilemap.SetTile(cell, groundTile);
                if (isWall[x, y])
                {
                    wallTilemap.SetTile(cell, wallTile);
                }
            }
        }

        groundTilemap.CompressBounds();
        wallTilemap.CompressBounds();
        return wallTilemap;
    }

    private static TileBase GetMostUsedTile(Tilemap tilemap)
    {
        Dictionary<TileBase, int> counts = new Dictionary<TileBase, int>();
        foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(position);
            if (tile != null)
            {
                counts.TryGetValue(tile, out int count);
                counts[tile] = count + 1;
            }
        }

        TileBase best = null;
        int bestCount = 0;
        foreach (KeyValuePair<TileBase, int> pair in counts)
        {
            if (pair.Value > bestCount)
            {
                best = pair.Key;
                bestCount = pair.Value;
            }
        }

        return best;
    }

    // 방의 네 귀퉁이 중 한 곳에만 소품을 둬서 방 가운데 줄(통로가 이어지는 곳)은 항상 비어 있게 한다.
    private static void PlaceProps(Tilemap wallTilemap, System.Random random, Vector2Int playerRoom)
    {
        GameObject propsRoot = new GameObject("Props");

        for (int x = 0; x < RoomsX; x++)
        {
            for (int y = 0; y < RoomsY; y++)
            {
                Vector2Int room = new Vector2Int(x, y);
                if (room == playerRoom || random.NextDouble() >= PropChance)
                {
                    continue;
                }

                GameObject propPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PropPrefabPaths[random.Next(PropPrefabPaths.Length)]);
                if (propPrefab == null)
                {
                    continue;
                }

                Vector2Int origin = GetRoomOrigin(room);
                int cornerX = random.Next(2) == 0 ? 0 : CorridorWidth - 1;
                int cornerY = random.Next(2) == 0 ? 0 : CorridorWidth - 1;
                Vector3 position = wallTilemap.GetCellCenterWorld(new Vector3Int(origin.x + cornerX, origin.y + cornerY, 0));

                GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(propPrefab, propsRoot.transform);
                prop.transform.position = position;
            }
        }
    }

    private static void PlacePlayer(Tilemap wallTilemap, Vector2Int room)
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            Debug.LogError($"{PlayerPrefabPath} 를 찾을 수 없음");
            return;
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = GetRoomCenter(wallTilemap, room);
    }

    // 플레이어 반대편 구역부터 몬스터를 종류별로 나눠 배치한다.
    private static void PlaceEnemies(Tilemap wallTilemap, System.Random random)
    {
        GameObject enemiesRoot = new GameObject("Enemies");
        Vector2Int[] spawnRooms =
        {
            new Vector2Int(RoomsX - 1, RoomsY - 1),
            new Vector2Int(RoomsX - 1, 0),
            new Vector2Int(0, RoomsY - 1),
            new Vector2Int(RoomsX / 2, RoomsY / 2),
            new Vector2Int(RoomsX - 2, RoomsY / 2),
            new Vector2Int(RoomsX / 2, RoomsY - 1),
            new Vector2Int(RoomsX / 2, 0),
            new Vector2Int(RoomsX - 1, RoomsY - 2)
        };

        int spawnIndex = 0;
        for (int round = 0; round < EnemiesPerType; round++)
        {
            foreach (string path in EnemyPrefabPaths)
            {
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (enemyPrefab == null)
                {
                    Debug.LogError($"{path} 를 찾을 수 없음");
                    continue;
                }

                Vector2Int room = spawnRooms[spawnIndex % spawnRooms.Length];
                spawnIndex++;

                Vector3 offset = new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, 0f);
                GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, enemiesRoot.transform);
                enemy.transform.position = GetRoomCenter(wallTilemap, room) + offset;
            }
        }
    }

    private static Vector3 GetRoomCenter(Tilemap wallTilemap, Vector2Int room)
    {
        Vector2Int origin = GetRoomOrigin(room);
        int half = CorridorWidth / 2;
        return wallTilemap.GetCellCenterWorld(new Vector3Int(origin.x + half, origin.y + half, 0));
    }

    // 미로 전체가 한 화면에 들어오는 고정 카메라. 몬스터들이 어떻게 돌아오는지 한눈에 보기 위함.
    private static void CreateCamera(Tilemap wallTilemap, int width, int height)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);

        Vector3 min = wallTilemap.CellToWorld(Vector3Int.zero);
        Vector3 max = wallTilemap.CellToWorld(new Vector3Int(width, height, 0));
        Vector3 center = (min + max) * 0.5f;
        cameraObject.transform.position = new Vector3(center.x, center.y, -10f);

        const float Aspect = 16f / 9f;
        float sizeByHeight = (max.y - min.y) * 0.5f;
        float sizeByWidth = (max.x - min.x) * 0.5f / Aspect;
        camera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth) + 1f;
    }
}
