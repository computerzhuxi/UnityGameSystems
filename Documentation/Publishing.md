# Health 发布与安装

health-v1.0.0 已发布到私有 computerzhuxi/UnityGameSystems，指向 ee41bb3538021c177dd30599107025a85aa658c2。通过 Git Credential Manager 完成认证和推送，实际 UPM 下载已经验证。

Unity Package Manager 使用以下固定 URL：

`https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.0`

其他电脑和 CI 需要私有仓库读取权限。使用本机 Git 凭据管理或 CI 密钥管理，不把凭据放入 URL、manifest、包源码或日志。
HealthLab 继续使用本地路径用于开发。ARPG 已锁定上述标签且只提交到本地，没有推送游戏仓库。

后续发布必须更新包版本与 CHANGELOG，通过包和消费者验收后创建新标签；不要移动或覆盖 health-v1.0.0。main 的验收文档更新不会影响已发布版本。
Tools/PublishHealth.ps1 是使用 GitHub CLI 登录的首版辅助入口；当前发布使用原生 Git 已完成，不需要重复运行或再次登录。
