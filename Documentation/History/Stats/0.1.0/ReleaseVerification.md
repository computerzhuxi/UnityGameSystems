# Stats 0.1.0 发布与远端标签复验

日期：2026-09-29。用户已确认 StatsLab 人工 GUI 验收，并授权先发布 Stats、再评估 ARPG 接入。发布范围为 `com.computerzhuxi.stats`、StatsLab、Sample 与验证工具；本次未修改 ARPG 或其他消费项目。

## 不可变发布身份

- 包：`com.computerzhuxi.stats@0.1.0`
- Git 安装地址：`https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.stats#stats-v0.1.0`
- 发布提交与标签 peeled commit：`570c1320e61540e501fb7dca42b83e8fdd1e5554`
- 带注释标签：`stats-v0.1.0`；标签对象：`518d51da994dc788e820e50fc2234e1f9b283879`

推送明确的发布提交后，创建并推送注释标签；`git ls-remote` 证实远端 main、标签对象及 peeled commit 与本地值完全一致。未移动既有标签。发布提交包含正式包源码；本记录属于后续独立文档归档，不在 `stats-v0.1.0` 标签内。

## 发布前候选验收

最终候选的 `Tools/ValidateStats.ps1 -Full` 证据在 `Artifacts/Logs/Stats/20260929-150613-584-a14cfb3d`：EditMode 13/13、PlayMode 2/2，Standalone 构建和冒烟均正常退出，冒烟日志包含 `STATS_SMOKE_PASS`。独立候选工程 `Artifacts/Validation/StatsCandidate-20260929-1510/Project` 从复制的候选包导入完整 Sample，真实 Prefab PlayMode 1/1；场景审计含 `STATS_CANDIDATE_SCENE_PASS`。28 个包内 meta GUID 唯一，无 DLL 或 ARPG 业务依赖；候选包和发布前源包的 52 个文件哈希一致。测试详情与用户 GUI 反馈见 [StatsLab 开发验收](../../../../Projects/StatsLab/DevelopmentAcceptance.md)。

## 远端标签安装后的独立复验

新工程：`Artifacts/Validation/StatsRemote-20260929-1540/Project`。建立时只有从 StatsLab 复制的 `ProjectSettings`、空 `Assets` 及上述远端固定标签的 manifest；没有初始 `Library`、`PackageCache`、本地 `file:` 引用或嵌入包。Unity 6000.3.21f1 解析 Git 包后，`Packages/packages-lock.json` 的 `source` 为 `git`，`hash` 为发布提交 `570c1320e61540e501fb7dca42b83e8fdd1e5554`。未清空机器级 UPM 下载缓存。

执行入口为 `Tools/InvokeUnityValidation.ps1`，目标工程为上述新工程，Editor 为 `D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe`。逐份读取本轮 XML 并核对具体用例：

|证据|结果|覆盖|
|---|---|---|
|`Artifacts/Validation/StatsRemote-20260929-1540/Logs/EditMode.xml`|13/13 Passed，0 Failed|`StatRegistryTests` 全部 13 例|
|`Artifacts/Validation/StatsRemote-20260929-1540/Logs/PlayMode.xml`|1/1 Passed，0 Failed|`StatCollectionLifecycleTests.DisableEnablePreservesState_DestroyReleasesComponent`|
|`Artifacts/Validation/StatsRemote-20260929-1540/SampleLogs/PlayMode.xml`|2/2 Passed，0 Failed|包生命周期用例及 `StatsLabIntegrationTests.PrefabStartsWithDefinitionAssets_AndMatchesProgrammaticRegistry`|

从该工程 Git 包的 `Library/PackageCache/.../Samples~/BasicStatsDemo` 复制完整 17 个 Sample 文件到工程 `Assets/BasicStatsDemo`，保留包内 meta；随后加入独立场景审计脚本和 Lab 的真实 Prefab 集成用例作为测试宿主，正式包内容未修改。`Artifacts/Validation/StatsRemote-20260929-1540/SceneLogs/StatsCandidateSceneAudit.Run.log` 包含 `STATS_CANDIDATE_SCENE_PASS`，进程退出码 0；审计实际打开 `BasicStatsDemo.unity`，检查场景脚本、控制器绑定、Prefab 的组件及三个定义资产引用。Sample 导入后的 PlayMode 用例验证真实 Prefab 启动、组件与程序式入口计算一致及修正来源移除。

本轮远端复验未重新运行 Standalone Full；该项使用上述发布前候选结果。用户 GUI 验收也属于发布前反馈，本轮未要求重复人工操作。两份无关的未跟踪 `PackageManagerSettings.asset` 保留。ARPG 接入状态应以 ARPG 自己的 manifest、lock 与集成验证为准；本次发布不代表 ARPG 已升级。
