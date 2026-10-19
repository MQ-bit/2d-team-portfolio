using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

// Copied by Analyze-Profiler.ps1 into a separate, version-matched Unity project.
// This file is outside Assets and is not part of the game/editor assemblies.
public static class ProfilerCaptureAnalyzer
{
    [Serializable] public sealed class Request
    {
        public string[] sources;
        public string manifestPath, outputDirectory;
        public string[] manifestPaths;
        public bool optimizationOnly;
        public int maxFrames = 100000;
    }
    [Serializable] public sealed class CaptureMetadata
    {
        public string scenarioName, fileName, status, failure;
        public int repetition, measureSamples, startFrame, endFrame;
        public double startRealtime, endRealtime;
        public long fileBytes;
    }
    [Serializable] public sealed class Manifest
    {
        public string unity, result, failure, scenePath, conditions;
        public bool isEditor, developmentBuild;
        public List<CaptureMetadata> captures;
        public List<GameLoopMetadata> gameLoops;
    }
    [Serializable] public sealed class GameLoopMetadata
    {
        public string scenarioName, outcome, scope, failure, inputTimeline, playerData, firstPotion, secondPotion;
        public int repetition, configuredEnemies, configuredWaves, enemiesSpawned, wavesStarted, combinations, playerThrows;
        public int meleeAttacks, enemyDamageEvents, playerDamageEvents, enemyDeaths, enemyReleases;
        public int effectiveSeed, potionDamageEvents, projectilesLaunched, meleeHits, dashes;
        public int inputAttempts, inputAccepted, peakEnemies, cycles;
        public bool functionalValid, combatCompleted, showDemoUI, diagnosticHighHealth;
        public float playerDistance, enemyDistance, combatCompletedAt;
    }
    [Serializable] public sealed class Timing
    {
        public int frames;
        public double averageMs, p50Ms, p95Ms, p99Ms, maximumMs;
        public int over16_67Ms, over33_33Ms;
    }
    [Serializable] public sealed class Hotspot
    {
        public string thread, name;
        public long calls, inclusiveGcBytes;
        public int frames;
        public double inclusiveMs, selfMs, maxSingleMs, averageSelfMsPerSelectedFrame;
    }
    [Serializable] public sealed class AllocationSite
    {
        public string thread, scopePath;
        public long bytes, allocations;
    }
    [Serializable] public sealed class CaptureReport
    {
        public string source, reportDirectory, scenarioName, runtimeStatus, markerVerification, validationLevel;
        public int repetition, firstLoadedFrame, lastLoadedFrame, loadedFrames, selectedFrames, boundaryFramesExcluded;
        public bool valid, manifestMatched;
        public int expectedMeasureSamples, actualMeasureSamples, setupSamples, cleanupSamples;
        public long mainThreadGcBytes;
        public double mainThreadGcBytesPerSelectedFrame;
        public Timing observedMainThreadTiming, measurementMainThreadTiming;
        public GameLoopMetadata gameLoop;
        public List<string> issues = new List<string>();
        public List<string> warnings = new List<string>();
        public List<Hotspot> topHotspots = new List<Hotspot>();
        public List<AllocationSite> topAllocationSites = new List<AllocationSite>();
    }
    [Serializable] public sealed class Summary
    {
        public string analysisUnityVersion, manifestPath, generatedUtc;
        public int historyLimit, validCaptures, invalidCaptures;
        public string metricNotes = "Main-thread frame timing includes Editor and waits. Render-thread CPU time is not GPU time. " +
            "GC bytes are allocations, not retained memory. Inclusive times/allocations overlap and must not be added. " +
            "Hotspots use only interior frames containing PerfDemo.Measure and no Setup/Cleanup markers; boundary frames are excluded. " +
            "Without matching run.json, validation covers markers only, not runtime success or complete coverage.";
        public List<CaptureReport> captures = new List<CaptureReport>();
    }
    sealed class Frame
    {
        public int index, measure, setup, cleanup;
        public double mainMs, renderMs;
        public long gcBytes;
        public bool selected;
        public readonly List<FrameScope> eventScopes = new List<FrameScope>();
        public readonly List<FrameScope> spikeScopes = new List<FrameScope>();
    }
    sealed class FrameScope
    {
        public string name;
        public int sample;
        public double inclusiveMs;
    }
    sealed class OptimizationScope
    {
        public string name;
        public int end;
        public double inclusiveMs, selfMs;
        public long gcBytes, allocations;
    }
    sealed class Stat
    {
        public Hotspot value;
        public int lastFrame = -1;
    }
    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    static string Number(double value) => value.ToString("F6", Invariant);
    static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
    static string Code(string value) => (value ?? "").Replace("`", "'").Replace("\r", " ").Replace("\n", " ");

    public static void Run()
    {
        Request request = null;
        try
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(arguments, "-profilerAnalysisRequest");
            if (option < 0 || option + 1 == arguments.Length) throw new ArgumentException("Missing -profilerAnalysisRequest.");
            request = JsonUtility.FromJson<Request>(File.ReadAllText(arguments[option + 1]));
            if (request == null || request.sources == null || request.sources.Length == 0) throw new ArgumentException("No capture sources.");
            if (request.manifestPaths != null && request.manifestPaths.Length != request.sources.Length)
                throw new ArgumentException("Per-source manifest count must match captures.");
            Directory.CreateDirectory(request.outputDirectory);
            SetHistoryLimit(request.maxFrames);
            Manifest manifest = string.IsNullOrEmpty(request.manifestPath) ? null : JsonUtility.FromJson<Manifest>(File.ReadAllText(request.manifestPath));
            var summary = new Summary { analysisUnityVersion = Application.unityVersion, manifestPath = request.manifestPath,
                generatedUtc = DateTime.UtcNow.ToString("O"), historyLimit = request.maxFrames };
            if (request.optimizationOnly) summary.metricNotes += " Optimization-only aggregation: hotspot and allocation paths contain selected instrumented scopes, not every sample.";
            for (int i = 0; i < request.sources.Length; i++)
            {
                string directory = Path.Combine(request.outputDirectory, $"capture_{i + 1:000}");
                Directory.CreateDirectory(directory);
                var report = new CaptureReport { source = Path.GetFullPath(request.sources[i]), reportDirectory = directory };
                try
                {
                    Manifest sourceManifest = request.manifestPaths == null ? manifest :
                        JsonUtility.FromJson<Manifest>(File.ReadAllText(request.manifestPaths[i]));
                    Analyze(request, sourceManifest, report);
                }
                catch (Exception exception) { report.issues.Add("AnalysisError: " + exception.Message); report.valid = false; }
                report.markerVerification = report.valid ? "Verified" : "Failed";
                File.WriteAllText(Path.Combine(directory, "summary.json"), JsonUtility.ToJson(report, true));
                File.WriteAllText(Path.Combine(directory, "report.md"), CaptureMarkdown(report));
                summary.captures.Add(report);
                Debug.Log($"PROFILER_CAPTURE {i + 1}/{request.sources.Length}: valid={report.valid}, selectedFrames={report.selectedFrames}, {report.source}");
            }
            summary.validCaptures = summary.captures.Count(c => c.valid);
            summary.invalidCaptures = summary.captures.Count - summary.validCaptures;
            File.WriteAllText(Path.Combine(request.outputDirectory, "summary.json"), JsonUtility.ToJson(summary, true));
            var markdown = new StringBuilder("# Unity Profiler analysis\n\n");
            markdown.AppendLine(summary.metricNotes + "\n");
            markdown.AppendLine($"Valid: {summary.validCaptures}; invalid: {summary.invalidCaptures}. Analysis Unity: {Application.unityVersion}.\n");
            markdown.AppendLine("| Capture | Valid | Measure frames | Main p95 (ms) | Main GC/frame (bytes) | Details |\n| --- | --- | ---: | ---: | ---: | --- |");
            for (int i = 0; i < summary.captures.Count; i++)
            {
                var c = summary.captures[i];
                markdown.AppendLine($"| {Code(Path.GetFileName(c.source)).Replace("|", "\\|")} | {c.valid} | {c.selectedFrames} | {Number(c.measurementMainThreadTiming?.p95Ms ?? 0)} | {Number(c.mainThreadGcBytesPerSelectedFrame)} | [Report](capture_{i + 1:000}/report.md) |");
            }
            File.WriteAllText(Path.Combine(request.outputDirectory, "report.md"), markdown.ToString());
            EditorApplication.Exit(summary.invalidCaptures == 0 ? 0 : 2);
        }
        catch (Exception exception)
        {
            if (request != null && !string.IsNullOrEmpty(request.outputDirectory)) File.WriteAllText(Path.Combine(request.outputDirectory, "error.txt"), exception.ToString());
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static void SetHistoryLimit(int count)
    {
        if (count < 2000 || count > 1000000) throw new ArgumentException("History limit must be between 2000 and 1000000.");
        // Unity's public loader otherwise silently retains only the configured
        // UI history. This isolated adapter is intentionally version checked.
        var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var setter = typeof(ProfilerDriver).GetMethod("SetMaxFrameHistoryLength", flags, null, new[] { typeof(int) }, null);
        if (setter == null) throw new NotSupportedException("This Unity version has no supported SetMaxFrameHistoryLength adapter.");
        setter.Invoke(null, new object[] { count });
        typeof(ProfilerDriver).GetMethod("SetAutomaticMemoryManagement", flags, null, new[] { typeof(bool) }, null)?.Invoke(null, new object[] { false });
        ProfilerDriver.enabled = false;
    }

    static void Analyze(Request request, Manifest manifest, CaptureReport report)
    {
        CaptureMetadata metadata = manifest?.captures?.FirstOrDefault(c => string.Equals(c.fileName, Path.GetFileName(report.source), StringComparison.OrdinalIgnoreCase));
        report.manifestMatched = metadata != null;
        report.validationLevel = metadata != null ? "RuntimeAndMarkers" : "MarkersOnly";
        report.scenarioName = metadata?.scenarioName ?? Path.GetFileNameWithoutExtension(report.source);
        report.repetition = metadata?.repetition ?? 0;
        report.runtimeStatus = metadata?.status ?? "Unknown";
        report.gameLoop = manifest?.gameLoops?.FirstOrDefault(loop => loop.scenarioName == report.scenarioName && loop.repetition == report.repetition);
        if (report.gameLoop != null)
        {
            if (!report.gameLoop.functionalValid) report.issues.Add("GameLoopFunctionalValidation: " + report.gameLoop.failure);
            if (report.gameLoop.outcome == "PlayerDefeated") report.warnings.Add("PlayerDefeated: allowed defeat outcome, not complete-wave victory; compare only equivalent outcomes and workloads.");
            if (report.gameLoop.diagnosticHighHealth) report.warnings.Add("Diagnostic high-health player; not representative combat balance.");
        }
        if (metadata == null) report.warnings.Add("No matching run.json metadata: runtime success and full capture coverage cannot be verified.");
        else
        {
            report.expectedMeasureSamples = metadata.measureSamples;
            if (metadata.status != "Completed") report.issues.Add("RuntimeStatus: " + metadata.status + "; " + metadata.failure);
            if (metadata.measureSamples <= 0) report.issues.Add("Manifest contains no measurement samples.");
        }
        if (manifest != null)
        {
            if (!string.IsNullOrEmpty(manifest.unity) && manifest.unity != Application.unityVersion)
                report.issues.Add("UnityVersionMismatch: source=" + manifest.unity + "; analyzer=" + Application.unityVersion);
            if (manifest.isEditor) report.warnings.Add("Recorded in the Editor; inspect EditorLoop separately and confirm performance in a Development Build.");
            if (manifest.result != "Completed") report.warnings.Add("Containing run ended as " + manifest.result + ": " + manifest.failure);
        }
        if (!File.Exists(report.source)) throw new FileNotFoundException("Capture listed but not found.", report.source);
        if (metadata != null && metadata.fileBytes > 0 && new FileInfo(report.source).Length != metadata.fileBytes)
            report.issues.Add("FileSizeMismatch: capture may have been modified or incompletely copied.");
        if (!ProfilerDriver.LoadProfile(report.source, false)) throw new InvalidDataException("Unity cannot load capture.");
        report.firstLoadedFrame = ProfilerDriver.firstFrameIndex;
        report.lastLoadedFrame = ProfilerDriver.lastFrameIndex;
        if (report.firstLoadedFrame < 0 || report.lastLoadedFrame < report.firstLoadedFrame) throw new InvalidDataException("No frames loaded.");
        report.loadedFrames = report.lastLoadedFrame - report.firstLoadedFrame + 1;
        if (report.loadedFrames >= request.maxFrames) report.issues.Add("HistoryLimitReached: increase -MaxFrames; complete capture is not guaranteed.");
        var frames = new List<Frame>();
        var stats = new Dictionary<string, Stat>();
        var allocations = new Dictionary<string, AllocationSite>();
        for (int index = report.firstLoadedFrame; index <= report.lastLoadedFrame; index++)
        {
            int mainThread = -1, renderThread = -1;
            for (int thread = 0; thread < 512; thread++)
                using (var view = ProfilerDriver.GetRawFrameDataView(index, thread))
                {
                    if (!view.valid) break;
                    if (view.threadName == "Main Thread") mainThread = thread;
                    if (view.threadName == "Render Thread") renderThread = thread;
                    if (mainThread >= 0 && renderThread >= 0) break;
                }
            if (mainThread < 0) { report.issues.Add("MissingMainThread: loaded frame " + index); continue; }
            var frame = new Frame { index = index };
            var optimizationScopes = request.optimizationOnly ? new List<OptimizationScope>() : null;
            var activeScopes = request.optimizationOnly ? new List<OptimizationScope>() : null;
            using (var view = ProfilerDriver.GetRawFrameDataView(index, mainThread))
            {
                frame.mainMs = view.frameTimeMs;
                for (int sample = 0; sample < view.sampleCount; sample++)
                {
                    string name = view.GetSampleName(sample) ?? "";
                    if (name == "PerfDemo.Measure") frame.measure++;
                    if (name == "PerfDemo.Setup") frame.setup++;
                    if (name == "PerfDemo.Cleanup") frame.cleanup++;
                    if (name == "GC.Alloc")
                    {
                        long bytes = AllocationBytes(view, sample);
                        frame.gcBytes += bytes;
                        if (request.optimizationOnly)
                            foreach (var scope in activeScopes)
                                if (sample < scope.end)
                                {
                                    scope.gcBytes += bytes;
                                    if (bytes > 0) scope.allocations++;
                                }
                    }
                    if (request.optimizationOnly)
                    {
                        while (activeScopes.Count > 0 && sample >= activeScopes[activeScopes.Count - 1].end)
                            activeScopes.RemoveAt(activeScopes.Count - 1);
                        if (IsOptimizationScope(name))
                        {
                            double duration = view.GetSampleTimeMs(sample), self = duration;
                            int child = sample + 1;
                            for (int c = 0; c < view.GetSampleChildrenCount(sample); c++)
                            {
                                self -= view.GetSampleTimeMs(child);
                                child += 1 + view.GetSampleChildrenCountRecursive(child);
                            }
                            var scope = new OptimizationScope { name = name,
                                end = sample + 1 + view.GetSampleChildrenCountRecursive(sample),
                                inclusiveMs = duration, selfMs = Math.Max(0, self) };
                            optimizationScopes.Add(scope); activeScopes.Add(scope);
                        }
                        if (IsEventScope(name)) frame.eventScopes.Add(new FrameScope
                            { name = name, sample = sample, inclusiveMs = view.GetSampleTimeMs(sample) });
                        if (frame.mainMs > 100 && view.GetSampleTimeMs(sample) >= 10)
                            frame.spikeScopes.Add(new FrameScope { name = name, sample = sample, inclusiveMs = view.GetSampleTimeMs(sample) });
                    }
                }
                bool boundary = index == report.firstLoadedFrame || index == report.lastLoadedFrame;
                if (boundary && frame.measure > 0) report.boundaryFramesExcluded++;
                frame.selected = !boundary && frame.measure > 0 && frame.setup == 0 && frame.cleanup == 0;
                if (!frame.selected) frame.eventScopes.Clear();
                if (!request.optimizationOnly && frame.mainMs > 100)
                    for (int sample = 0; sample < view.sampleCount; sample++)
                    {
                        double duration = view.GetSampleTimeMs(sample);
                        if (duration >= 10)
                            frame.spikeScopes.Add(new FrameScope { name = view.GetSampleName(sample), sample = sample, inclusiveMs = duration });
                    }
                if (frame.selected && request.optimizationOnly)
                {
                    foreach (var scope in optimizationScopes)
                    {
                        string key = "Main Thread\t" + scope.name;
                        if (!stats.TryGetValue(key, out Stat stat)) stats[key] = stat = new Stat
                            { value = new Hotspot { thread = "Main Thread", name = scope.name } };
                        stat.value.calls++;
                        stat.value.inclusiveMs += scope.inclusiveMs;
                        stat.value.selfMs += scope.selfMs;
                        stat.value.inclusiveGcBytes += scope.gcBytes;
                        stat.value.maxSingleMs = Math.Max(stat.value.maxSingleMs, scope.inclusiveMs);
                        if (stat.lastFrame != index) { stat.lastFrame = index; stat.value.frames++; }
                        if (scope.gcBytes > 0)
                        {
                            if (!allocations.TryGetValue(key, out AllocationSite site)) allocations[key] = site = new AllocationSite
                                { thread = "Main Thread", scopePath = scope.name };
                            site.bytes += scope.gcBytes; site.allocations += scope.allocations;
                        }
                    }
                }
                if (frame.selected && !request.optimizationOnly)
                {
                    Aggregate(view, index, stats, allocations);
                    for (int sample = 0; sample < view.sampleCount; sample++)
                    {
                        string name = view.GetSampleName(sample) ?? "";
                        if (IsEventScope(name))
                            frame.eventScopes.Add(new FrameScope { name = name, sample = sample, inclusiveMs = view.GetSampleTimeMs(sample) });
                    }
                }
            }
            if (renderThread >= 0)
                using (var view = ProfilerDriver.GetRawFrameDataView(index, renderThread))
                {
                    frame.renderMs = view.frameTimeMs;
                    if (frame.selected && !request.optimizationOnly) Aggregate(view, index, stats, allocations);
                }
            frames.Add(frame);
            report.actualMeasureSamples += frame.measure;
            report.setupSamples += frame.setup;
            report.cleanupSamples += frame.cleanup;
        }
        if (report.actualMeasureSamples == 0) report.issues.Add("NoMeasureMarkers: no active demo measurement is present.");
        if (report.setupSamples > 0 || report.cleanupSamples > 0) report.issues.Add("PreparationOrCleanupPresent: capture is not confined to Measure.");
        if (metadata != null && Math.Abs(report.actualMeasureSamples - (long)metadata.measureSamples) > 2)
            report.issues.Add($"MeasurementCoverageMismatch: expected {metadata.measureSamples}, loaded {report.actualMeasureSamples}; truncated or wrong file.");
        var selected = frames.Where(f => f.selected).ToList();
        report.selectedFrames = selected.Count;
        if (selected.Count == 0 && report.actualMeasureSamples > 0) report.issues.Add("NoCompleteMeasureFrames: capture is too short after boundary exclusion.");
        report.observedMainThreadTiming = Summarize(frames.Select(f => f.mainMs));
        report.measurementMainThreadTiming = Summarize(selected.Select(f => f.mainMs));
        report.mainThreadGcBytes = selected.Sum(f => f.gcBytes);
        report.mainThreadGcBytesPerSelectedFrame = selected.Count > 0 ? (double)report.mainThreadGcBytes / selected.Count : 0;
        var hotspots = stats.Values.Select(s => s.value).OrderByDescending(s => s.selfMs).ToList();
        foreach (var hotspot in hotspots) hotspot.averageSelfMsPerSelectedFrame = selected.Count > 0 ? hotspot.selfMs / selected.Count : 0;
        report.topHotspots = hotspots.Take(20).ToList();
        report.topAllocationSites = allocations.Values.OrderByDescending(a => a.bytes).Take(20).ToList();
        report.valid = report.issues.Count == 0;
        WriteCsv(report.reportDirectory, frames, hotspots, allocations.Values);
    }

    static long AllocationBytes(RawFrameDataView view, int sample) => view.GetSampleMetadataCount(sample) > 0
        ? Math.Max(0, view.GetSampleMetadataAsLong(sample, 0)) : 0;

    static bool IsEventScope(string name) => name == "PerfDemo.CombineCommit" || name.Contains("PotionProjectile.Update()") ||
        name.StartsWith("Optimization.Combine.", StringComparison.Ordinal) || name.StartsWith("Optimization.Mask.", StringComparison.Ordinal);

    static bool IsOptimizationScope(string name) => name == "PlayerLoop" || name == "PerfDemo.CombineCommit" ||
        name.StartsWith("Optimization.", StringComparison.Ordinal) || name.Contains("PotionStatusVisual.LateUpdate()") ||
        name.Contains("RangedEnemyController.FixedUpdate()") || name.Contains("PotionProjectile.Update()");

    static void Aggregate(RawFrameDataView view, int frameIndex, Dictionary<string, Stat> stats, Dictionary<string, AllocationSite> allocations)
    {
        var ancestors = new List<int>();
        int count = view.sampleCount;
        var ends = new int[count];
        var names = new string[count];
        var samples = new Stat[count];
        for (int i = 0; i < count; i++)
        {
            while (ancestors.Count > 0 && i >= ends[ancestors[ancestors.Count - 1]]) ancestors.RemoveAt(ancestors.Count - 1);
            string name = names[i] = view.GetSampleName(i) ?? "<unnamed>";
            ends[i] = i + 1 + view.GetSampleChildrenCountRecursive(i);
            double duration = view.GetSampleTimeMs(i), self = duration;
            int childIndex = i + 1;
            for (int c = 0; c < view.GetSampleChildrenCount(i); c++)
            {
                self -= view.GetSampleTimeMs(childIndex);
                childIndex += 1 + view.GetSampleChildrenCountRecursive(childIndex);
            }
            string key = view.threadName + "\t" + name;
            if (!stats.TryGetValue(key, out Stat stat)) stats[key] = stat = new Stat { value = new Hotspot { thread = view.threadName, name = name } };
            samples[i] = stat;
            stat.value.inclusiveMs += duration;
            stat.value.selfMs += Math.Max(0, self);
            stat.value.maxSingleMs = Math.Max(stat.value.maxSingleMs, duration);
            stat.value.calls++;
            if (stat.lastFrame != frameIndex) { stat.lastFrame = frameIndex; stat.value.frames++; }
            long bytes = name == "GC.Alloc" ? AllocationBytes(view, i) : 0;
            if (bytes > 0)
            {
                stat.value.inclusiveGcBytes += bytes;
                foreach (int ancestor in ancestors) samples[ancestor].value.inclusiveGcBytes += bytes;
                string path = string.Join("/", ancestors.Select(a => names[a]));
                string allocationKey = view.threadName + "\t" + path;
                if (!allocations.TryGetValue(allocationKey, out AllocationSite site))
                    allocations[allocationKey] = site = new AllocationSite { thread = view.threadName, scopePath = path };
                site.bytes += bytes;
                site.allocations++;
            }
            if (ends[i] > i + 1) ancestors.Add(i);
        }
    }
    static Timing Summarize(IEnumerable<double> values)
    {
        double[] sorted = values.OrderBy(v => v).ToArray();
        var timing = new Timing { frames = sorted.Length };
        if (sorted.Length == 0) return timing;
        Func<double, double> percentile = p => sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(sorted.Length * p) - 1)];
        timing.averageMs = sorted.Average(); timing.p50Ms = percentile(.5); timing.p95Ms = percentile(.95);
        timing.p99Ms = percentile(.99); timing.maximumMs = sorted[sorted.Length - 1];
        timing.over16_67Ms = sorted.Count(v => v > 1000.0 / 60); timing.over33_33Ms = sorted.Count(v => v > 1000.0 / 30);
        return timing;
    }
    static void WriteCsv(string directory, List<Frame> frames, List<Hotspot> hotspots, IEnumerable<AllocationSite> sites)
    {
        // Include boundary/failed frames for diagnosis without making them eligible
        // for the validated performance statistics above.
        using (var writer = new StreamWriter(Path.Combine(directory, "observed-spikes.csv")))
        {
            writer.WriteLine("loadedFrameIndex,selected,mainThreadMs,sampleIndex,name,inclusiveMs");
            foreach (var frame in frames)
                foreach (var scope in frame.spikeScopes)
                    writer.WriteLine(string.Join(",", frame.index, frame.selected ? "1" : "0", Number(frame.mainMs), scope.sample, Csv(scope.name), Number(scope.inclusiveMs)));
        }
        // Same-frame correlation only. Inclusive scopes overlap; never sum them.
        using (var writer = new StreamWriter(Path.Combine(directory, "event-frames.csv")))
        {
            writer.WriteLine("loadedFrameIndex,mainThreadMs,sampleIndex,name,inclusiveMs");
            foreach (var frame in frames)
                foreach (var scope in frame.eventScopes)
                    writer.WriteLine(string.Join(",", frame.index, Number(frame.mainMs), scope.sample, Csv(scope.name), Number(scope.inclusiveMs)));
        }
        using (var writer = new StreamWriter(Path.Combine(directory, "frames.csv")))
        {
            writer.WriteLine("loadedFrameIndex,mainThreadMs,renderThreadCpuMs,mainThreadGcBytes,measureMarkers,setupMarkers,cleanupMarkers,selected");
            foreach (var f in frames) writer.WriteLine(string.Join(",", f.index, Number(f.mainMs), Number(f.renderMs), f.gcBytes, f.measure, f.setup, f.cleanup, f.selected));
        }
        using (var writer = new StreamWriter(Path.Combine(directory, "hotspots.csv")))
        {
            writer.WriteLine("thread,name,selfMs,inclusiveMs,maxSingleMs,calls,frames,inclusiveGcBytes,averageSelfMsPerSelectedFrame");
            foreach (var h in hotspots) writer.WriteLine(string.Join(",", Csv(h.thread), Csv(h.name), Number(h.selfMs), Number(h.inclusiveMs), Number(h.maxSingleMs), h.calls, h.frames, h.inclusiveGcBytes, Number(h.averageSelfMsPerSelectedFrame)));
        }
        using (var writer = new StreamWriter(Path.Combine(directory, "allocation-sites.csv")))
        {
            writer.WriteLine("thread,scopePath,bytes,allocations");
            foreach (var a in sites.OrderByDescending(a => a.bytes)) writer.WriteLine(string.Join(",", Csv(a.thread), Csv(a.scopePath), a.bytes, a.allocations));
        }
    }
    static string CaptureMarkdown(CaptureReport report)
    {
        var text = new StringBuilder("# " + Code(report.scenarioName) + "\n\n");
        text.AppendLine($"Validation: **{(report.valid ? "VALID" : "INVALID")}** ({report.validationLevel}). Marker verification: {report.markerVerification}.\n");
        text.AppendLine("Source: `" + Code(report.source) + "`\n");
        if (report.gameLoop != null)
        {
            var loop = report.gameLoop;
            text.AppendLine("## Game-loop execution\n");
            text.AppendLine($"Outcome: **{Code(loop.outcome)}**. Functional validation: {loop.functionalValid}. Wave completion: {loop.combatCompleted}.\n");
            text.AppendLine($"Scope: {Code(loop.scope)}. Input policy: {Code(loop.inputTimeline)}. Demo UI: {loop.showDemoUI}. Diagnostic high health: {loop.diagnosticHighHealth}.\n");
            text.AppendLine($"Enemies spawned: {loop.enemiesSpawned}; configured per wave: {loop.configuredEnemies}; waves started/configured: {loop.wavesStarted}/{loop.configuredWaves}; combinations: {loop.combinations}; throws: {loop.playerThrows}; melee attacks: {loop.meleeAttacks}; enemy damage events: {loop.enemyDamageEvents}; player damage events: {loop.playerDamageEvents}; player distance: {Number(loop.playerDistance)}.\n");
            text.AppendLine($"Effective seed: {loop.effectiveSeed}; potion damage events: {loop.potionDamageEvents}; projectiles launched: {loop.projectilesLaunched}; melee hits: {loop.meleeHits}; dashes: {loop.dashes}; inputs accepted/attempted: {loop.inputAccepted}/{loop.inputAttempts}; peak enemies: {loop.peakEnemies}; lifecycle cycles: {loop.cycles}; enemy distance: {Number(loop.enemyDistance)}; combat completed at: {Number(loop.combatCompletedAt)}. Missing fields in older manifests deserialize as zero; consult the original manifest before interpreting them.\n");
        }
        foreach (string issue in report.issues) text.AppendLine("- INVALID: " + Code(issue));
        foreach (string warning in report.warnings) text.AppendLine("- NOTE: " + Code(warning));
        text.AppendLine($"\nLoaded frames: {report.loadedFrames}; selected interior Measure frames: {report.selectedFrames}; boundary frames excluded: {report.boundaryFramesExcluded}. Expected/actual Measure samples: {report.expectedMeasureSamples}/{report.actualMeasureSamples}.\n");
        var timing = report.measurementMainThreadTiming;
        if (timing != null && timing.frames > 0)
        {
            text.AppendLine("Statistics below describe selected frames. INVALID captures must not be used as performance baselines.\n");
            text.AppendLine("| Main-thread metric | Value |\n| --- | ---: |");
            text.AppendLine($"| Average ms | {Number(timing.averageMs)} |\n| p50 ms | {Number(timing.p50Ms)} |\n| p95 ms | {Number(timing.p95Ms)} |\n| p99 ms | {Number(timing.p99Ms)} |\n| Max ms | {Number(timing.maximumMs)} |\n| Over 16.67 ms | {timing.over16_67Ms} |\n| Over 33.33 ms | {timing.over33_33Ms} |\n| Main-thread GC bytes/frame | {Number(report.mainThreadGcBytesPerSelectedFrame)} |\n");
            text.AppendLine("## Largest CPU scopes\n\nInclusive columns overlap. Self columns exclude immediate child scopes.\n");
            text.AppendLine("| Thread | Scope | Average self ms / selected frame | Calls | Inclusive GC bytes |\n| --- | --- | ---: | ---: | ---: |");
            foreach (var h in report.topHotspots.Take(12)) text.AppendLine($"| {Code(h.thread)} | {Code(h.name).Replace("|", "\\|")} | {Number(h.averageSelfMsPerSelectedFrame)} | {h.calls} | {h.inclusiveGcBytes} |");
        }
        else text.AppendLine("No measurement statistics: this capture contains no qualifying Measure frames.\n");
        text.AppendLine("\nMain-thread timing includes editor work and waits. Render-thread CPU timing is not GPU timing. GC allocations do not establish retained memory or leaks. See frames.csv, hotspots.csv, and allocation-sites.csv for details.\n");
        return text.ToString();
    }
}
