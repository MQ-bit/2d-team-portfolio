using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MapPrefabValidator
{
    private static readonly string[] SearchFolders =
    {
        "Assets/03.Prefabs/Map/StartRoom",
        "Assets/03.Prefabs/Map/EndRoom",
        "Assets/03.Prefabs/Map/NormalRooms",
        "Assets/03.Prefabs/Map/EliteRooms",
        "Assets/03.Prefabs/Map/Hallway"
    };

    [MenuItem("Tools/Map Generation/Validate Prefabs")]
    private static void ValidatePrefabs()
    {
        string[] guids =
            AssetDatabase.FindAssets("t:Prefab", SearchFolders);

        List<string> allErrors = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = null;

            try
            {
                root = PrefabUtility.LoadPrefabContents(path);

                RoomDefinition[] rooms =
                    root.GetComponentsInChildren<RoomDefinition>(true);

                HallwayDefinition[] hallways =
                    root.GetComponentsInChildren<HallwayDefinition>(true);

                if (rooms.Length == 0 && hallways.Length == 0)
                {
                    allErrors.Add(
                        $"{path}: RoomDefinition 또는 HallwayDefinition이 없습니다.");

                    continue;
                }

                if (rooms.Length > 1)
                {
                    allErrors.Add(
                        $"{path}: RoomDefinition이 두 개 이상 존재합니다.");
                }

                if (hallways.Length > 1)
                {
                    allErrors.Add(
                        $"{path}: HallwayDefinition이 두 개 이상 존재합니다.");
                }

                foreach (RoomDefinition room in rooms)
                {
                    List<string> errors = new();
                    room.CollectValidationErrors(errors);

                    foreach (string error in errors)
                    {
                        allErrors.Add($"{path}: {error}");
                    }
                }

                foreach (HallwayDefinition hallway in hallways)
                {
                    List<string> errors = new();
                    hallway.CollectValidationErrors(errors);

                    foreach (string error in errors)
                    {
                        allErrors.Add($"{path}: {error}");
                    }
                }
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        if (allErrors.Count == 0)
        {
            Debug.Log(
                $"Map Prefab 검증 성공: {guids.Length}개 프리팹");
            return;
        }

        foreach (string error in allErrors)
        {
            Debug.LogError(error);
        }

        Debug.LogError(
            $"Map Prefab 검증 실패: {allErrors.Count}개 오류");
    }
}