using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Run with -batchmode -executeMethod PerformanceDemoSmokeCheck.RunBatch (without -quit).
// Copies settings in memory; authored scenario assets are never shortened or saved.
public static class PerformanceDemoSmokeCheck
{
    private const string Key = "PerformanceDemo.Smoke";
    private static bool _started;
    private static double _deadline;
    private static int _stage;
    private static int _initialScenes;

    public static void RunBatch()
    {
        PerformanceDemoSceneBuilder.CreateBatch();
        PerformanceDemoSceneBuilder.FixServiceRootBatch();
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool(Key, false)) return;
        _deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup > _deadline) { Finish(1, "Timeout."); return; }
        if (!EditorApplication.isPlaying) return;
        var runner = Object.FindFirstObjectByType<PerformanceDemoRunner>();
        if (runner == null) { Finish(1, "Runner missing."); return; }
        if (!_started)
        {
            _started = true;
            _initialScenes = SceneManager.sceneCount;
            // This short batch check remains a regression for the original
            // eleven scenarios. Game-loop workloads have their own validation.
            runner.Scenarios = Array.FindAll(runner.Scenarios, s => !s.IsGameLoop && s.name != "S0_Baseline_UI_Off");
            for (int i = 0; i < runner.Scenarios.Length; i++)
            {
                var copy = Object.Instantiate(runner.Scenarios[i]);
                copy.Repetitions = 1;
                copy.WarmupSeconds = 0.2f;
                copy.MeasureSeconds = copy.Kind == PerformanceDemoKind.Baseline ? 0.3f :
                    copy.Kind == PerformanceDemoKind.Areas ? 0.6f : copy.Kind == PerformanceDemoKind.Throw ? 2 : 10;
                runner.Scenarios[i] = copy;
            }
            runner.RunAll();
            return;
        }
        if (_stage == 1 && runner.IsRunning) { runner.Stop(); _stage = 2; return; }
        if (runner.IsRunning) return;
        if (SceneManager.sceneCount != _initialScenes) { Finish(1, "Residual runtime scene."); return; }
        if (_stage == 0)
        {
            if (runner.Phase != "Completed") { Finish(1, runner.Failure); return; }
            Debug.Log("PerformanceDemo all 11 scenarios passed: " + runner.OutputPath);
            runner.RunOne(0);
            _stage = 1;
        }
        else if (_stage == 2)
        {
            if (runner.Phase != "Stopped") { Finish(1, "Stop path: " + runner.Phase); return; }
            runner.Scenarios[1].Count = 0;
            runner.RunOne(1);
            _stage = 3;
        }
        else if (_stage == 3)
        {
            if (runner.Phase != "Failed") { Finish(1, "Invalid settings accepted."); return; }
            runner.RunOne(0);
            _stage = 4;
        }
        else if (_stage == 4)
        {
            Finish(runner.Phase == "Completed" ? 0 : 1,
            "All scenarios, stop, invalid settings, and restart: " + runner.Phase + "; " + runner.Failure);
        }
    }
    private static void Finish(int code, string message)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Debug.Log("PerformanceDemo smoke result: " + message);
        EditorApplication.Exit(code);
    }
}
