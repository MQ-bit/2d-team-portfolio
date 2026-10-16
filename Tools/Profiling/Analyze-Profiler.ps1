[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$CapturePath,
    [string]$OutputDirectory,
    [string]$UnityPath,
    [ValidateRange(2000, 1000000)][int]$MaxFrames = 100000,
    [ValidateRange(30, 3600)][int]$TimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$inputPath = (Resolve-Path -LiteralPath $CapturePath).ProviderPath
$versionText = [IO.File]::ReadAllText((Join-Path $repoRoot 'ProjectSettings/ProjectVersion.txt'))
$editorVersion = [regex]::Match($versionText, 'm_EditorVersion:\s*(\S+)').Groups[1].Value
if (-not $UnityPath) {
    $candidates = @(
        "C:/Program Files/Unity/Hub/Editor/$editorVersion/Editor/Unity.exe",
        "C:/Program Files/Unity Hub/Editor/$editorVersion/Editor/Unity.exe"
    )
    $UnityPath = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity $editorVersion not found. Supply -UnityPath with the Unity.exe path."
}
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).ProviderPath

$manifestPath = ''
$sources = @()
if (Test-Path -LiteralPath $inputPath -PathType Container) {
    $inputDirectory = $inputPath
    if (Test-Path -LiteralPath (Join-Path $inputDirectory 'run.json')) {
        $manifestPath = Join-Path $inputDirectory 'run.json'
    } else {
        $sources = @(Get-ChildItem -LiteralPath $inputDirectory -File | Where-Object { $_.Extension -in '.raw', '.data' } | Sort-Object Name | ForEach-Object { $_.FullName })
    }
} elseif ([IO.Path]::GetFileName($inputPath) -eq 'run.json') {
    $manifestPath = $inputPath
    $inputDirectory = Split-Path -Parent $inputPath
} elseif ([IO.Path]::GetExtension($inputPath) -in '.raw', '.data') {
    $inputDirectory = Split-Path -Parent $inputPath
    $sources = @($inputPath)
    if (Test-Path -LiteralPath (Join-Path $inputDirectory 'run.json')) { $manifestPath = Join-Path $inputDirectory 'run.json' }
} else { throw 'CapturePath must be a .raw, .data, run.json, or capture directory.' }

if ($manifestPath -and $sources.Count -eq 0) {
    $manifest = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
    $directoryPrefix = [IO.Path]::GetFullPath($inputDirectory).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $sources = @($manifest.captures | ForEach-Object {
        if ([string]::IsNullOrWhiteSpace($_.fileName)) { throw 'Manifest capture has an empty fileName.' }
        $sourcePath = [IO.Path]::GetFullPath((Join-Path $inputDirectory $_.fileName))
        if (-not $sourcePath.StartsWith($directoryPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest capture path leaves the run directory.' }
        $sourcePath
    })
}
if ($sources.Count -eq 0) { throw 'No binary captures found. This run may contain only events, or failed before recording.' }

$jobId = (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot "Logs/ProfilerReports/$jobId" }
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory = Join-Path (Get-Location).ProviderPath $OutputDirectory }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $OutputDirectory) -and @(Get-ChildItem -LiteralPath $OutputDirectory -Force).Count -gt 0) {
    throw 'OutputDirectory must be empty; existing reports will not be overwritten.'
}
[IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
$projectDirectory = Join-Path $repoRoot "Logs/ProfilerCliProject/$editorVersion"
[IO.Directory]::CreateDirectory($projectDirectory) | Out-Null
$lockStream = $null
$unityProcess = $null
$exitCode = 1
try {
    try { $lockStream = [IO.File]::Open((Join-Path $projectDirectory 'analysis.lock'), 'OpenOrCreate', 'ReadWrite', 'None') }
    catch { throw 'Another Profiler CLI job is using this analysis project. Wait for it to finish.' }
    foreach ($directory in @('Assets/Editor', 'Packages', 'ProjectSettings')) {
        [IO.Directory]::CreateDirectory((Join-Path $projectDirectory $directory)) | Out-Null
    }
    $utf8 = New-Object Text.UTF8Encoding($false)
    $analyzerSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Editor/ProfilerCaptureAnalyzer.cs'))
    $analyzerTarget = Join-Path $projectDirectory 'Assets/Editor/ProfilerCaptureAnalyzer.cs'
    if (-not (Test-Path -LiteralPath $analyzerTarget) -or [IO.File]::ReadAllText($analyzerTarget) -ne $analyzerSource) {
        [IO.File]::WriteAllText($analyzerTarget, $analyzerSource, $utf8)
    }
    [IO.File]::WriteAllText((Join-Path $projectDirectory 'Packages/manifest.json'), '{"dependencies":{"com.unity.modules.jsonserialize":"1.0.0"}}', $utf8)
    [IO.File]::WriteAllText((Join-Path $projectDirectory 'ProjectSettings/ProjectVersion.txt'), "m_EditorVersion: $editorVersion`n", $utf8)
    $settingsFile = Join-Path $projectDirectory 'ProjectSettings/ProjectSettings.asset'
    if (-not (Test-Path -LiteralPath $settingsFile)) {
        [IO.File]::WriteAllText($settingsFile, "%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!129 &1`nPlayerSettings:`n  m_ObjectHideFlags: 0`n  serializedVersion: 28`n  companyName: LocalProfilerTools`n  productName: ProfilerCli`n", $utf8)
    }
    $requestPath = Join-Path $OutputDirectory 'request.json'
    $request = @{ sources = [string[]]$sources; manifestPath = $manifestPath; outputDirectory = $OutputDirectory; maxFrames = $MaxFrames }
    [IO.File]::WriteAllText($requestPath, ($request | ConvertTo-Json -Depth 4), $utf8)
    $logPath = Join-Path $OutputDirectory 'unity.log'
    # Windows filenames cannot contain a double quote. Pass quoted arguments
    # directly to CreateProcess; no shell parses input paths as commands.
    $arguments = '-batchmode -nographics -projectPath "' + $projectDirectory + '" -executeMethod ProfilerCaptureAnalyzer.Run -profilerAnalysisRequest "' + $requestPath + '" -logFile "' + $logPath + '"'
    Write-Host "Analyzing $($sources.Count) capture(s). Report: $OutputDirectory"
    $unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (-not $unityProcess.WaitForExit(1000)) {
        if ([DateTime]::UtcNow -gt $deadline) { $unityProcess.Kill(); $unityProcess.WaitForExit(); throw "Analysis timed out. See $logPath" }
    }
    $unityProcess.Refresh()
    $exitCode = $unityProcess.ExitCode
    $reportPath = Join-Path $OutputDirectory 'summary.json'
    if (-not (Test-Path -LiteralPath $reportPath)) { throw "Unity did not create summary.json (exit $exitCode). See $logPath" }
    $summary = [IO.File]::ReadAllText($reportPath) | ConvertFrom-Json
    # Use the finished report as the portable validation outcome; Unity versions
    # can normalize nonzero EditorApplication.Exit codes.
    $exitCode = if ($summary.invalidCaptures -gt 0) { 2 } else { 0 }
    Write-Host "Valid: $($summary.validCaptures), invalid: $($summary.invalidCaptures)"
    Write-Host "Report: $(Join-Path $OutputDirectory 'report.md')"
    Write-Host "JSON:   $reportPath"
} finally {
    if ($lockStream) { $lockStream.Dispose() }
}
exit $exitCode
