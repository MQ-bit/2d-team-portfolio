using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OptimizationScenarioBuilder
{
    public const string DataPath = "Assets/PerformanceDemo/Optimization";

    [MenuItem("Tools/Performance/Add Optimization Comparisons")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play mode and wait for compilation.");
        if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != PerformanceDemoSceneBuilder.ScenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the modified scene first.");
            EditorSceneManager.OpenScene(PerformanceDemoSceneBuilder.ScenePath);
        }
        var runner = UnityEngine.Object.FindFirstObjectByType<PerformanceDemoRunner>();
        if (runner == null) throw new InvalidOperationException("Demo runner missing.");
        if (!AssetDatabase.IsValidFolder(DataPath)) AssetDatabase.CreateFolder("Assets/PerformanceDemo", "Optimization");
        var scenarios = new List<PerformanceDemoScenario>(runner.Scenarios);
        var originals = runner.Scenarios;
        foreach (var source in originals)
        {
            if (source == null || AssetDatabase.GetAssetPath(source).StartsWith(DataPath + "/")) continue;
            if (source.Kind == PerformanceDemoKind.Combine)
            {
                Add(source, OptimizationVariant.SharedMask, false, scenarios);
                Add(source, OptimizationVariant.SharedMask | OptimizationVariant.FastMask, false, scenarios, "FastMask");
            }
            if (source.Kind == PerformanceDemoKind.Chase)
                Add(source, OptimizationVariant.EnemySearch, false, scenarios);
            if (source.Kind == PerformanceDemoKind.ContactEffects)
            {
                Add(source, OptimizationVariant.StatusQuery, false, scenarios);
                Add(source, OptimizationVariant.Timeline, false, scenarios);
                Add(source, OptimizationVariant.Improved, false, scenarios);
                if (source.name == "S7_Area_ContactEffects_30")
                {
                    Add(source, OptimizationVariant.Original, true, scenarios);
                    Add(source, OptimizationVariant.StatusQuery, true, scenarios);
                    Add(source, OptimizationVariant.Timeline, true, scenarios);
                    Add(source, OptimizationVariant.Improved, true, scenarios);
                }
            }
            if (source.Kind == PerformanceDemoKind.Combat || source.Kind == PerformanceDemoKind.Lifecycle)
                Add(source, OptimizationVariant.Improved, false, scenarios);
        }
        runner.Scenarios = scenarios.ToArray();
        EditorUtility.SetDirty(runner);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Optimization comparisons ready. Scenarios: " + scenarios.Count);
    }

    private static void Add(PerformanceDemoScenario source, OptimizationVariant variant,
        bool sustained, List<PerformanceDemoScenario> scenarios, string label = null)
    {
        string name = source.name + (sustained ? "_Sustained" : "") + "_" + (label ?? variant.ToString());
        string path = DataPath + "/" + name + ".asset";
        var scenario = AssetDatabase.LoadAssetAtPath<PerformanceDemoScenario>(path);
        if (scenario == null)
        {
            scenario = UnityEngine.Object.Instantiate(source);
            scenario.name = name;
            scenario.Optimizations = variant;
            scenario.DiagnosticSustainedEnemies = sustained;
            scenario.Repetitions = 3;
            scenario.ShowDemoUI = false;
            scenario.DataChanges += " Comparison variant=" + variant + "." +
                (sustained ? " Diagnostic enemy HP=1000000; compare only with the paired Sustained condition." : "");
            AssetDatabase.CreateAsset(scenario, path);
        }
        if (!scenarios.Contains(scenario)) scenarios.Add(scenario);
    }
}
