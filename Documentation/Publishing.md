# 手动发布 Health 1.0.0

发布目标仅为私有 `computerzhuxi/UnityGameSystems`，脚本不会推送 ARPG，也不会强制覆盖远程或标签。

## 首次登录

在本机 PowerShell 运行：

```powershell
& "C:/Program Files/GitHub CLI/gh.exe" auth login --hostname github.com --git-protocol https --web
& "C:/Program Files/GitHub CLI/gh.exe" auth setup-git --hostname github.com
& "C:/Program Files/GitHub CLI/gh.exe" api user --jq .login
```

网页中使用 computerzhuxi 账号授权，最后一条输出必须为 computerzhuxi。不要分享密码、令牌或验证码。

## 创建私有仓库并推送

检查 Tools/PublishHealth.ps1 后运行：

```powershell
& "D:/Unity/Project/UnityGameSystems/Tools/PublishHealth.ps1"
```

脚本验证账号、干净工作区、固定本地标签和仓库私有可见性，然后推送 main 与 health-v1.0.0。
无需提前创建仓库；若仓库已存在，脚本先验证身份和私有可见性，普通 Git push 仍保护已有历史。

## 发布后接入

回到原任务告知发布完成。代理还须将 ARPG 本地依赖切换到以下 URL，刷新锁文件，验证隔离下载和全部最终回归：

`https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.0`

仅创建本地标签或完成本地测试不等于已验证远程 UPM 安装。私有包的其他开发机和 CI 同样需要读取权限。
