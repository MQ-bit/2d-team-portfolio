using System;
using System.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PerformanceDemoRecorder), typeof(PerformanceDemoPotionDriver), typeof(PerformanceDemoReaperDriver))]
public sealed class PerformanceDemoRunner : MonoBehaviour
{
    public PerformanceDemoScenario[] Scenarios;
    public GameObject[] Arenas;
    public GameObject PlayerPrefab;
    public GameObject LoopPlayerPrefab;
    public PotionRunCatalog Catalog;
    public bool AutoStart;
    public bool CaptureProfiler = true;
    // One PNG per scenario, taken after the Profiler log is closed so it never affects the measured frames.
    public bool CaptureScreenshots;
    [Min(0.1f)] public float MaximumFrameGapSeconds = 1f;
    public string Phase { get; private set; } = "Idle";
    public string Failure { get; private set; }
    public string CurrentScenario { get; private set; } = "-";
    public int Repetition { get; private set; }
    public int ActualCount { get; private set; }
    public bool IsRunning { get; private set; }
    public string OutputPath => _recorder != null ? _recorder.OutputPath : null;
    private PerformanceDemoRecorder _recorder;
    private PerformanceDemoPotionDriver _potions;
    private PerformanceDemoReaperDriver _reaper;
    private PerformanceDemoLoopDriver _loop;
    public bool ShowDemoUI => !IsRunning || _scenario == null || _scenario.ShowDemoUI;
    private Scene _runtime;
    private Scene _home;
    private PlayerStatus _player;
    private bool _stop;
    private string _runtimeError;
    private PerformanceDemoScenario _scenario;
    private UnityEngine.Random.State _randomState;
    private float _timeScale;
    private PerformanceDemoCapture _capture;
    private bool _focusLostDuringMeasure;
    private bool _phaseOpen;
    private PerformanceDemoScenario _phaseScenario;
    private int _phaseRepetition;
    private static readonly ProfilerMarker SetupMarker = new("PerfDemo.Setup");
    private static readonly ProfilerMarker CleanupMarker = new("PerfDemo.Cleanup");
    private static readonly ProfilerMarker MeasureMarker = new("PerfDemo.Measure");

    private void Awake()
    {
        _recorder = GetComponent<PerformanceDemoRecorder>();
        _potions = GetComponent<PerformanceDemoPotionDriver>();
        _reaper = GetComponent<PerformanceDemoReaperDriver>();
        _loop = GetComponent<PerformanceDemoLoopDriver>();
        if (_loop == null) _loop = gameObject.AddComponent<PerformanceDemoLoopDriver>();
        _home = gameObject.scene;
    }
    private void Start() { if (AutoStart) RunAll(); }
    public void RunAll() => Begin(Scenarios);
    public void RunOne(int index)
    {
        if (index >= 0 && index < Scenarios.Length) Begin(new[] { Scenarios[index] });
    }
    public void Stop() { if (IsRunning) _stop = true; }
    private void Begin(PerformanceDemoScenario[] scenarios)
    {
        if (IsRunning) return;
        IsRunning = true;
        _stop = false;
        Failure = _runtimeError = null;
        _randomState = UnityEngine.Random.state;
        _timeScale = Time.timeScale;
        _scenario = null;
        Repetition = 0;
        Phase = "Idle";
        _phaseOpen = false;
        try { _recorder.Begin(); }
        catch (Exception exception)
        {
            Failure = "Cannot create result directory: " + exception.Message;
            Phase = "Failed";
            IsRunning = false;
            return;
        }
        _recorder.Current.automaticProfilerCapture = CaptureProfiler;
        _recorder.Current.maximumFrameGapSeconds = MaximumFrameGapSeconds;
        _recorder.Current.scenePath = _home.path;
        Application.logMessageReceived += OnLog;
        StartCoroutine(Execute(scenarios));
    }
    private void OnLog(string condition, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            _runtimeError ??= condition;
    }
    private void SetPhase(string phase)
    {
        if (_phaseOpen) _recorder.Record(_phaseScenario, _phaseRepetition, Phase, "End", ActualCount);
        Phase = phase;
        _recorder.Record(_scenario, Repetition, Phase, "Begin", ActualCount);
        _phaseScenario = _scenario;
        _phaseRepetition = Repetition;
        _phaseOpen = true;
    }
    private IEnumerator Execute(PerformanceDemoScenario[] scenarios)
    {
        if (scenarios == null || scenarios.Length == 0) Failure = "No scenarios configured.";
        if (scenarios != null)
        {
            foreach (PerformanceDemoScenario scenario in scenarios)
            {
                if (Failure != null || _stop) break;
                _scenario = scenario;
                Repetition = 0;
                CurrentScenario = scenario != null ? scenario.Id : "Missing";
                SetPhase("Validate");
                try { Validate(scenario); }
                catch (Exception exception) { Failure = exception.Message; }
                if (Failure != null) break;
                _recorder.Current.settings.Add(JsonUtility.ToJson(scenario));
                int intervals = scenario.Repetitions * (scenario.Kind == PerformanceDemoKind.Lifecycle ? scenario.LifecycleCycles : 1);
                for (int repetition = 1; repetition <= intervals; repetition++)
                {
                    Repetition = repetition;
                    SetPhase("Setup");
                    try
                    {
                        using (SetupMarker.Auto()) { Setup(scenario); StartMeasurement(scenario); }
                        if (scenario.IsGameLoop) _loop.Begin(true);
                    }
                    catch (Exception exception) { Failure = exception.ToString(); }
                    if (Failure == null)
                    {
                        SetPhase("Warmup");
                        yield return Wait(scenario.WarmupSeconds);
                        if (!_stop && Failure == null && _runtimeError == null)
                        {
                            yield return Cleanup();
                            SetPhase("Setup");
                            try
                            {
                                using (SetupMarker.Auto())
                                {
                                    Setup(scenario);
                                    if (scenario.Kind != PerformanceDemoKind.Throw && !scenario.IsGameLoop) StartMeasurement(scenario);
                                    if (IsReaper(scenario)) _reaper.SetSimulationEnabled(false);
                                }
                            }
                            catch (Exception exception) { Failure = exception.ToString(); }
                            // Let preparation finish its frame before opening a binary log.
                            yield return null;
                            _focusLostDuringMeasure = false;
                            if (Failure == null && !_stop && _runtimeError == null)
                            {
                                try
                                {
                                    if (CaptureProfiler)
                                    {
                                        _capture = new PerformanceDemoCapture();
                                        var capture = _capture.Begin(_recorder.OutputPath, scenario.name, Repetition);
                                        _recorder.Current.captures.Add(capture);
                                    }
                                }
                                catch (Exception exception) { Failure = exception.Message; }
                            }
                            SetPhase("Measure");
                            if (Failure == null && !_stop && _runtimeError == null)
                            {
                                try
                                {
                                    if (scenario.Kind == PerformanceDemoKind.Throw) StartMeasurement(scenario);
                                    if (scenario.IsGameLoop) _loop.Begin(false);
                                    if (IsReaper(scenario)) _reaper.SetSimulationEnabled(true);
                                }
                                catch (Exception exception) { Failure = exception.Message; }
                            }
                            double end = Time.realtimeSinceStartupAsDouble + scenario.MeasureSeconds;
                            var timing = new PerformanceDemoTimingGuard(Time.realtimeSinceStartupAsDouble,
                                scenario.MeasureSeconds, MaximumFrameGapSeconds);
                            double observeAt = 0;
                            bool landed = false;
                            while (Failure == null && !_stop && _runtimeError == null && Time.realtimeSinceStartupAsDouble < end)
                            {
                                try
                                {
                                    using (MeasureMarker.Auto())
                                    {
                                        _capture?.MarkMeasurementSample();
                                        Failure ??= timing.Observe(Time.realtimeSinceStartupAsDouble, Time.timeScale);
                                        if (_focusLostDuringMeasure) Failure ??= "Application lost focus during measurement.";
                                        if (scenario.IsGameLoop)
                                        {
                                            ActualCount = _loop.ActiveEnemies;
                                            if (_loop.Error != null) throw new InvalidOperationException(_loop.Error);
                                            if (_loop.ReadyToFinish) break;
                                        }
                                        if (scenario.Kind == PerformanceDemoKind.Areas)
                                        {
                                            ActualCount = _potions.ActualCount;
                                            if (ActualCount != scenario.Count) throw new InvalidOperationException("Area count changed (expiry/reaction/removal); invalid maintenance interval.");
                                        }
                                        if (IsReaper(scenario))
                                        {
                                            string transition = _reaper.Observe();
                                            if (transition != null) _recorder.Record(scenario, Repetition, Phase, transition, _reaper.SkillStarts);
                                            if (_player.IsDead) throw new InvalidOperationException("Player died; invalid load.");
                                        }
                                        if ((scenario.Kind == PerformanceDemoKind.Throw || scenario.Kind == PerformanceDemoKind.Summon) && Time.realtimeSinceStartupAsDouble >= observeAt)
                                        {
                                            observeAt = Time.realtimeSinceStartupAsDouble + 0.1;
                                            ActualCount = _potions.ObserveLanding(_runtime);
                                            if (!landed && ActualCount > 0)
                                            {
                                                if (scenario.Kind == PerformanceDemoKind.Throw && !_potions.HasLandingAt(_landingPosition))
                                                    throw new InvalidOperationException("Actual landing does not match collision prediction.");
                                                landed = true;
                                                _recorder.Record(scenario, Repetition, Phase, "Landing/footprint verified", ActualCount);
                                            }
                                        }
                                    }
                                }
                                catch (Exception exception) { Failure = exception.Message; }
                                yield return null;
                            }
                            if (!_stop)
                            {
                                Failure ??= timing.Observe(Time.realtimeSinceStartupAsDouble, Time.timeScale);
                                Failure ??= timing.CompletionError(Time.realtimeSinceStartupAsDouble);
                                if (_focusLostDuringMeasure) Failure ??= "Application lost focus during measurement.";
                            }
                            if (!_stop && Failure == null && _runtimeError == null)
                            {
                                if (scenario.IsGameLoop) Failure = _loop.ValidateResult();
                                if ((scenario.Kind == PerformanceDemoKind.Throw || scenario.Kind == PerformanceDemoKind.Summon) && !landed)
                                    Failure = "No valid landing observed.";
                                if (IsReaper(scenario))
                                {
                                    _recorder.Record(scenario, Repetition, Phase, $"Skill starts={_reaper.SkillStarts}; completions={_reaper.SkillCompletions}; theft={_reaper.TheftSucceeded}", ActualCount);
                                    if (_reaper.SkillStarts == 0 || _reaper.SkillCompletions == 0) Failure = "Expected Reaper skill did not complete.";
                                    if (scenario.Kind == PerformanceDemoKind.Steal && !_reaper.TheftSucceeded) Failure = "Potion theft did not succeed.";
                                }
                            }
                            Failure ??= _runtimeError;
                            EndCapture(Failure != null ? "Failed" : _stop ? "Stopped" : "Completed", Failure);
                            if (CaptureScreenshots && Repetition == 1 && Failure == null && !_stop)
                                yield return CaptureScreenshot(scenario);
                        }
                    }
                    Failure ??= _runtimeError;
                    yield return Cleanup();
                    if (Failure != null || _stop) break;
                }
            }
        }

        if (_runtime.IsValid() && _runtime.isLoaded) yield return Cleanup();
        Failure ??= _runtimeError;
        SetPhase("Save");
        string result = Failure != null ? "Failed" : _stop ? "Stopped" : "Completed";
        try { _recorder.Save(result, Failure); }
        catch (Exception exception) { Failure = "Save failed: " + exception.Message; result = "Failed"; }
        Application.logMessageReceived -= OnLog;
        UnityEngine.Random.state = _randomState;
        Time.timeScale = _timeScale;
        Phase = result;
        IsRunning = false;
        OptimizationRuntime.Select(OptimizationVariant.Original);
        OptimizedEnemySearch.Clear();
    }
    private static bool IsReaper(PerformanceDemoScenario scenario) => scenario.Kind >= PerformanceDemoKind.Dash && scenario.Kind <= PerformanceDemoKind.Summon;
    private void Validate(PerformanceDemoScenario scenario)
    {
        if (MaximumFrameGapSeconds < 0.1f || float.IsNaN(MaximumFrameGapSeconds) || float.IsInfinity(MaximumFrameGapSeconds))
            throw new InvalidOperationException("Maximum frame gap must be finite and at least 0.1 seconds.");
        if (scenario == null || scenario.Repetitions < 1 || scenario.Count < 1 || scenario.MeasureSeconds <= 0 || scenario.WarmupSeconds < 0 ||
            float.IsNaN(scenario.MeasureSeconds) || float.IsInfinity(scenario.MeasureSeconds) ||
            float.IsNaN(scenario.WarmupSeconds) || float.IsInfinity(scenario.WarmupSeconds))
        {
            throw new InvalidOperationException("Invalid scenario settings.");
        }

        if (Catalog == null || !Catalog.IsInitialized || SoundManager.Data == null || LayerMask.NameToLayer("Wall") < 0)
            throw new InvalidOperationException("Catalog, sound data or Wall layer missing.");
        if (Arenas == null || Arenas.Length != 4 || Arenas[(int)scenario.Arena] == null)
            throw new InvalidOperationException("Missing arena.");
        if (scenario.Kind != PerformanceDemoKind.Baseline && (scenario.Potion == null || !scenario.Potion.CreatesFloor))
            throw new InvalidOperationException("Valid floor potion required.");
        if (scenario.Kind == PerformanceDemoKind.Areas && scenario.Potion.FloorDuration < scenario.MeasureSeconds + 0.25f)
            throw new InvalidOperationException("Maintenance interval exceeds potion lifetime.");
        if (IsReaper(scenario) && (scenario.ReaperPrefab == null || PlayerPrefab == null))
            throw new InvalidOperationException("Missing Reaper/player prefab.");
        if (_potions.AreaPrefab == null || _potions.ProjectilePrefab == null)
            throw new InvalidOperationException("Missing potion prefabs.");
        if (scenario.IsGameLoop)
        {
            if (LoopPlayerPrefab == null || scenario.EnemyCount < 0 || scenario.CombinationPairs < 0 || scenario.LifecycleCycles < 1 || scenario.WaveCount < 1)
                throw new InvalidOperationException("Invalid game-loop settings/player prefab.");
            if (scenario.EnemyCount > 0 && (scenario.EnemyPrefabs == null || scenario.EnemyPrefabs.Length == 0))
                throw new InvalidOperationException("Game-loop enemy roster is empty.");
            if ((scenario.CombinationPairs > 0 || scenario.Kind == PerformanceDemoKind.Combat || scenario.Kind == PerformanceDemoKind.Lifecycle) && scenario.SecondPotion == null)
                throw new InvalidOperationException("A second potion is required for combination/combat.");
        }
    }
    private void Setup(PerformanceDemoScenario scenario)
    {
        OptimizationRuntime.Select(scenario.Optimizations);
        ActualCount = 0;
        for (int i = 0; i < Arenas.Length; i++) Arenas[i].SetActive(i == (int)scenario.Arena);
        Physics2D.SyncTransforms();
        EnemyPathfinder.Instance.RebuildGrid();
        _runtime = SceneManager.CreateScene("PerformanceDemoRuntime");
        SceneManager.SetActiveScene(_runtime);
        UnityEngine.Random.InitState(scenario.Seed + Repetition - 1);
        if (scenario.IsGameLoop)
        {
            _loop.Setup(scenario, scenario.DiagnosticHighHealth ? PlayerPrefab : LoopPlayerPrefab, Catalog, _potions, _recorder, Repetition);
            return;
        }
        if (IsReaper(scenario))
        {
            _player = Instantiate(PlayerPrefab).GetComponent<PlayerStatus>();
            _player.transform.position = scenario.Kind == PerformanceDemoKind.Summon ? new Vector3(4, 0, 0) : Vector3.zero;
            _player.GetComponent<PlayerController>().AcquireGamePlayInputLock(this);
        }
    }
    private Vector3 _landingPosition;
    private void StartMeasurement(PerformanceDemoScenario scenario)
    {
        if (scenario.IsGameLoop) return;
        if (scenario.Kind == PerformanceDemoKind.Areas)
        {
            using (SetupMarker.Auto()) _potions.CreateAreas(scenario, null, Catalog);
            ActualCount = _potions.ActualCount;
            if (ActualCount != scenario.Count) throw new InvalidOperationException("Area spawn count mismatch.");
        }
        else if (scenario.Kind == PerformanceDemoKind.Throw)
        {
            PotionThrowPrediction prediction = _potions.Throw(scenario, null, Catalog);
            _landingPosition = prediction.AreaPosition;
            if (!prediction.CanCreateArea) throw new InvalidOperationException("Throw has no valid ground placement.");
            if (scenario.Arena != PerformanceDemoArena.OpenFloor && prediction.FlightProgress >= 1)
                throw new InvalidOperationException("Wall collision not reproduced.");
            _recorder.Record(scenario, Repetition, Phase, "Launch; predicted ground=" + prediction.AreaPosition);
        }
        else if (IsReaper(scenario))
        {
            _reaper.Setup(scenario, _player, Catalog);
            ActualCount = 1;
        }
    }
    private IEnumerator CaptureScreenshot(PerformanceDemoScenario scenario)
    {
        // Failure here only loses the picture; it must not invalidate a finished measurement.
        yield return new WaitForEndOfFrame();
        Texture2D texture = null;
        try
        {
            texture = ScreenCapture.CaptureScreenshotAsTexture();
            string path = System.IO.Path.Combine(_recorder.OutputPath, scenario.name + "_repeat" + Repetition.ToString("00") + ".png");
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            _recorder.Record(scenario, Repetition, Phase, "Screenshot saved: " + System.IO.Path.GetFileName(path));
        }
        catch (Exception exception) { Debug.LogWarning("Screenshot skipped: " + exception.Message); }
        finally { if (texture != null) Destroy(texture); }
    }
    private IEnumerator Wait(float seconds)
    {
        double start = Time.realtimeSinceStartupAsDouble;
        double end = start + seconds;
        var timing = new PerformanceDemoTimingGuard(start, seconds, MaximumFrameGapSeconds);
        while (!_stop && Failure == null && _runtimeError == null && Time.realtimeSinceStartupAsDouble < end)
        {
            Failure ??= timing.Observe(Time.realtimeSinceStartupAsDouble, Time.timeScale);
            yield return null;
        }
        if (!_stop)
        {
            Failure ??= timing.Observe(Time.realtimeSinceStartupAsDouble, Time.timeScale);
            Failure ??= timing.CompletionError(Time.realtimeSinceStartupAsDouble);
        }
    }
    private IEnumerator Cleanup()
    {
        EndCapture(Failure != null ? "Failed" : "Stopped", Failure);
        SetPhase("Cleanup");
        AsyncOperation unload = null;
        using (CleanupMarker.Auto())
        {
            _loop.Clear();
            SceneManager.SetActiveScene(_home);
            if (_runtime.IsValid() && _runtime.isLoaded)
            {
                QuiesceRuntimeEffects();
                foreach (GameObject root in _runtime.GetRootGameObjects()) root.SetActive(false);
                unload = SceneManager.UnloadSceneAsync(_runtime);
            }
            _potions.Clear();
            _reaper.Clear();
            _player = null;
        }
        if (unload != null) yield return unload;
        SetPhase("Settle");
        yield return null;
        yield return null;
        if (_runtime.IsValid() && _runtime.isLoaded) Failure ??= "Runtime scene remained after cleanup.";
        ActualCount = 0;
        _recorder.Record(_scenario, Repetition, Phase, "Runtime scene unloaded; residual scene count=0");
        if (_scenario != null && _scenario.Kind == PerformanceDemoKind.Lifecycle)
        {
            _recorder.Current.cleanupSnapshots.Add(new PerformanceDemoRecorder.CleanupSnapshot
            {
                scenarioName = _scenario.name,
                repetition = Repetition,
                sceneCount = SceneManager.sceneCount,
                managedBytes = GC.GetTotalMemory(false),
                textures = Resources.FindObjectsOfTypeAll<Texture>().Length,
                afterMeasurement = _loop.Result != null && _recorder.Current.gameLoops.Contains(_loop.Result),
                meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length,
                materials = Resources.FindObjectsOfTypeAll<Material>().Length,
                realtime = Time.realtimeSinceStartup
            });
        }
    }
    private void EndCapture(string status, string failure)
    {
        if (_capture == null) return;
        try
        {
            var capture = _capture.End(status, failure);
            if (capture != null && capture.status == "Failed") Failure ??= capture.failure;
        }
        catch (Exception exception) { Failure ??= "Capture finalization failed: " + exception.Message; }
        finally { _capture = null; }
    }
    private void OnApplicationFocus(bool focused)
    {
        if (CaptureProfiler && IsRunning && Phase == "Measure" && !focused && !Application.isBatchMode) _focusLostDuringMeasure = true;
    }
    private void OnDisable() => AbortRun();
    private void OnApplicationQuit() => AbortRun();
    private void OnDestroy() => AbortRun();
    private void AbortRun()
    {
        if (_loop != null) _loop.Clear();
        Application.logMessageReceived -= OnLog;
        if (!IsRunning) return;
        StopAllCoroutines();
        EndCapture("Stopped", "Runner disabled, destroyed, or application exiting.");
        SetPhase("Save");
        string result = Failure != null ? "Failed" : "Stopped";
        try { _recorder.Save(result, Failure ?? "Runner interrupted."); }
        catch (Exception exception) { Debug.LogWarning("Interrupted demo save failed: " + exception.Message); }
        UnityEngine.Random.state = _randomState;
        Time.timeScale = _timeScale;
        Phase = result;
        IsRunning = false;
        QuiesceRuntimeEffects();
        if (_runtime.IsValid() && _runtime.isLoaded) SceneManager.UnloadSceneAsync(_runtime);
        OptimizationRuntime.Select(OptimizationVariant.Original);
        OptimizedEnemySearch.Clear();
    }
    private void QuiesceRuntimeEffects()
    {
        if (!_runtime.IsValid() || !_runtime.isLoaded) return;
        // End area timelines before deactivating targets. Trigger exits during
        // target teardown must not flush pending damage into an inactive enemy.
        foreach (GameObject root in _runtime.GetRootGameObjects())
        {
            foreach (var effect in root.GetComponentsInChildren<PotionAreaEffect>(true))
                effect.StopForVaporization();
        }
    }
}
