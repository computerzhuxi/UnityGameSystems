# Health 1.0.0 最终验收

私有仓库 computerzhuxi/UnityGameSystems 已发布 health-v1.0.0，标签固定提交 ee41bb3538021c177dd30599107025a85aa658c2，不移动。
Unity 6000.3.21f1；HealthLab 干净导入成功，EditMode 23/23、PlayMode 1/1；空项目导入 Sample 后 EditMode 23/23。
HealthLab Windows Standalone 构建并实际运行命令链通过，HEALTH_SMOKE_PASS，退出码 0。
ARPG 正式项目和干净导入隔离副本都通过 Git URL 下载固定版本；各 EditMode 179/179、PlayMode 3/3。正式项目 dotnet 使用 Git 包缓存源码，0 警告、0 错误；正式场景 Standalone 构建通过。

Core DLL 仅引用 netstandard；Unity DLL 仅引用 netstandard、UnityEngine.CoreModule、Computerzhuxi.Health.Core。运行时源码无 ARPG 引用，实际 DLL 无环。
Health Core 唯一拥有当前值/上限，ARPG 门面保持旧内容和事件；仅四份目标配置迁移。Missing Script、缺失托管类型、meta 缺失/孤立/重复 GUID、非目标内容差异均为零。
完整 ARPG 报告：D:/Unity/Project/ARPG/Docs/Architecture/health-extraction-acceptance.md。
日志与构建：本仓库 Artifacts；ARPG 的 Artifacts/HealthExtraction。构建、缓存、日志不入库。

HealthLab 保留本地包路径；ARPG 使用固定 Git 标签；未来私有包使用者需要仓库读取权限。Core 使用单线程同步命令，拒绝事件回调重入；订阅者异常不回滚已提交状态。验证包含自动玩法链，没有额外人工视觉试玩。
