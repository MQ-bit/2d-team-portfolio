using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

// Batch-mode Development Build of the measurement scene only:
// Unity.exe -batchmode -quit -projectPath . -executeMethod OptimizationBuild.Build -optimizationBuildOutput <exe path>
public static class OptimizationBuild
{
    public static void Build()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-optimizationBuildOutput");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("-optimizationBuildOutput <exe path> is required.");
        string output = Path.GetFullPath(args[index + 1]);
        if (File.Exists(output)) throw new IOException("Build output already exists; use a new folder: " + output);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { PerformanceDemoSceneBuilder.ScenePath },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };
        BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
        if (summary.result != BuildResult.Succeeded)
            throw new Exception("Build " + summary.result + ": " + summary.totalErrors + " errors.");
        UnityEngine.Debug.Log("Optimization build succeeded: " + output);
    }
}
