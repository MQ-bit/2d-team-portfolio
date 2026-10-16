using System;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

// Owns only the binary recording started by this instance. Does not replace an
// existing binary recording or depend on the Profiler window's history buffer.
public sealed class PerformanceDemoCapture : IDisposable
{
    [Serializable]
    public sealed class Result
    {
        public string scenarioName, fileName, status, failure;
        public string markerVerification = "PendingOfflineAnalysis";
        public int repetition, startFrame, endFrame, measureSamples;
        public double startRealtime, endRealtime;
        public long fileBytes;
    }

    public Result Current { get; private set; }
    public bool IsRecording { get; private set; }
    private bool _previousEnabled;
    private string _previousLogFile;
    private string _path;

    public Result Begin(string directory, string scenarioName, int repetition)
    {
#if !ENABLE_PROFILER
        throw new NotSupportedException("Profiler capture requires the Unity Editor or a Development Build.");
#else
        if (IsRecording) throw new InvalidOperationException("A demo capture is already active.");
        if (Profiler.enableBinaryLog)
            throw new InvalidOperationException("Another binary Profiler recording is active. Stop it before running the demo.");
        Directory.CreateDirectory(directory);
        string safeName = scenarioName;
        foreach (char invalid in Path.GetInvalidFileNameChars()) safeName = safeName.Replace(invalid, '_');
        _path = Path.Combine(directory, $"{safeName}_repeat{repetition:00}.raw");
        if (File.Exists(_path)) throw new IOException("Capture already exists: " + _path);
        Current = new Result
        {
            scenarioName = scenarioName,
            repetition = repetition,
            fileName = Path.GetFileName(_path),
            status = "Recording",
            startFrame = Time.frameCount,
            startRealtime = Time.realtimeSinceStartupAsDouble
        };
        _previousEnabled = Profiler.enabled;
        _previousLogFile = Profiler.logFile;
        IsRecording = true;
        try
        {
            Profiler.enabled = false;
            Profiler.logFile = _path;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            return Current;
        }
        catch
        {
            End("Failed", "Could not start binary Profiler recording.");
            throw;
        }
#endif
    }

    public void MarkMeasurementSample()
    {
        if (IsRecording) Current.measureSamples++;
    }

    public Result End(string status, string failure)
    {
        if (!IsRecording) return Current;
        // Close the log before checking its size or beginning scene cleanup.
        try
        {
            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.logFile = string.Empty;
        }
        finally
        {
            IsRecording = false;
            Profiler.logFile = _previousLogFile;
            Profiler.enabled = _previousEnabled;
            Current.endFrame = Time.frameCount;
            Current.endRealtime = Time.realtimeSinceStartupAsDouble;
            Current.status = status;
            Current.failure = failure;
        }
        Current.fileBytes = File.Exists(_path) ? new FileInfo(_path).Length : 0;
        if (Current.status == "Completed" && (Current.fileBytes == 0 || Current.measureSamples == 0))
        {
            Current.status = "Failed";
            Current.failure = "Capture has no file data or no measurement samples.";
        }
        return Current;
    }

    public void Dispose() => End("Stopped", "Capture owner was disabled or destroyed.");
}

// Uses real time so Editor pauses/suspension cannot silently turn a 20-second
// measurement into a much longer valid interval. Short gameplay hit stops remain allowed.
public sealed class PerformanceDemoTimingGuard
{
    private readonly double _start, _duration, _maxGap;
    private double _previous, _pausedSince = -1;
    public PerformanceDemoTimingGuard(double start, double duration, double maxGap)
    {
        _start = _previous = start;
        _duration = duration;
        _maxGap = maxGap;
    }
    public string Observe(double now, float timeScale)
    {
        double gap = now - _previous;
        _previous = now;
        if (gap > _maxGap) return $"Frame/update gap {gap:F3}s exceeds {_maxGap:F3}s; interval is invalid.";
        if (timeScale == 0)
        {
            if (_pausedSince < 0) _pausedSince = now;
            if (now - _pausedSince > _maxGap) return "Continuous timeScale=0 pause exceeded the allowed interval.";
        }
        else
        {
            _pausedSince = -1;
        }

        return null;
    }
    public string CompletionError(double now) => now - _start > _duration + _maxGap
        ? $"Interval overran: expected {_duration:F3}s, observed {now - _start:F3}s." : null;
}
