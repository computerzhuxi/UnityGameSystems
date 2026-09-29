param(
    [string]$UnityEditor,
    [ValidateSet('EditMode', 'PlayMode')][string]$Platform = 'EditMode',
    [string]$TestFilter,
    [switch]$MirrorOnly,
    [switch]$Full,
    [ValidateRange(1, 86400)][int]$TimeoutSeconds = 1200,
    [ValidateRange(1, 86400)][int]$SmokeTimeoutSeconds = 60
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/ValidationCommon.ps1"
$root = Split-Path -Parent $PSScriptRoot
$lab = Join-Path $root 'Projects/StatsLab'
$source = Join-Path $root 'Packages/com.computerzhuxi.stats/Samples~/BasicStatsDemo'
$mirror = Join-Path $lab 'Assets/BasicStatsDemo'
# 只核对权威 Sample 源码及程序集；Lab 场景、Prefab 和定义资产另作真实入口验证。
Assert-ValidationMirrorFiles -PackageRoot $source -LabRoot $mirror -Files @(
    'BasicStatsDemo.cs', 'SampleCharacterStats.cs', 'Computerzhuxi.Stats.Samples.asmdef')
if ($MirrorOnly) {
    if ($Full -or $TestFilter) { throw 'MirrorOnly cannot be combined with Full or TestFilter.' }
    Write-Output 'Stats Sample mirror passed.'
    return
}
if ($Full -and $TestFilter) { throw 'Full cannot be combined with TestFilter.' }
if (-not $Full -and -not $TestFilter) { throw 'Specify TestFilter for a targeted run, Full, or MirrorOnly.' }
if (-not $UnityEditor) { throw 'UnityEditor is required for test runs.' }
$logs = New-ValidationRunRoot -Root $root -System 'Stats'
$platforms = if ($Full) { @('EditMode', 'PlayMode') } else { @($Platform) }
foreach ($testPlatform in $platforms) {
    $invoke = @{ UnityEditor = $UnityEditor; Project = $lab; Platform = $testPlatform;
        LogRoot = $logs; TimeoutSeconds = $TimeoutSeconds }
    if ($TestFilter) { $invoke.TestFilter = $TestFilter }
    & "$PSScriptRoot/InvokeUnityValidation.ps1" @invoke
}
if ($Full) {
    & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $lab `
        -Method 'StatsLabBuild.Build' -LogRoot $logs -TimeoutSeconds $TimeoutSeconds
    $player = Join-Path $root 'Artifacts/Build/StatsLab.exe'
    if (-not (Test-Path -LiteralPath $player -PathType Leaf)) { throw "Build output missing: $player" }
    $smokeLog = Join-Path $logs 'standalone-smoke.log'
    $arguments = "-batchmode -nographics --stats-smoke -logFile `"$smokeLog`""
    Invoke-ValidationProcess -FilePath $player -Arguments $arguments -LogPath $smokeLog `
        -ResultPath (Join-Path $logs 'standalone-smoke.result.json') -TimeoutSeconds $SmokeTimeoutSeconds
    Assert-ValidationMarker -LogPath $smokeLog -Marker 'STATS_SMOKE_PASS'
}
Write-Output "Stats evidence: $logs"
