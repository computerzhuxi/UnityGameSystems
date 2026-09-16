param(
    [Parameter(Mandatory)][string]$UnityEditor,
    [Parameter(Mandatory)][string]$Project,
    [ValidateSet('EditMode','PlayMode')][string]$Platform,
    [string]$Method,
    [Parameter(Mandatory)][string]$LogRoot
)
$ErrorActionPreference='Stop'
if([bool]$Platform -eq [bool]$Method){throw 'Specify exactly one of Platform or Method.'}
New-Item -ItemType Directory -Force $LogRoot | Out-Null
$name=if($Platform){$Platform}else{$Method}
$log=Join-Path $LogRoot "$name.log"
if(Test-Path $log){throw "Refusing to overwrite evidence: $log"}
$arguments="-batchmode -nographics -projectPath `"$Project`" -logFile `"$log`""
if($Platform){$arguments+=" -runTests -testPlatform $Platform -testResults `"$LogRoot/$name.xml`""}else{$arguments+=" -quit -executeMethod $Method"}
$run=Start-Process $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$run.WaitForExit()
@{exitCode=$run.ExitCode;project=$Project;platform=$Platform;method=$Method;finished=(Get-Date -Format o)} | ConvertTo-Json | Set-Content "$LogRoot/$name.result.json"
if($run.ExitCode -ne 0){throw "Unity failed ($($run.ExitCode)): $log"}
if($Platform){
    [xml]$xml=Get-Content "$LogRoot/$name.xml"
    if($xml.'test-run'.result -ne 'Passed' -or [int]$xml.'test-run'.total -le 0 -or [int]$xml.'test-run'.failed -ne 0){throw "No valid passing test run: $log"}
    Write-Output "$Project $Platform passed=$($xml.'test-run'.passed)"
}else{Write-Output "$Project $Method completed"}
