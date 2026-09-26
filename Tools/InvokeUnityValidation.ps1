param(
    [Parameter(Mandatory)][string]$UnityEditor,
    [Parameter(Mandatory)][string]$Project,
    [ValidateSet('EditMode', 'PlayMode')][string]$Platform,
    [string]$Method,
    [string]$TestFilter,
    [Parameter(Mandatory)][string]$LogRoot,
    [ValidateRange(1, 86400)][int]$TimeoutSeconds = 1200
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/ValidationCommon.ps1"
if ([bool]$Platform -eq [bool]$Method) { throw 'Specify exactly one of Platform or Method.' }
if ($Method -and $TestFilter) { throw 'TestFilter requires Platform.' }
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity editor missing: $UnityEditor" }
if (-not (Test-Path -LiteralPath $Project -PathType Container)) { throw "Unity project missing: $Project" }
$name = if ($Platform) { $Platform } else { $Method }
$log = Join-Path $LogRoot "$name.log"
$result = Join-Path $LogRoot "$name.result.json"
$xmlPath = Join-Path $LogRoot "$name.xml"
if ($Platform -and (Test-Path -LiteralPath $xmlPath)) { throw "Refusing to overwrite evidence: $xmlPath" }
$arguments = "-batchmode -nographics -projectPath `"$Project`" -logFile `"$log`""
if ($Platform) {
    $arguments += " -runTests -testPlatform $Platform -testResults `"$xmlPath`""
    if ($TestFilter) { $arguments += " -testFilter `"$TestFilter`"" }
} else {
    $arguments += " -quit -executeMethod $Method"
}
Invoke-ValidationProcess -FilePath $UnityEditor -Arguments $arguments -LogPath $log -ResultPath $result -TimeoutSeconds $TimeoutSeconds
if ($Platform) {
    if (-not (Test-Path -LiteralPath $xmlPath -PathType Leaf)) { throw "Missing test XML: $xmlPath" }
    [xml]$xml = Get-Content -LiteralPath $xmlPath -Raw
    $run = $xml.'test-run'
    if ($null -eq $run -or $run.result -ne 'Passed' -or [int]$run.total -le 0 -or
        [int]$run.passed -le 0 -or [int]$run.failed -ne 0) {
        throw "No valid passing test run: $xmlPath"
    }
    Write-Output "$Project $Platform passed=$($run.passed) filter=$TestFilter"
} else {
    Write-Output "$Project $Method completed"
}
