using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class PerformanceDemoRecorder : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string scenario, scenarioName, phase, detail;
        public int repetition, frame, actualCount;
        public float realtime, scaledTime, timeScale;
    }
    [Serializable]
    public sealed class Run
    {
        public int schemaVersion = 2;
        public string runId, unity, device, graphicsApi, quality, result, failure;
        public string scenePath;
        public bool isEditor, developmentBuild;
        public int width, height, targetFrameRate, vSync;
        public float fixedDeltaTime;
        public string conditions = "Implementation flags and diagnostic overrides are in settings; audio enabled; fixed camera; no camera shake controller. " +
            "S3 landing observation: runtime scene roots every 0.1 unscaled seconds. " +
            "Per-interval binary capture is controlled by the runner; its status is in captures. " +
            "CPU/GPU frame statistics require offline analysis; markerVerification remains pending until that analysis.";
        public List<string> settings = new();
        public List<Entry> entries = new();
        public List<PerformanceDemoCapture.Result> captures = new();
        public bool automaticProfilerCapture;
        public float maximumFrameGapSeconds;
        public List<LoopResult> gameLoops = new();
        public List<CleanupSnapshot> cleanupSnapshots = new();
    }
    [Serializable]
    public sealed class LoopResult
    {
        public string scenarioName, failure, scope = "Fixed arena combat; no stage/room progression";
        public string outcome = "Running", inputTimeline = "loop-v2: fixed opening, nearest-target melee and fuse avoidance";
        public string playerData, firstPotion, secondPotion;
        public int configuredEnemies, configuredWaves, effectiveSeed, playerDamageEvents;
        public int repetition, enemiesSpawned, enemyDeaths, enemyReleases, enemyDamageEvents, potionDamageEvents;
        public int combinations, projectilesLaunched, playerThrows, meleeAttacks, meleeHits, dashes, inputAttempts, inputAccepted;
        public int peakEnemies, wavesStarted, cycles;
        public float playerDistance, enemyDistance, combatCompletedAt;
        public bool combatCompleted, functionalValid, showDemoUI, diagnosticHighHealth;
    }
    public Run Current { get; private set; }
    [Serializable]
    public sealed class CleanupSnapshot
    {
        public string scenarioName;
        public int repetition, sceneCount, textures, meshes, materials;
        public long managedBytes;
        public float realtime;
        public bool afterMeasurement;
    }
    public string OutputPath { get; private set; }
    public void Begin()
    {
        Current = new Run
        {
            runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8),
            unity = Application.unityVersion,
            device = SystemInfo.deviceModel,
            graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
            quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
            width = Screen.width,
            height = Screen.height,
            targetFrameRate = Application.targetFrameRate,
            vSync = QualitySettings.vSyncCount,
            fixedDeltaTime = Time.fixedDeltaTime,
            isEditor = Application.isEditor,
            developmentBuild = Debug.isDebugBuild
        };
        OutputPath = Path.Combine(Application.persistentDataPath, "PerformanceDemo", Current.runId);
        Directory.CreateDirectory(OutputPath);
    }
    public void Record(PerformanceDemoScenario scenario, int repetition, string phase, string detail, int count = 0)
    {
        if (Current == null) return;
        Current.entries.Add(new Entry
        {
            scenario = scenario != null ? scenario.Id : "Run",
            scenarioName = scenario != null ? scenario.name : "Run",
            repetition = repetition,
            phase = phase,
            detail = detail,
            actualCount = count,
            frame = Time.frameCount,
            realtime = Time.realtimeSinceStartup,
            scaledTime = Time.time,
            timeScale = Time.timeScale
        });
    }
    public void Save(string result, string failure)
    {
        Current.result = result;
        Current.failure = failure;
        Directory.CreateDirectory(OutputPath);
        File.WriteAllText(Path.Combine(OutputPath, "run.json"), JsonUtility.ToJson(Current, true));
        var csv = new StringBuilder("scenario,scenarioName,repetition,phase,frame,realtime,scaledTime,timeScale,actualCount,detail\n");
        foreach (Entry entry in Current.entries)
        {
            csv.AppendLine(string.Join(",", Quote(entry.scenario), Quote(entry.scenarioName), entry.repetition, Quote(entry.phase), entry.frame,
                entry.realtime.ToString("R", CultureInfo.InvariantCulture), entry.scaledTime.ToString("R", CultureInfo.InvariantCulture),
                entry.timeScale.ToString("R", CultureInfo.InvariantCulture), entry.actualCount, Quote(entry.detail)));
        }

        File.WriteAllText(Path.Combine(OutputPath, "events.csv"), csv.ToString());
        Debug.Log($"PerformanceDemo {result}: {OutputPath}");
    }
    private static string Quote(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
}
