param([Parameter(Mandatory)][string]$UnityEditor,[string]$ArpgProject)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$lab=Join-Path $root 'Projects/Navigation2DLab'
foreach($sampleName in @('BasicNavigation2D','AgentNavigation2D','CrowdNavigation2D','QuickStartNavigation2D')){
    $sample=Join-Path $root "Packages/com.computerzhuxi.navigation2d/Samples~/$sampleName"
    $mirrorRoot=Join-Path $lab "Assets/Samples/$sampleName"
    $files=@(Get-ChildItem -LiteralPath $sample -File -Recurse)
    $mirrors=@(Get-ChildItem -LiteralPath $mirrorRoot -File -Recurse)
    if($files.Count -ne $mirrors.Count){throw "Sample mirror file count drift: $sampleName"}
    foreach($file in $files){
        $relative=$file.FullName.Substring($sample.Length).TrimStart('\','/')
        $mirror=Join-Path $mirrorRoot $relative
        if(-not(Test-Path -LiteralPath $mirror) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $mirror).Hash){throw "Sample mirror drift: $sampleName/$relative"}
    }
}
$logs=Join-Path $root ('Artifacts/Logs/Navigation2D/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
foreach($platform in @('EditMode','PlayMode')){& "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $lab -Platform $platform -LogRoot "$logs/Lab"}
# 防止条件编译误排除示例测试后，仅凭较小的绿色总数宣布通过。
[xml]$labPlay=Get-Content "$logs/Lab/PlayMode.xml"
foreach($required in @('QuickStartExampleTests.ExampleDisabledAndRemoved_NavigationContinues','NavigatorOwnershipTests.RuntimeAssembly_HasNoMoverOrSampleDependency')){
    if(-not $labPlay.SelectSingleNode("//test-case[contains(@fullname, '$required') and @result='Passed']")){throw "Missing passing boundary test: $required"}
}
if(Select-String -Path "$logs/Lab/*.log" -Pattern 'ExpressionNotValidException' -Quiet){throw 'Invalid assembly version expression'}
foreach($method in @('NavigationLabBuild.Build','NavigationLabBuild.BuildAgent','NavigationLabBuild.BuildCrowd','NavigationQuickStartBuild.Build')){
    & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $lab -Method $method -LogRoot "$logs/Lab"
}
foreach($variant in @(@{Name='Navigation2DLab';Flag='--navigation-smoke';Marker='NAVIGATION_SMOKE_PASS'},@{Name='NavigationAgentLab';Flag='--agent-smoke';Marker='NAVIGATION_AGENT_SMOKE_PASS'},@{Name='NavigationCrowdLab';Flag='--crowd-smoke';Marker='NAVIGATION_CROWD_SMOKE_PASS'},@{Name='NavigationQuickStartLab';Flag='--quickstart-smoke';Marker='NAVIGATION_QUICKSTART_SMOKE_PASS'})){
    $player=Join-Path $root "Artifacts/Logs/Navigation2D/Build/$($variant.Name).exe"
    $smokeLog="$logs/Lab/$($variant.Name)-smoke.log"
    $smoke=Start-Process $player -WorkingDirectory (Split-Path -Parent $player) -ArgumentList "-batchmode -nographics $($variant.Flag) -logFile `"$smokeLog`"" -WindowStyle Hidden -PassThru
    if(-not $smoke.WaitForExit(60000)){$smoke.Kill();throw "Standalone smoke timed out: $($variant.Name)"}
    if($smoke.ExitCode -ne 0 -or -not(Select-String -LiteralPath $smokeLog -Pattern $variant.Marker -Quiet)){throw "Standalone smoke failed: $($variant.Name)"}
}
if($ArpgProject){
    foreach($platform in @('EditMode','PlayMode')){& "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $ArpgProject -Platform $platform -LogRoot "$logs/ARPG"}
    & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $ArpgProject -Method NavigationValidation.Audit -LogRoot "$logs/ARPG"
    & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $ArpgProject -Method NavigationValidation.Build -LogRoot "$logs/ARPG"
}
Write-Output "Evidence: $logs. Manual visual acceptance is still required."
