using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Runs the exact saved scenarios in a Development Build, without editor-only MCP.
public sealed class OptimizationPlayerBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Application.isEditor) return;
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "--optimization-scenarios");
        if (index < 0 || index + 1 >= args.Length) return;
        var bootstrap = new GameObject("OptimizationBuildRunner").AddComponent<OptimizationPlayerBootstrap>();
        DontDestroyOnLoad(bootstrap.gameObject);
        bootstrap.StartCoroutine(bootstrap.Run(args[index + 1].Split(',')));
    }

    private IEnumerator Run(string[] names)
    {
        yield return null;
        string[] args = Environment.GetCommandLineArgs();
        int repetitions = ReadPositiveInt(args, "--optimization-repetitions", 3);
        int frameRate = ReadPositiveInt(args, "--optimization-frame-rate", 120);
        var runner = FindFirstObjectByType<PerformanceDemoRunner>();
        if (runner == null) { Debug.LogError("Optimization build runner missing."); Application.Quit(2); yield break; }
        var selected = new List<PerformanceDemoScenario>();
        foreach (string name in names)
        {
            var source = Array.Find(runner.Scenarios, s => s != null && s.name == name);
            if (source == null) { Debug.LogError("Unknown optimization scenario: " + name); Application.Quit(2); yield break; }
            var copy = Instantiate(source);
            copy.name = source.name;
            copy.Repetitions = Mathf.Clamp(repetitions, 1, 20);
            selected.Add(copy);
        }
        runner.Scenarios = selected.ToArray();
        runner.CaptureProfiler = true;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = frameRate;
        runner.RunAll();
        while (runner.IsRunning) yield return null;
        int resultIndex = Array.IndexOf(args, "--optimization-result");
        string path = resultIndex >= 0 && resultIndex + 1 < args.Length ? args[resultIndex + 1]
            : Path.Combine(Application.dataPath, "..", "optimization-last-run.json");
        File.WriteAllText(path, JsonUtility.ToJson(new BuildResult
        { status = runner.Phase, failure = runner.Failure, runDirectory = runner.OutputPath }, true));
        Debug.Log("Optimization build result: " + runner.Phase + "; " + runner.OutputPath);
        Application.Quit(runner.Phase == "Completed" ? 0 : 2);
    }
    private static int ReadPositiveInt(string[] args, string flag, int fallback)
    {
        int index = Array.IndexOf(args, flag);
        return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out int value) && value > 0
            ? value : fallback;
    }
    [Serializable] private sealed class BuildResult { public string status, failure, runDirectory; }
}
