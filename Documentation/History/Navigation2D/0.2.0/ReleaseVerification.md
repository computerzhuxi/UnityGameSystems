# Navigation2D 0.2.0 发布与远程标签验证

日期：2026-09-24。用户在候选人工验收和独立复核通过后明确授权按发布流程执行。本记录是发布阶段结果；[最终候选验收](QuickStartAcceptance.md)保留发布前的边界、自动测试和用户实际观察范围。

## 不可变包身份

- 包：`com.computerzhuxi.navigation2d@0.2.0`。
- 带注释远程标签：`navigation2d-v0.2.0`，标签对象 `6a32647ea1ece315fef56e8fbe7d21d46ba1d8b8`，解引用提交 `2c5d452cf6cc801e3d9f4a9b1cd66dc8ae55cf49`。
- 安装地址：`https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.navigation2d#navigation2d-v0.2.0`。
- 先把 120 个目标包、Lab、测试及文档文件白名单提交并推送 `origin/main`，再创建、推送标签。历史 `navigation2d-v0.1.0` 标签对象仍为 `1ad688e1612d37c3cdfcfff0c4ea7106da5d849e`，提交仍为 `8ebe60c54a7db122cd153c5ca26979bcbb4c2b57`。

## 从远端标签安装

证据根：`Artifacts/Logs/Navigation2D/release-20260924-remote-tag/`。两个全新消费者初始均没有 Library、PackageCache 和包锁文件，Assets 仅有独立验证工具。Unity Package Manager 从远程 Git 标签安装，不使用本地 `file:` 包；两个锁文件均为 `source=git`、`hash=2c5d452cf6cc801e3d9f4a9b1cd66dc8ae55cf49`。

|消费者|验证结果|
|---|---|
|带 Test Framework|导入四个 Sample；EditMode 47/47、PlayMode 24/24；实际资产/DLL Audit 通过，QuickStart 5 个序列化 GameObject 无 Missing Script/托管类型缺失；Direct、Agent、Crowd、QuickStart 四种 Standalone 构建与自然退出冒烟全部通过|
|不带 Test Framework|四个 Sample 导入、实际资产/DLL Audit、四种 Standalone 构建通过；QuickStart 自然退出冒烟通过；manifest/lock 不含 Test Framework|

Runtime 实际 DLL 仅依赖 `netstandard`、`UnityEngine.CoreModule` 和 `UnityEngine.Physics2DModule`，无 ARPG 或 Sample 反向依赖。`QuickStartExampleMover2D` 仅属于 Sample 程序集；正式 Runtime 不含 Mover 类型。PlayMode XML 中明确存在且通过禁用/移除示例运动器后 Navigator 继续更新、Runtime 不依赖 Mover/Sample 的用例。

## ARPG 固定标签消费

真实项目 `D:/Unity/Project/ARPG` 将 manifest 和锁文件从本地路径改成上述标签，UPM 锁定到发布提交。ARPG 白名单接入提交 `bff5a46a21da525b42108ba823e42aba9e7108e8` 已推送 `origin/main`。Unity `6000.3.21f1` 的本轮证据位于 ARPG `Artifacts/Logs/Navigation2D/release-20260924/Evidence/` 和 `Artifacts/Logs/Navigation2D/audit-20260924-090116/`；[ARPG 发布验收](../../../../../ARPG/Docs/Architecture/navigation2d-0.2.0-release-verification.md)记录消费细节。

|验证|结果|
|---|---|
|完整 EditMode / PlayMode|176/176、15/15，失败 0|
|StandaloneWindows64|真实场景构建通过；播放器有界运行 60 秒无匹配脚本异常，随后仅停止本次启动的进程，不冒充自然退出|
|dotnet build ARPG.sln --no-restore|14 个项目，警告 0、错误 0|
|真实资产加载|73 个 GameObject，Missing Script/缺失托管类型 0|
|DLL|`ARPG.Enemy` 依赖 `ARPG.World`、`Computerzhuxi.Navigation2D`；World 不反向依赖项目生产程序集，包不依赖 ARPG|
|GUID|ARPG Assets + 安装包 740 个 meta，缺失/孤立/重复 0/0/0|

开工前对 ARPG 的 1,343 个已跟踪文件及 2 个既有未跟踪文件记录哈希。受保护内容与开工字节一致；Unity 自动改写的 UniversalRP、URP Global Settings、ProjectSettings 和 TimeManager 保存生成字节后，证明开工字节与 HEAD 一致才恢复，证据在 ARPG `Artifacts/Logs/Navigation2D/release-20260924/generated-backups/proof.json`。Bangers 用户既有修改保持原样，不进提交，SHA-256 `F551412F6599D7D2327E18E8004DE9B5CB0601EF2AE29F2C066DA5A8E1E07321`。

ARPG 23 张查看器图与 Markdown 逐字一致，连续生成哈希相同：`E3C43458FEA08B55AB89487B810D4CAF8BBDE8F460BFCEAAC6C66084A56B1530`。两仓库白名单提交均通过 staged `git diff --check`；发布阶段未新增人工游戏观察，沿用候选报告中明确由用户确认的范围。

## 已知边界

正式 Runtime 只负责路径、导航建议和可选批量避让；项目运动组件执行实际移动。敌人自主选择楼梯并跨层寻路、跨层技能/多目标近战过滤、地面检测、自动脱困与战斗站位不属于 0.2.0。
