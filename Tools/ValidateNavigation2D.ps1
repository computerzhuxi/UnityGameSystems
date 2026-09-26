param(
    [string]$UnityEditor,
    [ValidateSet('EditMode', 'PlayMode')][string]$Platform = 'EditMode',
    [string]$TestFilter,
    [switch]$MirrorOnly,
    [switch]$Full,
    [string]$ArpgProject,
    [ValidateRange(1, 86400)][int]$TimeoutSeconds = 1200,
    [ValidateRange(1, 86400)][int]$SmokeTimeoutSeconds = 60
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/ValidationCommon.ps1"
$root = Split-Path -Parent $PSScriptRoot
$lab = Join-Path $root 'Projects/Navigation2DLab'
# Navigation 的四套 Sample 由包全树镜像到 Lab，保留文件集合及内容双重检查。
foreach ($sampleName in @('BasicNavigation2D', 'AgentNavigation2D', 'CrowdNavigation2D', 'QuickStartNavigation2D')) {
    $source = Join-Path $root "Packages/com.computerzhuxi.navigation2d/Samples~/$sampleName"
    $mirror = Join-Path $lab "Assets/Samples/$sampleName"
    Assert-ValidationMirrorTree -PackageRoot $source -LabRoot $mirror
}
if ($MirrorOnly) {
    if ($Full -or $TestFilter -or $ArpgProject) { throw 'MirrorOnly cannot be combined with Full, TestFilter or ArpgProject.' }
    Write-Output 'Navigation 2D Sample mirrors passed.'
    return
}
if ($Full -and $TestFilter) { throw 'Full cannot be combined with TestFilter.' }
if (-not $Full -and -not $TestFilter) { throw 'Specify TestFilter for a targeted run, Full, or MirrorOnly.' }
if ($ArpgProject -and -not $Full) { throw 'ArpgProject requires Full.' }
if (-not $UnityEditor) { throw 'UnityEditor is required for test runs.' }
$logs = New-ValidationRunRoot -Root $root -System 'Navigation2D'
$platforms = if ($Full) { @('EditMode', 'PlayMode') } else { @($Platform) }
foreach ($testPlatform in $platforms) {
    $invoke = @{ UnityEditor = $UnityEditor; Project = $lab; Platform = $testPlatform;
        LogRoot = (Join-Path $logs 'Lab'); TimeoutSeconds = $TimeoutSeconds }
    if ($TestFilter) { $invoke.TestFilter = $TestFilter }
    & "$PSScriptRoot/InvokeUnityValidation.ps1" @invoke
}
if ($Full) {
    # 全量才要求专属边界测试存在；定向测试可以只运行当前改动的测试。
    [xml]$labPlay = Get-Content -LiteralPath (Join-Path $logs 'Lab/PlayMode.xml') -Raw
    foreach ($required in @('QuickStartExampleTests.ExampleDisabledAndRemoved_NavigationContinues',
                            'NavigatorOwnershipTests.RuntimeAssembly_HasNoMoverOrSampleDependency')) {
        if (-not $labPlay.SelectSingleNode("//test-case[contains(@fullname, '$required') and @result='Passed']")) {
            throw "Missing passing boundary test: $required"
        }
    }
    if (Select-String -Path (Join-Path $logs 'Lab/*.log') -Pattern 'ExpressionNotValidException' -Quiet) {
        throw 'Invalid assembly version expression'
    }
    foreach ($method in @('NavigationLabBuild.Build', 'NavigationLabBuild.BuildAgent',
                          'NavigationLabBuild.BuildCrowd', 'NavigationQuickStartBuild.Build')) {
        & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $lab `
            -Method $method -LogRoot (Join-Path $logs 'Lab') -TimeoutSeconds $TimeoutSeconds
    }
    foreach ($variant in @(
        @{ Name = 'Navigation2DLab'; Flag = '--navigation-smoke'; Marker = 'NAVIGATION_SMOKE_PASS' },
        @{ Name = 'NavigationAgentLab'; Flag = '--agent-smoke'; Marker = 'NAVIGATION_AGENT_SMOKE_PASS' },
        @{ Name = 'NavigationCrowdLab'; Flag = '--crowd-smoke'; Marker = 'NAVIGATION_CROWD_SMOKE_PASS' },
        @{ Name = 'NavigationQuickStartLab'; Flag = '--quickstart-smoke'; Marker = 'NAVIGATION_QUICKSTART_SMOKE_PASS' })) {
        $player = Join-Path $root "Artifacts/Logs/Navigation2D/Build/$($variant.Name).exe"
        if (-not (Test-Path -LiteralPath $player -PathType Leaf)) { throw "Build output missing: $player" }
        $smokeLog = Join-Path $logs "Lab/$($variant.Name)-smoke.log"
        $arguments = "-batchmode -nographics $($variant.Flag) -logFile `"$smokeLog`""
        Invoke-ValidationProcess -FilePath $player -Arguments $arguments -LogPath $smokeLog `
            -ResultPath (Join-Path $logs "Lab/$($variant.Name)-smoke.result.json") `
            -TimeoutSeconds $SmokeTimeoutSeconds -WorkingDirectory (Split-Path -Parent $player)
        Assert-ValidationMarker -LogPath $smokeLog -Marker $variant.Marker
    }
    if ($ArpgProject) {
        foreach ($testPlatform in @('EditMode', 'PlayMode')) {
            & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $ArpgProject `
                -Platform $testPlatform -LogRoot (Join-Path $logs 'ARPG') -TimeoutSeconds $TimeoutSeconds
        }
        foreach ($method in @('NavigationValidation.Audit', 'NavigationValidation.Build')) {
            & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $ArpgProject `
                -Method $method -LogRoot (Join-Path $logs 'ARPG') -TimeoutSeconds $TimeoutSeconds
        }
    }
}
Write-Output "Navigation 2D evidence: $logs. Manual visual acceptance is still required."
