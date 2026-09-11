# 用户手动运行此脚本，创建私有仓库并推送已验证的 main 与固定版本标签。
$ErrorActionPreference='Stop'
$systemsRoot=Split-Path -Parent $PSScriptRoot
$githubCli='C:/Program Files/GitHub CLI/gh.exe'
$repository='computerzhuxi/UnityGameSystems'
$remoteUrl='https://github.com/computerzhuxi/UnityGameSystems.git'
$releaseTag='health-v1.0.0'
$login=& $githubCli api user --jq .login
if($LASTEXITCODE -ne 0 -or $login -ne 'computerzhuxi'){throw '请先使用 gh auth login 登录 computerzhuxi。'}
& $githubCli auth setup-git --hostname github.com
if($LASTEXITCODE -ne 0){throw 'Git 凭据配置失败。'}
$status=git -C $systemsRoot status --porcelain
if($LASTEXITCODE -ne 0 -or $status){throw '功能库工作区必须干净后才能发布。'}
git -C $systemsRoot rev-parse --verify "refs/tags/$releaseTag" | Out-Null
if($LASTEXITCODE -ne 0){throw '缺少已验证的固定版本标签。'}
$repoInfo=& $githubCli repo view $repository --json nameWithOwner,isPrivate 2>$null
if($LASTEXITCODE -ne 0){
    & $githubCli repo create $repository --private --description 'Personal reusable Unity systems: Health package and HealthLab'
    if($LASTEXITCODE -ne 0){throw '私有仓库创建失败，请检查账号权限。'}
    $repoInfo=& $githubCli repo view $repository --json nameWithOwner,isPrivate
    if($LASTEXITCODE -ne 0){throw '无法验证新仓库。'}
}
$repo=$repoInfo | ConvertFrom-Json
if(-not $repo.isPrivate -or $repo.nameWithOwner -ne $repository){throw '仓库身份或私有可见性不符合计划，停止推送。'}
$existingRemote=git -C $systemsRoot remote get-url origin 2>$null
if($LASTEXITCODE -ne 0){
    git -C $systemsRoot remote add origin $remoteUrl
    if($LASTEXITCODE -ne 0){throw '添加远程仓库失败。'}
} elseif($existingRemote -ne $remoteUrl){throw '已有 origin 与目标不一致，停止操作。'}
git -C $systemsRoot push -u origin main
if($LASTEXITCODE -ne 0){throw '主分支推送失败；不会强制推送。'}
git -C $systemsRoot push origin "refs/tags/$releaseTag"
if($LASTEXITCODE -ne 0){throw '标签推送失败；不会移动或覆盖已有标签。'}
$remoteTag=git -C $systemsRoot ls-remote origin "refs/tags/$releaseTag"
if($LASTEXITCODE -ne 0 -or -not $remoteTag){throw '远程版本标签读取验证失败。'}
Write-Output '私有仓库和版本已发布。请回到任务告知完成，以继续 ARPG 固定 Git 依赖切换与验证。'
Write-Output 'https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.0'
