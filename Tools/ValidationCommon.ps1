$ErrorActionPreference = 'Stop'

# 为每轮 Unity 验证创建独立证据目录，避免复用或覆盖旧结果。
function New-ValidationRunRoot {
    param([Parameter(Mandatory)][string]$Root, [Parameter(Mandatory)][string]$System)
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $suffix = [guid]::NewGuid().ToString('N').Substring(0, 8)
    $path = Join-Path $Root "Artifacts/Logs/$System/$stamp-$suffix"
    New-Item -ItemType Directory -Path $path -ErrorAction Stop | Out-Null
    return $path
}

# 比较指定文件的内容；由调用者明确列出真正需要镜像的文件。
function Assert-ValidationMirrorFiles {
    param([Parameter(Mandatory)][string]$PackageRoot,
          [Parameter(Mandatory)][string]$LabRoot,
          [Parameter(Mandatory)][string[]]$Files)
    foreach ($relative in $Files) {
        $source = Join-Path $PackageRoot $relative
        $mirror = Join-Path $LabRoot $relative
        if (-not (Test-Path -LiteralPath $source -PathType Leaf) -or
            -not (Test-Path -LiteralPath $mirror -PathType Leaf)) {
            throw "Sample mirror missing: $relative"
        }
        if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $mirror).Hash) {
            throw "Sample mirror drift: $relative"
        }
    }
}

# 对确实全树镜像的 Sample 比较文件集合及每个文件的哈希。
function Assert-ValidationMirrorTree {
    param([Parameter(Mandatory)][string]$PackageRoot,
          [Parameter(Mandatory)][string]$LabRoot)
    if (-not (Test-Path -LiteralPath $PackageRoot -PathType Container) -or
        -not (Test-Path -LiteralPath $LabRoot -PathType Container)) {
        throw "Sample mirror directory missing: $PackageRoot / $LabRoot"
    }
    $sourceFiles = @(Get-ChildItem -LiteralPath $PackageRoot -File -Recurse |
        ForEach-Object { $_.FullName.Substring($PackageRoot.Length).TrimStart('\', '/') } | Sort-Object)
    $mirrorFiles = @(Get-ChildItem -LiteralPath $LabRoot -File -Recurse |
        ForEach-Object { $_.FullName.Substring($LabRoot.Length).TrimStart('\', '/') } | Sort-Object)
    if (($sourceFiles -join "`n") -ne ($mirrorFiles -join "`n")) {
        throw "Sample mirror file set drift: $PackageRoot"
    }
    Assert-ValidationMirrorFiles -PackageRoot $PackageRoot -LabRoot $LabRoot -Files $sourceFiles
}

# 启动受限时长的进程，并拒绝覆盖该步骤已有的日志与结果。
function Invoke-ValidationProcess {
    param([Parameter(Mandatory)][string]$FilePath,
          [Parameter(Mandatory)][string]$Arguments,
          [Parameter(Mandatory)][string]$LogPath,
          [Parameter(Mandatory)][string]$ResultPath,
          [Parameter(Mandatory)][int]$TimeoutSeconds,
          [string]$WorkingDirectory)
    if ($TimeoutSeconds -le 0) { throw 'TimeoutSeconds must be positive.' }
    foreach ($path in @($LogPath, $ResultPath)) {
        if (Test-Path -LiteralPath $path) { throw "Refusing to overwrite evidence: $path" }
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $LogPath) -Force | Out-Null
    $start = @{ FilePath = $FilePath; ArgumentList = $Arguments; WindowStyle = 'Hidden'; PassThru = $true }
    if ($WorkingDirectory) { $start.WorkingDirectory = $WorkingDirectory }
    $run = Start-Process @start
    $finished = $run.WaitForExit($TimeoutSeconds * 1000)
    $terminationError = $null
    $stillRunning = $false
    if (-not $finished) {
        # 仅终止本次启动的进程；即使终止失败，清理等待也必须有界。
        try { $run.Kill() } catch { $terminationError = $_.Exception.Message }
        try { $stillRunning = -not $run.WaitForExit(5000) }
        catch { $terminationError = $_.Exception.Message; $stillRunning = $true }
    }
    @{ exitCode = $(if ($finished) { $run.ExitCode } else { $null });
       timedOut = (-not $finished); stillRunning = $stillRunning;
       terminationError = $terminationError; finished = (Get-Date -Format o);
       executable = $FilePath; arguments = $Arguments } |
        ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    if (-not $finished) {
        $detail = if ($stillRunning) { 'process is still running' }
                  elseif ($terminationError) { "termination error: $terminationError" }
                  else { 'process terminated' }
        throw "Process timed out after $TimeoutSeconds seconds ($detail): $LogPath"
    }
    if ($run.ExitCode -ne 0) { throw "Process failed ($($run.ExitCode)): $LogPath" }
}

# 核对冒烟标记，防止退出码为零但验证逻辑未执行。
function Assert-ValidationMarker {
    param([Parameter(Mandatory)][string]$LogPath, [Parameter(Mandatory)][string]$Marker)
    if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf) -or
        -not (Select-String -LiteralPath $LogPath -Pattern $Marker -SimpleMatch -Quiet)) {
        throw "Missing smoke marker '$Marker': $LogPath"
    }
}
