using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class PerformanceDemoSceneBuilder
{
    public const string ScenePath = "Assets/01.Scenes/Optimization/PerformanceDemoScene.unity";
    private const string DataPath = "Assets/PerformanceDemo";

    [MenuItem("Tools/Performance/Add Game Loop Scenarios")]
    public static void AddGameLoopScenarios()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play mode and wait for compilation before upgrading.");
        if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != ScenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the modified scene before upgrading the demo.");
            EditorSceneManager.OpenScene(ScenePath);
        }
        var runner = Object.FindFirstObjectByType<PerformanceDemoRunner>();
        if (runner == null) throw new InvalidOperationException("Existing demo runner is missing.");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the modified demo before upgrading.");
        var fire = Load<PotionData>("Assets/07.Datas/PotionData/FirePotion.asset");
        var poison = Load<PotionData>("Assets/07.Datas/PotionData/PoisonPotion.asset");
        var ranged = Load<GameObject>("Assets/03.Prefabs/Enemy/RangedMonster.prefab");
        var bomb = Load<GameObject>("Assets/03.Prefabs/Enemy/BombMonster.prefab");
        if (ranged.GetComponent<WaveEnemyHandle>() == null || bomb.GetComponent<WaveEnemyHandle>() == null)
            throw new InvalidOperationException("Roster prefabs require production WaveEnemyHandle components.");
        Undo.RecordObject(runner, "Add performance game-loop scenarios");
        runner.LoopPlayerPrefab = Load<GameObject>("Assets/03.Prefabs/Player/Player.prefab");
        var scenarios = runner.Scenarios.ToList();
        void Add(string name, PerformanceDemoKind kind, Action<PerformanceDemoScenario> configure)
        {
            string path = DataPath + "/" + name + ".asset";
            var scenario = AssetDatabase.LoadAssetAtPath<PerformanceDemoScenario>(path);
            if (scenario == null)
            {
                scenario = ScriptableObject.CreateInstance<PerformanceDemoScenario>();
                scenario.name = name; scenario.Id = name.Split('_')[0]; scenario.Kind = kind;
                scenario.Potion = fire; scenario.SecondPotion = poison;
                scenario.EnemyPrefabs = new[] { ranged }; scenario.MovePlayer = false;
                scenario.MinimumPlayerDistance = 1; scenario.ShowDemoUI = false;
                scenario.WarmupSeconds = 2; scenario.MeasureSeconds = 7;
                scenario.CombinationPairs = kind == PerformanceDemoKind.Combine ? 1 : 0;
                scenario.DataChanges = "Original potion and enemy data. Fixed arena; production AI/physics/damage. " +
                    "Combinations use independent fire/poison pairs, no recombination. Stage/room progression and game HUD are not included. " +
                    "Diagnostic direct projectile launches in S4/S7; S8/S9 use PlayerController replay input and finite inventory.";
                configure(scenario);
                AssetDatabase.CreateAsset(scenario, path);
            }
            if (!scenarios.Contains(scenario)) scenarios.Add(scenario);
        }
        Add("S0_Baseline_UI_Off", PerformanceDemoKind.Baseline, s => { s.MeasureSeconds = 20; });
        Add("S4_Combine_Pair", PerformanceDemoKind.Combine, s => { });
        Add("S4_Combine_NoOverlap", PerformanceDemoKind.Combine, s => { s.NonOverlappingControl = true; });
        foreach (int count in new[] { 5, 10 })
        {
            Add($"S4_Combine_Burst_{count:00}", PerformanceDemoKind.Combine, s => { s.CombinationPairs = count; });
            Add($"S4_Combine_Staggered_{count:00}", PerformanceDemoKind.Combine, s => { s.CombinationPairs = count; s.PairInterval = .3f; s.MeasureSeconds = 9; });
        }
        foreach (var arena in new[] { PerformanceDemoArena.SimpleWall, PerformanceDemoArena.ComplexCorner })
            Add("S4_Combine_" + arena, PerformanceDemoKind.Combine, s => { s.Arena = arena; });
        foreach (int count in new[] { 10, 30, 60 })
        {
            foreach (bool moving in new[] { false, true })
            {
                Add($"S6_Chase_{(moving ? "Moving" : "Static")}_{count:00}", PerformanceDemoKind.Chase, s =>
            {
                s.EnemyCount = count; s.MovePlayer = moving; s.MeasureSeconds = 15; s.DiagnosticHighHealth = true;
                s.DataChanges += " Diagnostic player HP=100000 (not representative combat); enemy AI and attacks remain enabled.";
            });
            }
        }

        Add("S6_Chase_Moving_Dense_30", PerformanceDemoKind.Chase, s => { s.EnemyCount = 30; s.MovePlayer = true; s.DenseSpawn = true; s.DiagnosticHighHealth = true; s.MeasureSeconds = 15; s.DataChanges += " Diagnostic player HP=100000."; });
        foreach (int count in new[] { 1, 10, 30 })
            Add($"S7_Area_ContactEffects_{count:00}", PerformanceDemoKind.ContactEffects, s => { s.EnemyCount = count; s.DiagnosticHighHealth = true; s.DataChanges += " Diagnostic player HP=100000."; });
        Add("S7_Combine_ContactEffects_10", PerformanceDemoKind.ContactEffects, s => { s.EnemyCount = 10; s.CombinationPairs = 1; s.DiagnosticHighHealth = true; s.DataChanges += " Diagnostic player HP=100000."; });
        Add("S8_Combat_Minimal", PerformanceDemoKind.Combat, s =>
        { s.EnemyCount = 3; s.WaveCount = 2; s.MovePlayer = true; s.MeasureSeconds = 45; s.EnemyPrefabs = new[] { ranged }; });
        Add("S8_Combat_Representative", PerformanceDemoKind.Combat, s =>
        { s.EnemyCount = 10; s.WaveCount = 3; s.MovePlayer = true; s.MeasureSeconds = 90; s.AllowPlayerDeath = true; s.EnemyPrefabs = new[] { bomb, ranged }; s.DataChanges += " Representative load is provisional; defeat after required interactions is a distinct allowed outcome, not wave completion."; });
        Add("S8_Combat_Peak", PerformanceDemoKind.Combat, s =>
        { s.EnemyCount = 30; s.WaveCount = 2; s.MovePlayer = true; s.DenseSpawn = true; s.MeasureSeconds = 90; s.AllowPlayerDeath = true; s.EnemyPrefabs = new[] { bomb, ranged }; s.DataChanges += " Peak load is provisional; defeat after full initial spawn and required interactions is a distinct allowed outcome, not wave completion."; });
        Add("S9_Combat_Lifecycle", PerformanceDemoKind.Lifecycle, s =>
        { s.EnemyCount = 3; s.WaveCount = 2; s.MovePlayer = true; s.MeasureSeconds = 45; s.LifecycleCycles = 10; s.Repetitions = 1; s.EnemyPrefabs = new[] { ranged }; });
        runner.Scenarios = scenarios.ToArray();
        EditorUtility.SetDirty(runner); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Game-loop scenario upgrade completed. Total scenarios=" + runner.Scenarios.Length);
    }

    [MenuItem("Tools/Performance/Create Demo Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }
        Create();
    }
    // Batch entry point: does not replace an existing scene or modify the normal build list.
    public static void CreateBatch()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) Create();
        VerifyScene();
    }
    private static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/01.Scenes/Optimization"))
            AssetDatabase.CreateFolder("Assets/01.Scenes", "Optimization");
        if (!AssetDatabase.IsValidFolder(DataPath)) AssetDatabase.CreateFolder("Assets", "PerformanceDemo");
        GameObject room = Load<GameObject>("Assets/03.Prefabs/Map/NormalRooms/NormalRoom_1.prefab");
        Tilemap ground = room.GetComponentsInChildren<Tilemap>(true).First(map => map.name == "Tilemap_Ground");
        Tilemap wall = room.GetComponentsInChildren<Tilemap>(true).First(map => map.name == "Tilemap_Wall");
        PotionData fire = Load<PotionData>("Assets/07.Datas/PotionData/FirePotion.asset");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var system = new GameObject("DemoSystem");
        var runner = system.AddComponent<PerformanceDemoRunner>();
        var environment = new GameObject("EnvironmentRoot");
        runner.Arenas = new GameObject[4];
        for (int i = 0; i < 4; i++)
            runner.Arenas[i] = Arena((PerformanceDemoArena)i, environment.transform, ground, wall);

        var services = new GameObject("ServicesRoot");
        // SoundManager calls DontDestroyOnLoad in Awake, so it must be a scene root.
        PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/03.Prefabs/Manager/SoundManager.prefab"));
        new GameObject("EnemyPathfinder").AddComponent<EnemyPathfinder>().transform.SetParent(services.transform);
        GameObject catalogObject = new("PotionRunCatalog");
        catalogObject.transform.SetParent(services.transform);
        var catalog = catalogObject.AddComponent<PotionRunCatalog>();
        var catalogProperties = new SerializedObject(catalog);
        var database = Load<PotionDatabase>("Assets/07.Datas/PotionData/PotionDatabase.asset");
        catalogProperties.FindProperty("_database").objectReferenceValue = database;
        SerializedProperty sprites = catalogProperties.FindProperty("_potionSprites");
        sprites.arraySize = database.Count;
        for (int i = 0; i < database.Count; i++)
            sprites.GetArrayElementAtIndex(i).objectReferenceValue = database.Potions[i].VisualData.IconSprite;
        catalogProperties.ApplyModifiedPropertiesWithoutUndo();
        runner.Catalog = catalog;
        runner.PlayerPrefab = PlayerCopy();

        GameObject cameraRoot = new("CameraRoot");
        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        cameraObject.transform.SetParent(cameraRoot.transform);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 19;
        camera.backgroundColor = new Color(0.035f, 0.025f, 0.04f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.GetComponent<UniversalAdditionalCameraData>().SetRenderer(-1);
        GameObject lightObject = new("Global Light 2D", typeof(Light2D));
        lightObject.transform.SetParent(cameraRoot.transform);
        Light2D light = lightObject.GetComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1;

        var scenarios = new List<PerformanceDemoScenario>();
        scenarios.Add(Scenario("S0_Baseline", PerformanceDemoKind.Baseline, PerformanceDemoArena.OpenFloor, fire));
        foreach (int count in new[] { 1, 5, 10, 20 })
        {
            var scenario = Scenario($"S1_Areas_{count:00}", PerformanceDemoKind.Areas, PerformanceDemoArena.OpenFloor, fire);
            scenario.Count = count;
            scenario.MeasureSeconds = Mathf.Min(20, fire.FloorDuration - 0.5f);
            EditorUtility.SetDirty(scenario);
            scenarios.Add(scenario);
        }
        for (int i = 0; i < 3; i++)
        {
            var scenario = Scenario("S3_Throw_" + (PerformanceDemoArena)i, PerformanceDemoKind.Throw, (PerformanceDemoArena)i, fire);
            scenario.MeasureSeconds = 3;
            EditorUtility.SetDirty(scenario);
            scenarios.Add(scenario);
        }
        foreach (PerformanceDemoKind kind in new[] { PerformanceDemoKind.Dash, PerformanceDemoKind.Steal, PerformanceDemoKind.Summon })
        {
            var scenario = Scenario("S5_Reaper_" + kind, kind, PerformanceDemoArena.ReaperArena, fire);
            scenario.ReaperPrefab = ReaperCopy(kind);
            scenario.DataChanges = "ReaperData copy: only target ability enabled. Other timing, damage, visuals unchanged. " +
                "PlayerData copy: initial/max HP=100000; player input locked. Fixed camera, no shake controller. " +
                "Steal: one potion prepared, first successful theft only; further steals blocked by held potion.";
            EditorUtility.SetDirty(scenario);
            scenarios.Add(scenario);
        }
        runner.Scenarios = scenarios.ToArray();
        var driver = system.GetComponent<PerformanceDemoPotionDriver>();
        driver.AreaPrefab = Load<GameObject>("Assets/03.Prefabs/PotionGroundArea.prefab").GetComponent<PotionAreaController>();
        driver.ProjectilePrefab = Load<GameObject>("Assets/03.Prefabs/Potion.prefab").GetComponent<PotionProjectile>();
        new GameObject("DemoUI").AddComponent<PerformanceDemoView>().Runner = runner;
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("PerformanceDemoScene created: " + ScenePath);
    }
    private static GameObject Arena(PerformanceDemoArena arena, Transform parent, Tilemap groundTemplate, Tilemap wallTemplate)
    {
        var root = new GameObject(arena.ToString(), typeof(Grid));
        root.transform.SetParent(parent);
        var ground = Object.Instantiate(groundTemplate.gameObject, root.transform).GetComponent<Tilemap>();
        ground.name = "Tilemap_Ground";
        ground.gameObject.layer = 0;
        ground.ClearAllTiles();
        ground.transform.localPosition = Vector3.zero;
        // Ground is visual only; preserve the source renderer/material and sorting.
        foreach (Collider2D collider in ground.GetComponents<Collider2D>()) Object.DestroyImmediate(collider);
        foreach (Rigidbody2D body in ground.GetComponents<Rigidbody2D>()) Object.DestroyImmediate(body);
        TileBase groundTile = MostUsed(groundTemplate);
        for (int x = -26; x < 26; x++) for (int y = -16; y < 16; y++) ground.SetTile(new Vector3Int(x, y, 0), groundTile);
        ground.CompressBounds();
        var wall = Object.Instantiate(wallTemplate.gameObject, root.transform).GetComponent<Tilemap>();
        wall.name = "Tilemap_Wall";
        wall.transform.localPosition = Vector3.zero;
        wall.gameObject.layer = LayerMask.NameToLayer("Wall");
        wall.ClearAllTiles();
        TileBase wallTile = MostUsed(wallTemplate);
        if (arena == PerformanceDemoArena.SimpleWall || arena == PerformanceDemoArena.ComplexCorner)
        {
            for (int y = -5; y <= 5; y++) wall.SetTile(new Vector3Int(1, y, 0), wallTile);
            if (arena == PerformanceDemoArena.ComplexCorner)
            {
                for (int x = -1; x <= 5; x++) wall.SetTile(new Vector3Int(x, 2, 0), wallTile);
                for (int y = 2; y <= 5; y++) wall.SetTile(new Vector3Int(-1, y, 0), wallTile);
            }
        }
        wall.CompressBounds();
        root.SetActive(arena == PerformanceDemoArena.OpenFloor);
        return root;
    }
    private static TileBase MostUsed(Tilemap template)
    {
        var counts = new Dictionary<TileBase, int>();
        foreach (Vector3Int position in template.cellBounds.allPositionsWithin)
        {
            TileBase tile = template.GetTile(position);
            if (tile != null) { counts.TryGetValue(tile, out int count); counts[tile] = count + 1; }
        }
        if (counts.Count == 0) throw new InvalidOperationException("No tile in " + template.name);
        return counts.OrderByDescending(pair => pair.Value).First().Key;
    }
    private static PerformanceDemoScenario Scenario(string name, PerformanceDemoKind kind, PerformanceDemoArena arena, PotionData potion)
    {
        var scenario = ScriptableObject.CreateInstance<PerformanceDemoScenario>();
        scenario.Id = name.Split('_')[0];
        scenario.Kind = kind;
        scenario.Arena = arena;
        scenario.Potion = potion;
        scenario.DataChanges = "Original potion lifetime/radius/effects. S1 creates after warmup; no regeneration. Audio enabled. No renderer comparison in this version.";
        AssetDatabase.CreateAsset(scenario, AssetDatabase.GenerateUniqueAssetPath(DataPath + "/" + name + ".asset"));
        return scenario;
    }
    private static GameObject PlayerCopy()
    {
        var source = Load<GameObject>("Assets/03.Prefabs/Player/Player.prefab");
        var data = Object.Instantiate(source.GetComponent<PlayerStatus>().Data);
        var properties = new SerializedObject(data);
        properties.FindProperty("_health._maxHealth").floatValue = 100000;
        properties.FindProperty("_health._initialHealth").floatValue = 100000;
        properties.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(DataPath + "/DemoPlayerData.asset"));
        GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(source);
        var status = new SerializedObject(copy.GetComponent<PlayerStatus>());
        status.FindProperty("_data").objectReferenceValue = data;
        status.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copy, AssetDatabase.GenerateUniqueAssetPath(DataPath + "/DemoPlayer.prefab"));
        Object.DestroyImmediate(copy);
        return prefab;
    }
    private static GameObject ReaperCopy(PerformanceDemoKind kind)
    {
        string sourceName = kind == PerformanceDemoKind.Dash ? "ReaperNormal" : "ReaperRanged";
        var source = Load<GameObject>("Assets/03.Prefabs/Enemy/" + sourceName + ".prefab");
        var original = new SerializedObject(source.GetComponent<ReaperController>());
        var data = Object.Instantiate((ReaperData)original.FindProperty("_data").objectReferenceValue);
        var properties = new SerializedObject(data);
        properties.FindProperty("_useMeleeAttack").boolValue = false;
        properties.FindProperty("_useDashAttack").boolValue = kind == PerformanceDemoKind.Dash;
        properties.FindProperty("_usePotionSteal").boolValue = kind == PerformanceDemoKind.Steal;
        properties.FindProperty("_usePotionSummon").boolValue = kind == PerformanceDemoKind.Summon;
        properties.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(DataPath + "/Reaper" + kind + "Data.asset"));
        GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(source);
        var controller = new SerializedObject(copy.GetComponent<ReaperController>());
        controller.FindProperty("_data").objectReferenceValue = data;
        controller.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(copy, AssetDatabase.GenerateUniqueAssetPath(DataPath + "/Reaper" + kind + ".prefab"));
        Object.DestroyImmediate(copy);
        return prefab;
    }
    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ??
        throw new InvalidOperationException("Missing asset: " + path);
    public static void VerifyScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        PerformanceDemoRunner runner = Object.FindFirstObjectByType<PerformanceDemoRunner>();
        if (runner == null || runner.Scenarios.Length < 11 || runner.Arenas.Length != 4 || runner.Catalog == null)
            throw new InvalidOperationException("Serialized demo references are incomplete.");
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                    throw new InvalidOperationException("Missing script: " + child.name);
            }
        }

        Debug.Log("PerformanceDemoScene reload/reference verification passed.");
    }
    public static void FixServiceRootBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        var sound = Object.FindFirstObjectByType<SoundManager>();
        if (sound != null && sound.transform.parent != null)
        {
            sound.transform.SetParent(null);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        VerifyScene();
    }
}
