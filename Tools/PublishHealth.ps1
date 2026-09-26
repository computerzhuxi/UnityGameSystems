param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$TargetCommit
)
# 人工发布入口：仅推送已存在、已验证且指向显式目标提交的 Health 注释标签。
$ErrorActionPreference = 'Stop'
$systemsRoot = Split-Path -Parent $PSScriptRoot
$githubCli = 'C:/Program Files/GitHub CLI/gh.exe'
$repository = 'computerzhuxi/UnityGameSystems'
$remoteUrl = 'https://github.com/computerzhuxi/UnityGameSystems.git'
$package = Get-Content -LiteralPath (Join-Path $systemsRoot 'Packages/com.computerzhuxi.health/package.json') -Raw | ConvertFrom-Json
$releaseTag = "health-v$($package.version)"
$tagRef = "refs/tags/$releaseTag"

# 在任何远程写入前核对仓库、工作树、目标提交及本地注释标签的身份。
function Assert-LocalReleaseIdentity {
    $status = git -C $systemsRoot status --porcelain
    if ($LASTEXITCODE -ne 0 -or $status) { throw '功能库工作区必须干净后才能发布。' }
    $headCommit = git -C $systemsRoot rev-parse --verify HEAD
    if ($LASTEXITCODE -ne 0 -or $headCommit -ne $TargetCommit) { throw 'HEAD 与显式目标提交不一致。' }
    $tagType = git -C $systemsRoot cat-file -t $tagRef 2>$null
    if ($LASTEXITCODE -ne 0 -or $tagType -ne 'tag') { throw "缺少带注释的本地标签：$releaseTag" }
    $tagObject = git -C $systemsRoot rev-parse --verify $tagRef
    $tagCommit = git -C $systemsRoot rev-parse --verify "$tagRef^{commit}"
    if ($LASTEXITCODE -ne 0 -or $tagCommit -ne $TargetCommit) { throw '本地标签与显式目标提交不一致。' }
    return $tagObject
}

# 读取远端标签对象与 peeled commit；读取失败绝不等同于标签不存在。
function Get-RemoteReleaseTag {
    $lines = @(git -C $systemsRoot ls-remote origin $tagRef "$tagRef^{}")
    if ($LASTEXITCODE -ne 0) { throw '无法读取远端标签；停止发布。' }
    $objects = @{}
    foreach ($line in $lines) {
        if ($line -match '^([0-9a-fA-F]{40})\s+(.+)$') { $objects[$Matches[2]] = $Matches[1] }
    }
    if ($objects.Count -eq 0) { return @{ Exists = $false } }
    if (-not $objects.ContainsKey($tagRef) -or -not $objects.ContainsKey("$tagRef^{}")) {
        throw '远端标签不是可核对的带注释标签。'
    }
    return @{ Exists = $true; Object = $objects[$tagRef]; Commit = $objects["$tagRef^{}"] }
}

if (-not (Test-Path -LiteralPath $githubCli -PathType Leaf)) { throw "缺少 GitHub CLI：$githubCli" }
$login = & $githubCli api user --jq .login
if ($LASTEXITCODE -ne 0 -or $login -ne 'computerzhuxi') { throw '请先使用 gh auth login 登录 computerzhuxi。' }
$repoInfo = & $githubCli repo view $repository --json nameWithOwner,isPrivate
if ($LASTEXITCODE -ne 0) { throw '无法确认远程仓库身份；停止发布，不自动创建仓库。' }
$repo = $repoInfo | ConvertFrom-Json
if (-not $repo.isPrivate -or $repo.nameWithOwner -ne $repository) { throw '远程仓库身份或私有可见性不符。' }
$existingRemote = git -C $systemsRoot remote get-url origin
if ($LASTEXITCODE -ne 0 -or $existingRemote -ne $remoteUrl) { throw 'origin 与目标远程仓库不一致。' }
$tagObject = Assert-LocalReleaseIdentity
$remoteTag = Get-RemoteReleaseTag
if ($remoteTag.Exists) {
    if ($remoteTag.Object -ne $tagObject -or $remoteTag.Commit -ne $TargetCommit) {
        throw '远端同名标签指向其他对象；不会移动或覆盖。'
    }
    Write-Output "标签 $releaseTag 已存在且身份一致，无需推送。"
} else {
    git -C $systemsRoot push origin "${tagRef}:${tagRef}"
    if ($LASTEXITCODE -ne 0) { throw '标签推送失败；不会强制推送。' }
    $remoteTag = Get-RemoteReleaseTag
    if (-not $remoteTag.Exists -or $remoteTag.Object -ne $tagObject -or $remoteTag.Commit -ne $TargetCommit) {
        throw '推送后远端标签对象或目标提交核对失败。'
    }
    Write-Output "标签 $releaseTag 已推送并核对对象与目标提交。"
}
Write-Output "$remoteUrl`?path=/Packages/com.computerzhuxi.health#$releaseTag"
