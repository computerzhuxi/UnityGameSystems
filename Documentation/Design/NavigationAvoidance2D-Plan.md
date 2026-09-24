# Navigation2D 局部避让实施计划

> **历史实施计划：本文记录局部避让阶段的实施安排与当时状态；当前状态以 [0.2.0 最终候选验收](../History/Navigation2D/0.2.0/QuickStartAcceptance.md) 为准。**

Goal：在现有 0.2.0 Agent 候选上完成可选多角色避让及 ARPG 薄适配。
Architecture：批量快照 → 通用 AvoidanceWorld2D → 环境移动段检查 → 项目 Movement。包不持有游戏/World 状态。详见 NavigationAvoidance2D.md（已确认边界的具体化）。
Tech Stack：Unity 6000.3.21f1、C#、NUnit、Physics2D，ORCA 数学子集按 Apache-2.0 标注。
Execution：测试先行，依次实现；独立只读审计与实现使用 subagent-driven-development，禁止技能模板中的自动提交。遵循仓库在现有正式包/现有 Lab 工作的位置要求。

- [x] 核对 HEAD、未提交修改、Bangers、进程；保存 avoidance-baseline-20260917。
- [x] 运行 Lab 与 ARPG 现有基线；在修改前保存测试结果。
- [x] Runtime/AvoidanceWorld2D.cs 与相关契约：先写 Tests/EditMode/AvoidanceTests.cs，验证缺失行为失败，再实现批量邻域/ORCA/预算/确定性；附许可证与改编说明。
- [x] Samples~/CrowdNavigation2D 与独立 Crowd Lab：圆形角色迎面、停止障碍、墙/窄道，运行时可见半径/速度/状态；镜像哈希保护。新增 PlayMode 真实物理测试与构建入口。
- [x] ARPG EnemyNavigation 分离建议与提交、Enemy 统一协调器；GridPathfinder2D 映射组/环境扫掠；先新增真实状态机/刚体集成回归，保留序列化身份、动作优先级。
- [x] 两阶段审查：规格边界/行为，再实现质量；修复后复测。
- [x] 全量验证 Tools/ValidateNavigation2D.ps1；独立 Sample/空 Library、ARPG dotnet、资产加载/DLL/GUID 与基线哈希、diff/staged 检查。
- [x] 更新包 API/架构/维护/CHANGELOG、长期 ARPG 文档及图源、历史候选报告；图逐字一致/幂等。
- [ ] 提供人工操作步骤并等待新避让 Lab/ARPG 视觉结果；再次收口后停在未提交、未打标签、未推送。

证据一律 Artifacts/Logs；失败和重试保留。测试命令基于 Tools/InvokeUnityValidation.ps1 -UnityEditor D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe -Project <真实工程> -Platform EditMode/PlayMode -LogRoot <唯一目录>；不得同时对同一个 Unity 工程运行多个进程，也不得边修改源码边做最终验证。

自动验证收口：2026-09-22，真实/隔离 Lab 47/11，真实/隔离 ARPG 176/14，独立 Sample 46/10，构建、DLL、资产/GUID 已通过。人工验收未完成，报告 History/Navigation2D/0.2.0/AvoidanceAcceptance.md。

人工确认更新：用户明确确认 Crowd Lab 通过；ARPG 和架构查看器未确认，不标记整体完成。
