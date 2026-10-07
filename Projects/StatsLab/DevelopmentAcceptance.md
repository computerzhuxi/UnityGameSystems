# Stats 0.1.0 开发阶段验收

本页记录 `com.computerzhuxi.stats` 0.1.0 的开发阶段与发布候选验证。范围为独立包、StatsLab、Sample 和验证脚本；未修改 ARPG 或既有 Health 包。版本事实以 `package.json`、不可变标签和发布历史分别核对，消费项目升级另行处理。

表中 `Tools` 脚本命令是当时执行的历史证据，不作为当前操作入口；当前操作入口见 [StatsLab README](README.md)。

## 自动验证与证据

|行为|命令/入口|本轮证据与结果|
|---|---|---|
|Core 独立编译|`dotnet build C:/Users/yyy/Desktop/ARPG/stats-stage/CoreCompile.csproj --no-restore`|成功，0 警告、0 错误；该临时项目只编译 Core 源码。|
|Lab 实际资源生成|`InvokeUnityValidation.ps1 -Method StatsLabBuild.CreateDemoAssets`|`Artifacts/Logs/Stats/asset-create-20260928-01/StatsLabBuild.CreateDemoAssets.log` 含 `STATSLAB_ASSETS_PASS`；生成场景、Prefab、三份定义资产及 meta。|
|Sample 镜像|`Tools/ValidateStats.ps1 -MirrorOnly`|通过；GUI 可读性修改后于 2026-09-29 再次核对两个示例脚本和 Sample asmdef。|
|Core 公共契约|`Tools/ValidateStats.ps1 -UnityEditor D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe -Platform EditMode -TestFilter StatRegistryTests`|`Artifacts/Logs/Stats/20260928-204306-477-fe16aacf/EditMode.xml`：13/13 Passed，0 Failed。|
|组件生命周期|相同脚本，`-Platform PlayMode -TestFilter StatCollectionLifecycleTests`|`Artifacts/Logs/Stats/20260928-204423-284-f97e3c05/PlayMode.xml`：1/1 Passed，验证真实启停、销毁后旧只读视图释放。|
|真实 Prefab 接入|相同脚本，`-Platform PlayMode -TestFilter StatsLabIntegrationTests`|`Artifacts/Logs/Stats/20260928-204442-338-8249835e/PlayMode.xml`：1/1 Passed，验证定义资产、Prefab、Start 自动装配、来源移除和纯 C# 入口结果一致。|
|GUI 可读性修改后的编译|StatsLab Unity 批处理导入/编译|`Artifacts/Logs/Stats/gui-readability-20260929.log`：脚本已编译，Unity 报告成功退出、返回码 0；未据此声称自动测试重跑。|

第一次 PlayMode 尝试的 `Artifacts/Logs/Stats/20260928-204327-916-16c8c238/PlayMode.xml` 为 `total=0`，原因是 PlayMode asmdef 当时限制 `Editor` 平台；已修正并以上述两份新 XML 取代，零用例不计通过。三个有效 XML 均已按具体测试名与数量逐项核对，共 15 例通过。这 15 例发生在示例 GUI 可读性修改之前；之后仅修改 Sample/Lab 镜像中的显示代码和 Lab 说明，Core 与 Unity Runtime 未变。GUI 修改后只进行上述编译和镜像核对，未重跑自动用例。

## 人工与发布状态

2026-09-29 用户反馈“人工测试没问题”，据此记录 StatsLab GUI（含面板可读性修改后）的人工验收通过。反馈未提供截图、分辨率或逐项操作记录，本记录不把具体显示尺寸或单项按钮另行记为已核实。复核入口与预期仍见 `Projects/StatsLab/README.md`。原有两项其他 Lab 的 `PackageManagerSettings.asset` 未跟随本轮处理。

## 0.1.0 发布候选验证（2026-09-29）

`Tools/ValidateStats.ps1 -UnityEditor D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe -Full -TimeoutSeconds 1200 -SmokeTimeoutSeconds 120` 在最终候选源码上完成，证据目录为 `Artifacts/Logs/Stats/20260929-150613-584-a14cfb3d`：`EditMode.xml` 13/13 Passed，`PlayMode.xml` 2/2 Passed；两份 XML 的具体测试名和数量已逐项核对。`StatsLabBuild.Build.result.json` 与 `standalone-smoke.result.json` 均为退出码 0、未超时；`standalone-smoke.log` 包含 `STATS_SMOKE_PASS`。

独立候选工程 `Artifacts/Validation/StatsCandidate-20260929-1510/Project` 引用同目录复制的候选源码包，并从包的 `Samples~/BasicStatsDemo` 导入完整场景、Prefab、定义资产与 meta。`Artifacts/Logs/Stats/candidate-import-20260929-1510/PlayMode.xml` 的真实 Prefab 接入用例 1/1 Passed；`Artifacts/Logs/Stats/candidate-scene-20260929-1515/StatsCandidateSceneAudit.Run.log` 包含 `STATS_CANDIDATE_SCENE_PASS`，验证场景可打开且脚本、绑定、三份定义资产无缺失。包内 28 个 meta GUID 均唯一，Sample 序列化 GUID 无未解析引用，Runtime/Tests/Sample 文件均有 meta；包内无预编译 DLL，也没有 ARPG 业务引用。最终仅调整包 README 的版本安装说明与包外链接，未改变已验证的代码或资源；独立候选复制包与源包 52 个文件哈希一致。

本页不替代推送后的远端固定标签复验；当时的发布记录已从当前文档目录移除，可在对应版本的 Git 历史中查阅。这些步骤依发布流程单独记录。未升级 ARPG 或其他消费项目。
