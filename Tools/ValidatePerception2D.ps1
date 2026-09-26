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
$lab = Join-Path $root 'Projects/Perception2DLab'
$source = Join-Path $root 'Packages/com.computerzhuxi.perception2d/Samples~/BasicPerception2D'
$mirror = Join-Path $lab 'Assets/BasicPerception2D'
# Lab 场景由 Lab 拥有；只比较其镜像的 Sample 脚本与程序集定义。
Assert-ValidationMirrorFiles -PackageRoot $source -LabRoot $mirror -Files @(
    'BasicPerceptionDemo.cs', 'Computerzhuxi.Perception2D.Sample.asmdef')
if ($MirrorOnly) {
    if ($Full -or $TestFilter) { throw 'MirrorOnly cannot be combined with Full or TestFilter.' }
    Write-Output 'Perception 2D Sample mirror passed.'
    return
}
if ($Full -and $TestFilter) { throw 'Full cannot be combined with TestFilter.' }
if (-not $Full -and -not $TestFilter) { throw 'Specify TestFilter for a targeted run, Full, or MirrorOnly.' }
if (-not $UnityEditor) { throw 'UnityEditor is required for test runs.' }
$logs = New-ValidationRunRoot -Root $root -System 'Perception2D'
$platforms = if ($Full) { @('EditMode', 'PlayMode') } else { @($Platform) }
foreach ($testPlatform in $platforms) {
    $invoke = @{ UnityEditor = $UnityEditor; Project = $lab; Platform = $testPlatform;
        LogRoot = $logs; TimeoutSeconds = $TimeoutSeconds }
    if ($TestFilter) { $invoke.TestFilter = $TestFilter }
    & "$PSScriptRoot/InvokeUnityValidation.ps1" @invoke
}
if ($Full) {
    & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $lab `
        -Method 'PerceptionLabBuild.Build' -LogRoot $logs -TimeoutSeconds $TimeoutSeconds
    $player = Join-Path $root 'Artifacts/Perception2D/Build/Perception2DLab.exe'
    if (-not (Test-Path -LiteralPath $player -PathType Leaf)) { throw "Build output missing: $player" }
    $smokeLog = Join-Path $logs 'standalone-smoke.log'
    $arguments = "-batchmode -nographics --perception-smoke -logFile `"$smokeLog`""
    Invoke-ValidationProcess -FilePath $player -Arguments $arguments -LogPath $smokeLog `
        -ResultPath (Join-Path $logs 'standalone-smoke.result.json') -TimeoutSeconds $SmokeTimeoutSeconds
    Assert-ValidationMarker -LogPath $smokeLog -Marker 'PERCEPTION_SMOKE_PASS'
}
Write-Output "Perception 2D evidence: $logs"
