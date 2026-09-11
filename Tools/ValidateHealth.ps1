param(
    [Parameter(Mandatory=$true)][string]$UnityEditor
)
$ErrorActionPreference='Stop'
$systemsRoot=Split-Path -Parent $PSScriptRoot
$labProject=Join-Path $systemsRoot 'Projects/HealthLab'
$artifactsRoot=Join-Path $systemsRoot 'Artifacts'
New-Item -ItemType Directory -Force $artifactsRoot | Out-Null
foreach($platform in @('EditMode','PlayMode')) {
    $resultPath=Join-Path $artifactsRoot ($platform.ToLower()+'.xml')
    $logPath=Join-Path $artifactsRoot ($platform.ToLower()+'.log')
    $arguments="-batchmode -nographics -projectPath `"$labProject`" -runTests -testPlatform $platform -testResults `"$resultPath`" -logFile `"$logPath`""
    $run=Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $run.WaitForExit()
    if($run.ExitCode -ne 0){throw "$platform failed: $($run.ExitCode)"}
    [xml]$results=Get-Content -LiteralPath $resultPath
    if($results.'test-run'.result -ne 'Passed'){throw "$platform did not pass"}
    Write-Output "$platform passed: $($results.'test-run'.passed)"
}
$buildLog=Join-Path $artifactsRoot 'build.log'
$build=Start-Process -FilePath $UnityEditor -ArgumentList "-batchmode -nographics -quit -projectPath `"$labProject`" -executeMethod HealthLabBuild.Build -logFile `"$buildLog`"" -WindowStyle Hidden -PassThru
$build.WaitForExit()
if($build.ExitCode -ne 0){throw "Build failed: $($build.ExitCode)"}
$player=Join-Path $artifactsRoot 'Build/HealthLab.exe'
$smokeLog=Join-Path $artifactsRoot 'standalone-smoke.log'
$smoke=Start-Process -FilePath $player -ArgumentList "-batchmode -nographics --health-smoke -logFile `"$smokeLog`"" -WindowStyle Hidden -PassThru
$smoke.WaitForExit()
if($smoke.ExitCode -ne 0 -or -not (Select-String -LiteralPath $smokeLog -Pattern 'HEALTH_SMOKE_PASS' -Quiet)){throw 'Standalone smoke failed'}
Write-Output 'Health validation passed.'
