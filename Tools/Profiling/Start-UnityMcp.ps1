[CmdletBinding()]
param([ValidateRange(1024,65535)][int]$Port = 8080)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$logDirectory = Join-Path $repoRoot 'Logs/PerformanceMcp'
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
$url = "http://127.0.0.1:$Port"
$plugin = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Library/PackageCache') -Directory -Filter 'com.coplaydev.unity-mcp@*' | Select-Object -First 1
if (-not $plugin) { throw 'Open this Unity project once so Unity MCP is resolved in Library/PackageCache.' }
$pluginVersion = ([IO.File]::ReadAllText((Join-Path $plugin.FullName 'package.json')) | ConvertFrom-Json).version
if ($pluginVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'This launcher requires a stable Unity MCP package version.' }
$uvxCommand = Get-Command uvx -ErrorAction SilentlyContinue
$uvx = if ($uvxCommand) { $uvxCommand.Source } else { Join-Path $env:USERPROFILE '.local/bin/uvx.exe' }
if (-not (Test-Path -LiteralPath $uvx)) { throw 'uvx is not installed or not on PATH.' }
$running = $false
try {
    $health = Invoke-WebRequest -UseBasicParsing -Uri "$url/health" -TimeoutSec 2
    $running = $health.StatusCode -eq 200
} catch { }
if (-not $running) {
    $telemetryBefore = $env:UNITY_MCP_TELEMETRY_ENABLED
    try {
        $env:UNITY_MCP_TELEMETRY_ENABLED = 'false'
        Start-Process -FilePath $uvx -ArgumentList "--from mcpforunityserver==$pluginVersion mcp-for-unity --transport http --http-url $url --project-scoped-tools" -WindowStyle Hidden -RedirectStandardOutput (Join-Path $logDirectory 'server.out.log') -RedirectStandardError (Join-Path $logDirectory 'server.err.log') | Out-Null
    } finally { $env:UNITY_MCP_TELEMETRY_ENABLED = $telemetryBefore }
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    while (-not $running -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 500
        try { $running = (Invoke-WebRequest -UseBasicParsing -Uri "$url/health" -TimeoutSec 2).StatusCode -eq 200 } catch { }
    }
    if (-not $running) { throw "MCP server did not start. See $logDirectory/server.err.log" }
}
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $logDirectory 'connect.request'), $url, $utf8)
Write-Host "Unity MCP server: $url/mcp"
Write-Host 'Bridge connection requested. Keep this Unity project open.'
Write-Host 'Verify: python Tools/Profiling/Run-PerformanceDemo.py discover'
