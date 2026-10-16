using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
[McpForUnityTool("performance_demo", Description = "Run the PerformanceDemo scene and automatically analyze its binary Profiler captures. " +
    "Actions: start, status, stop, analyze. Start returns a job immediately; poll status until Completed/Failed/Stopped. " +
    "Use an exact scenario asset name, e.g. S1_Areas_20, or all. Keeps original scenario assets unchanged.", Group = "core")]
public static class PerformanceDemoMcpTool
{
    public sealed class Parameters
    {
        [ToolParameter("start, status, stop, or analyze", Required = false, DefaultValue = "status")]
        public string action { get; set; }
        [ToolParameter("Exact scenario asset name, or all", Required = false, DefaultValue = "S1_Areas_20")]
        public string scenario { get; set; }
        [ToolParameter("0 keeps authored repetitions; otherwise 1..20 on in-memory copies", Required = false, DefaultValue = "0")]
        public int repetitions { get; set; }
        [ToolParameter("Automatically analyze after a successful run", Required = false, DefaultValue = "true")]
        public bool analyze { get; set; }
        [ToolParameter("Record binary Profiler captures; false is functional validation only and requires analyze=false", Required = false, DefaultValue = "true")]
        public bool capture_profiler { get; set; }
        [ToolParameter("Existing result directory for action=analyze; otherwise latest job directory", Required = false)]
        public string run_directory { get; set; }
    }
    [Serializable]
    public sealed class Job
    {
        public string id, phase, scenario, startedUtc, failure, runnerPhase, runStatus, runDirectory;
        public string analysisDirectory, reportPath, summaryPath;
        public int repetitions, analysisProcessId, validCaptures, invalidCaptures;
        public bool analyze, captureProfiler, startedPlayMode, enteredPlayMode, exitedPlayMode, backgroundChanged, originalRunInBackground;
    }
    const string SessionKey = "PerformanceDemo.McpJob";
    static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static Job _job;
    static PerformanceDemoRunner _runner;
    static PerformanceDemoScenario[] _originalScenarios, _copies;
    static bool _originalCaptureProfiler;
    static double _nextTick;
    static PerformanceDemoMcpTool()
    {
        string saved = SessionState.GetString(SessionKey, "");
        if (!string.IsNullOrEmpty(saved)) _job = JsonUtility.FromJson<Job>(saved);
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && _job != null && _job.phase == "EnteringPlayMode")
            { _job.enteredPlayMode = true; Save(); }
            if (state == PlayModeStateChange.EnteredEditMode && _job != null && Busy)
            { _job.exitedPlayMode = true; Save(); }
        };
    }
    static bool Busy => _job != null && _job.phase != "Completed" && _job.phase != "Failed" && _job.phase != "Stopped";
    public static object HandleCommand(JObject parameters)
    {
        try
        {
            string action = parameters?.Value<string>("action") ?? "status";
            switch (action.ToLowerInvariant())
            {
                case "upgrade_scenarios":
                    if (Busy) throw new InvalidOperationException("A profiling job is active.");
                    PerformanceDemoSceneBuilder.AddGameLoopScenarios();
                    return new SuccessResponse("Game-loop scenarios added to the existing scene.");
                case "status":
                    return new SuccessResponse("Performance demo status.", Snapshot());
                case "start":
                    if (Busy) throw new InvalidOperationException("A profiling job is already active. Poll status or stop it.");
                    Start(parameters ?? new JObject());
                    return new SuccessResponse("Job accepted; poll performance_demo(action=status).", Snapshot());
                case "stop":
                    Stop();
                    return new SuccessResponse("Stop requested.", Snapshot());
                case "analyze":
                    if (Busy) throw new InvalidOperationException("Another profiling job is active.");
                    string directory = parameters?.Value<string>("run_directory") ?? _job?.runDirectory;
                    if (string.IsNullOrEmpty(directory) || !File.Exists(Path.Combine(directory, "run.json")))
                        throw new ArgumentException("run_directory must contain run.json.");
                    _job = NewJob("analysis", 0, true);
                    _job.runDirectory = Path.GetFullPath(directory);
                    BeginAnalysis();
                    return new SuccessResponse("Analysis started; poll status.", Snapshot());
                default: return new ErrorResponse("Unknown action. Use start, status, stop, analyze.");
            }
        }
        catch (Exception exception)
        {
            if (_job != null && _job.phase == "Preparing") { _job.failure = exception.Message; _job.phase = "Failed"; Save(); }
            return new ErrorResponse(exception.Message);
        }
    }
    static Job NewJob(string scenario, int repetitions, bool analyze) => new Job
    {
        id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8),
        phase = "Preparing",
        scenario = scenario,
        repetitions = repetitions,
        analyze = analyze,
        startedUtc = DateTime.UtcNow.ToString("O")
    };
    static void Start(JObject parameters)
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying)
            throw new InvalidOperationException("Unity is compiling, importing, or changing Play mode. Retry when ready.");
        if (Application.platform != RuntimePlatform.WindowsEditor) throw new NotSupportedException("The analysis launcher currently supports Windows Editor.");
        string scenario = parameters.Value<string>("scenario") ?? "S1_Areas_20";
        int repetitions = parameters.Value<int?>("repetitions") ?? 0;
        bool captureProfiler = parameters.Value<bool?>("capture_profiler") ?? true;
        bool analyze = parameters.Value<bool?>("analyze") ?? true;
        if (!captureProfiler && analyze) throw new ArgumentException("capture_profiler=false requires analyze=false.");
        if (repetitions < 0 || repetitions > 20) throw new ArgumentException("repetitions must be 0..20.");
        if (EditorApplication.isPaused) throw new InvalidOperationException("Unity is paused. Resume it before profiling.");
        if (EditorApplication.isPlaying)
        {
            _runner = Object.FindFirstObjectByType<PerformanceDemoRunner>();
            if (_runner == null || _runner.IsRunning) throw new InvalidOperationException("Play mode must contain an idle PerformanceDemoRunner.");
            SelectScenario(_runner, scenario); // Validate before accepting a job.
        }
        else
        {
            string scene = PerformanceDemoSceneBuilder.ScenePath;
            if (!File.Exists(scene)) throw new FileNotFoundException("Demo scene missing: " + scene);
            bool alreadyOpen = SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().path == scene;
            if (!alreadyOpen)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the currently modified scene before switching to the demo.");
                EditorSceneManager.OpenScene(scene);
            }
            var authored = Object.FindFirstObjectByType<PerformanceDemoRunner>();
            if (authored == null) throw new InvalidOperationException("Demo scene has no runner.");
            SelectScenario(authored, scenario);
            if (authored.AutoStart) throw new InvalidOperationException("Disable runner AutoStart before MCP-controlled runs.");
        }
        _job = NewJob(scenario, repetitions, parameters.Value<bool?>("analyze") ?? true);
        _job.captureProfiler = captureProfiler;
        _job.startedPlayMode = !EditorApplication.isPlaying;
        _job.enteredPlayMode = EditorApplication.isPlaying;
        _job.phase = "EnteringPlayMode";
        Save();
        if (_job.startedPlayMode) EditorApplication.EnterPlaymode();
    }
    static int SelectScenario(PerformanceDemoRunner runner, string name)
    {
        if (name == "all") return -1;
        int index = Array.FindIndex(runner.Scenarios, s => s != null && s.name == name);
        if (index < 0) throw new ArgumentException("Unknown scenario. Available: " + string.Join(", ", runner.Scenarios.Where(s => s != null).Select(s => s.name)));
        return index;
    }
    static object Snapshot()
    {
        if (_job == null) return new { phase = "Idle", project = Root };
        if (_runner != null && _runner.IsRunning)
        {
            _job.runnerPhase = _runner.Phase;
            _job.runDirectory = _runner.OutputPath;
        }
        return _job;
    }
    static void Tick()
    {
        if (!Busy || EditorApplication.timeSinceStartup < _nextTick) return;
        _nextTick = EditorApplication.timeSinceStartup + .25;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try
        {
            if (_job.phase != "Failing" && _job.phase != "LeavingPlayMode" &&
                DateTime.UtcNow - DateTime.Parse(_job.startedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind) > TimeSpan.FromHours(1))
            {
                throw new TimeoutException("Profiling job exceeded one hour.");
            }

            if (_job.phase == "EnteringPlayMode")
            {
                if (!_job.enteredPlayMode || !EditorApplication.isPlaying) return;
                _runner = Object.FindFirstObjectByType<PerformanceDemoRunner>();
                if (_runner == null) throw new InvalidOperationException("Runner missing after entering Play mode.");
                if (_runner.IsRunning) throw new InvalidOperationException("Runner was started by another caller.");
                int index = SelectScenario(_runner, _job.scenario);
                _originalScenarios = _runner.Scenarios;
                _originalCaptureProfiler = _runner.CaptureProfiler;
                _job.originalRunInBackground = Application.runInBackground;
                _job.backgroundChanged = true;
                Application.runInBackground = true;
                _copies = _runner.Scenarios.Select(source =>
                {
                    var copy = Object.Instantiate(source);
                    copy.name = source.name;
                    if (_job.repetitions > 0) copy.Repetitions = _job.repetitions;
                    return copy;
                }).ToArray();
                _runner.Scenarios = _copies;
                _runner.CaptureProfiler = _job.captureProfiler;
                _job.phase = "Running";
                Save();
                if (index < 0) _runner.RunAll(); else _runner.RunOne(index);
            }
            else if (_job.phase == "Running" || _job.phase == "Stopping")
            {
                if (!EditorApplication.isPlaying)
                {
                    RecoverInterruptedRun();
                    RestoreSettings();
                    _job.phase = "LeavingPlayMode";
                    Save();
                    return;
                }
                if (_runner == null) _runner = Object.FindFirstObjectByType<PerformanceDemoRunner>();
                if (_runner == null) throw new InvalidOperationException("Runner disappeared or Play mode was interrupted.");
                _job.runnerPhase = _runner.Phase;
                _job.runDirectory = _runner.OutputPath;
                if (_runner.IsRunning) return;
                _job.runStatus = _runner.Phase;
                _job.failure = _runner.Failure;
                if (_job.runStatus != "Completed" && _job.runStatus != "Stopped" && _job.runStatus != "Failed")
                    throw new InvalidOperationException("Runner lost its execution state; rerun the job.");
                RestoreSettings();
                _job.phase = "LeavingPlayMode";
                Save();
                if (_job.startedPlayMode && EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
            else if (_job.phase == "LeavingPlayMode")
            {
                if (_job.startedPlayMode && ((_job.enteredPlayMode && !_job.exitedPlayMode) || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)) return;
                RestoreBackground();
                if (_job.runStatus == "Completed" && _job.analyze)
                {
                    BeginAnalysis();
                }
                else { _job.phase = _job.runStatus; Save(); }
            }
            else if (_job.phase == "Analyzing")
            {
                PollAnalysis();
            }
            else if (_job.phase == "Failing")
            {
                if (_runner != null && _runner.IsRunning) return;
                RestoreSettings();
                _job.runStatus = "Failed";
                _job.phase = "LeavingPlayMode";
                Save();
                if (_job.startedPlayMode && EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception exception)
        {
            if (_runner != null && _runner.IsRunning) _runner.Stop();
            _job.failure = exception.Message;
            _job.phase = "Failing";
            Save();
        }
    }
    static void BeginAnalysis()
    {
        string script = Path.Combine(Root, "Tools/Profiling/Analyze-Profiler.ps1");
        if (!File.Exists(script)) throw new FileNotFoundException("Analyzer launcher missing.", script);
        if (!File.Exists(Path.Combine(_job.runDirectory, "run.json"))) throw new FileNotFoundException("Run did not save run.json.");
        _job.analysisDirectory = Path.Combine(Root, "Logs/ProfilerReports/mcp_" + _job.id);
        _job.reportPath = Path.Combine(_job.analysisDirectory, "report.md");
        _job.summaryPath = Path.Combine(_job.analysisDirectory, "summary.json");
        var info = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell/v1.0/powershell.exe"),
            Arguments = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(script) + " -CapturePath " + Quote(_job.runDirectory) + " -OutputDirectory " + Quote(_job.analysisDirectory),
            WorkingDirectory = Root,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        using (var process = Process.Start(info)) _job.analysisProcessId = process.Id;
        _job.phase = "Analyzing";
        Save();
    }
    static string Quote(string path)
    {
        if (path.Contains("\"")) throw new ArgumentException("Invalid quote in path.");
        return "\"" + path.TrimEnd('\\') + "\"";
    }
    static void RestoreSettings()
    {
        if (!_job.startedPlayMode) RestoreBackground();
        if (_runner != null && _originalScenarios != null)
        {
            _runner.Scenarios = _originalScenarios;
            _runner.CaptureProfiler = _originalCaptureProfiler;
        }
        if (_copies != null) foreach (var copy in _copies) if (copy != null) Object.Destroy(copy);
        _copies = _originalScenarios = null;
    }
    static void RestoreBackground()
    {
        if (_job.backgroundChanged) Application.runInBackground = _job.originalRunInBackground;
        _job.backgroundChanged = false;
    }
    static void RecoverInterruptedRun()
    {
        string path = Path.Combine(_job.runDirectory ?? "", "run.json");
        if (File.Exists(path))
        {
            var run = JObject.Parse(File.ReadAllText(path));
            _job.runStatus = run.Value<string>("result") ?? "Stopped";
            _job.failure = run.Value<string>("failure");
        }
        else { _job.runStatus = "Failed"; _job.failure = "Play mode ended before run.json was saved."; }
    }
    static void PollAnalysis()
    {
        if (File.Exists(_job.summaryPath))
        {
            JObject summary;
            try { summary = JObject.Parse(File.ReadAllText(_job.summaryPath)); }
            catch (IOException) { return; }
            catch (Newtonsoft.Json.JsonException) { return; }
            _job.validCaptures = summary.Value<int>("validCaptures");
            _job.invalidCaptures = summary.Value<int>("invalidCaptures");
            _job.phase = _job.invalidCaptures == 0 ? "Completed" : "Failed";
            if (_job.invalidCaptures > 0) _job.failure = "Analysis rejected " + _job.invalidCaptures + " capture(s). See report.md.";
            Save();
            return;
        }
        try
        {
            using (var process = Process.GetProcessById(_job.analysisProcessId))
                if (!process.HasExited) return;
        }
        catch (ArgumentException) { }
        throw new InvalidOperationException("Analysis exited without summary.json. See " + Path.Combine(_job.analysisDirectory, "unity.log"));
    }
    static void Stop()
    {
        if (!Busy) return;
        if (_job.phase == "Analyzing") throw new InvalidOperationException("Analysis is already running; poll status until it finishes.");
        if (_runner != null && _runner.IsRunning) { _runner.Stop(); _job.phase = "Stopping"; }
        else { _job.runStatus = "Stopped"; _job.phase = "LeavingPlayMode"; if (_job.startedPlayMode) EditorApplication.ExitPlaymode(); }
        Save();
    }
    static void Save()
    {
        string json = JsonUtility.ToJson(_job, true);
        SessionState.SetString(SessionKey, json);
        string directory = Path.Combine(Root, "Logs/PerformanceMcp");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "job.json"), json);
    }
}
