param([Parameter(Mandatory)][string]$UnityEditor,[string]$ArpgProject)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$lab=Join-Path $root 'Projects/Navigation2DLab'
$sample=Join-Path $root 'Packages/com.computerzhuxi.navigation2d/Samples~/BasicNavigation2D'
foreach($file in Get-ChildItem -LiteralPath $sample -File -Recurse){
    $relative=$file.FullName.Substring($sample.Length).TrimStart('\','/')
    $mirror=Join-Path "$lab/Assets/Samples/BasicNavigation2D" $relative
    if(-not(Test-Path $mirror) -or (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $mirror).Hash){throw "Sample mirror drift: $relative"}
}
$logs=Join-Path $root ('Artifacts/Logs/Navigation2D/'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
foreach($platform in @('EditMode','PlayMode')){& "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $lab -Platform $platform -LogRoot "$logs/Lab"}
& "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $lab -Method NavigationLabBuild.Build -LogRoot "$logs/Lab"
$player=Join-Path $root 'Artifacts/Logs/Navigation2D/Build/Navigation2DLab.exe'
$smoke=Start-Process $player -ArgumentList "-batchmode -nographics --navigation-smoke -logFile `"$logs/Lab/smoke.log`"" -WindowStyle Hidden -PassThru
$smoke.WaitForExit()
if($smoke.ExitCode -ne 0 -or -not(Select-String "$logs/Lab/smoke.log" -Pattern 'NAVIGATION_SMOKE_PASS' -Quiet)){throw 'Standalone smoke failed'}
if($ArpgProject){
    foreach($platform in @('EditMode','PlayMode')){& "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $ArpgProject -Platform $platform -LogRoot "$logs/ARPG"}
    & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $ArpgProject -Method NavigationValidation.Audit -LogRoot "$logs/ARPG"
    & "$PSScriptRoot/InvokeUnityValidation.ps1" -UnityEditor $UnityEditor -Project $ArpgProject -Method NavigationValidation.Build -LogRoot "$logs/ARPG"
}
Write-Output "Evidence: $logs. Manual visual acceptance is still required."
