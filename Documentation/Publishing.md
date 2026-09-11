# Health 发布与安装

当前发布 com.computerzhuxi.health@1.0.1，标签 health-v1.0.1，源码 `d05303f629393a41f93d3eba3a45e60dc4f177ca`，私有仓库 computerzhuxi/UnityGameSystems。

`https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1`

已通过 Git Credential Manager 认证并验证真实 UPM 下载。其他电脑和 CI 需要私有仓库读取权限；不把凭据放入 URL、manifest、包或日志。
HealthLab 保留本地引用，ARPG 锁定新标签且只本地提交，没有推送游戏仓库。原 health-v1.0.0 完整保留。
后续发布更新版本和 CHANGELOG，通过验证后创建新标签，不移动已发布标签。Tools/PublishHealth.ps1 从 package.json 读取版本；它使用 GitHub CLI 登录，本次已通过原生 Git 完成，无需重复登录或发布。
