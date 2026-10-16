using UnityEngine;

public sealed class PerformanceDemoView : MonoBehaviour
{
    public PerformanceDemoRunner Runner;
    private int _selected;
    private Vector2 _scroll;
    private void OnGUI()
    {
        if (Runner == null || Runner.Scenarios == null) return;
        if (!Runner.ShowDemoUI) return;
        GUILayout.BeginArea(new Rect(16, 16, 350, Mathf.Min(Screen.height - 32, 650)), GUI.skin.box);
        GUILayout.Label("PERFORMANCE DEMO");
        GUILayout.Label($"{Runner.CurrentScenario} / {Runner.Phase} / Repeat {Runner.Repetition}");
        GUILayout.Label($"Actual targets: {Runner.ActualCount}");
        GUI.enabled = !Runner.IsRunning;
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(310));
        for (int i = 0; i < Runner.Scenarios.Length; i++)
            if (GUILayout.Toggle(_selected == i, Runner.Scenarios[i].name, "Button")) _selected = i;
        GUILayout.EndScrollView();
        if (GUILayout.Button("Run selected")) Runner.RunOne(_selected);
        if (GUILayout.Button("Run all")) Runner.RunAll();
        GUI.enabled = Runner.IsRunning;
        if (GUILayout.Button("Stop and clean up")) Runner.Stop();
        GUI.enabled = true;
        GUILayout.Label(Runner.CaptureProfiler ? "Profiler: automatic .raw capture per Measure interval" : "Profiler: automatic capture disabled");
        if (!string.IsNullOrEmpty(Runner.Failure)) GUILayout.Label("INVALID: " + Runner.Failure);
        if (!string.IsNullOrEmpty(Runner.OutputPath)) GUILayout.Label("Saved: " + Runner.OutputPath);
        GUILayout.EndArea();
    }
}
