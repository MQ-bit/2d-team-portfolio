using System;
using System.IO;
using MCPForUnity.Editor.Services;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// A local one-shot connection request lets the setup script connect the already
// open editor without UI automation or restarting it. No remote code execution.
[InitializeOnLoad]
public static class PerformanceDemoMcpBootstrap
{
    static readonly string DirectoryPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PerformanceMcp"));
    static double _nextProbe;
    static bool _connecting;
    static PerformanceDemoMcpBootstrap() { EditorApplication.update += Probe; }
    static void Probe()
    {
        if (_connecting || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < _nextProbe) return;
        _nextProbe = EditorApplication.timeSinceStartup + 1;
        string request = Path.Combine(DirectoryPath, "connect.request");
        if (!File.Exists(request)) return;
        string url = File.ReadAllText(request).Trim();
        File.Delete(request);
        Connect(url);
    }
    [MenuItem("Tools/Performance/Connect MCP Bridge")]
    public static void ConnectFromMenu() => Connect("http://127.0.0.1:8080");
    static async void Connect(string url)
    {
        _connecting = true;
        try
        {
            var endpoint = new Uri(url);
            if (!endpoint.IsLoopback || endpoint.Scheme != "http") throw new ArgumentException("Only a local HTTP MCP endpoint is supported.");
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
            EditorPrefs.SetString("MCPForUnity.HttpUrl", url);
            EditorConfigurationCache.Instance.Refresh();
            MCPServiceLocator.ToolDiscovery.InvalidateCache();
            MCPServiceLocator.ToolDiscovery.SetToolEnabled("performance_demo", true);
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, "connect.result.json"), JsonConvert.SerializeObject(new
            { success = connected, url, project = Application.dataPath, utc = DateTime.UtcNow.ToString("O") }, Formatting.Indented));
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, "connect.result.json"), JsonConvert.SerializeObject(new { success = false, error = exception.Message }, Formatting.Indented));
        }
        finally { _connecting = false; }
    }
}
