# Navigation2D 0.2.0：正式包仅导航的候选收口

日期：2026-09-23。**实现与自动验证完成，QuickStart 人工验收已由用户观察确认通过，独立验收已通过，无阻断项。未暂存、未提交、未打标签、未推送。** 本记录取代前一轮“正式 Runtime 提供运动器”的候选边界；旧记录及原始证据保留在 `Artifacts/Logs/Navigation2D/0.2.0/navigation-only-baseline-20260923/previous-QuickStartAcceptance.md`。

## 产品边界与实际修改

- 正式 `Computerzhuxi.Navigation2D` Runtime 只提供查询、Agent、只读路径/方向建议及可选批量避让，没有实际运动组件。
- **NavigationRigidbody2DMover 已从 Runtime 移除**；删除其正式 Inspector、正式 API 契约和包内运动测试依赖。
- NavigationNavigator2D 保持不变：在 Inspector 配置目标、身体/净空、障碍层、网格、预算、重算与容差，自动固定帧计算，只读暴露路径和建议；不写 Transform/Rigidbody2D，也不停止外部速度。
- 移动实现改为 `Samples~/QuickStartNavigation2D/QuickStartExampleMover2D.cs`，命名空间 `Computerzhuxi.Navigation2D.Samples`，程序集 `Computerzhuxi.Navigation2D.QuickStartSample`。这是 **Sample/Lab 示例适配器，不是正式 API 或生产移动能力**。实际项目使用自己的移动系统。
- 原身体扫掠/暂停/动态障碍等示例集成测试移到 Sample 的独立测试程序集，并补充“禁用及移除示例后导航继续更新”和“实际 Runtime 无 Mover 类型”的检查。
- 新增正式包 NavigatorOwnershipTests，验证不移动 Kinematic 身体、不修改外部 Dynamic 速度，以及没有 Sample/ARPG 依赖。
- 保留脚本 meta GUID，Sample 添加 MovedFrom 兼容旧未发布候选身份；通过 Unity 真实加载并保存场景。Unity 场景仍保留旧 m_EditorClassIdentifier 历史标识，实际解析类型已验证为 Sample 程序集中的 QuickStartExampleMover2D；未手改 YAML 或改变 ARPG 内容。
- README、UnityGuide、API、Architecture、Maintenance、CHANGELOG、Lab、设计及历史索引已同步。包图中的移动节点明确标注仅 Sample/Lab；ARPG 图没有新依赖变化，生成结果保持原样。

## 两仓库基线与保护

| 仓库 | HEAD | 本轮范围 |
|---|---|---|
| UnityGameSystems | `81b912c903179cf965dd9893c67ce0cae631e6d7` | 包、示例、测试、Lab 工具及文档 |
| ARPG | `2727bfddb325eab1fc51bc4b2c24502b44bbdaf5` | **与本轮开始时全部 Git 可见文件逐字节一致**；保留此前候选及用户修改 |

Unity 均为 `6000.3.21f1`。完整基线状态、文件哈希与差异：`Artifacts/Logs/Navigation2D/0.2.0/navigation-only-baseline-20260923/`。

ARPG 仍为底层 API + EnemyNavigation / CharacterMovement2D；Assets/Scripts 没有 Navigator、新示例或旧生产 Mover 的引用。没有引入新的角色组件或场景配置。

Bangers SHA-256 保持 `F551412F6599D7D2327E18E8004DE9B5CB0601EF2AE29F2C066DA5A8E1E07321`。已发布 0.1.0 标签对象 `1ad688e1612d37c3cdfcfff0c4ea7106da5d849e`，提交 `8ebe60c54a7db122cd153c5ca26979bcbb4c2b57`，未改变。无 0.2.0 标签，两个暂存区为空。

## 自动验证结果

| 环境 | EditMode | PlayMode | 其他结果 |
|---|---:|---:|---|
| 最终真实 Lab | 48/48 | 25/25 | 四种 Standalone 构建及自然退出冒烟通过 |
| 真实 ARPG | 176/176 | 15/15 | Standalone、真实资产恢复、DLL 审计通过 |
| 空 Library Lab | 48/48 | 25/25 | 使用冻结本地候选 |
| 空 Library ARPG | 176/176 | 15/15 | 使用冻结本地候选 |
| 最终空项目，含测试框架 | 47/47 | 24/24 | Package Manager 导入四 Sample，四构建/自然退出冒烟通过 |
| 最终空项目，不含测试框架 | 不适用 | 不适用 | 四 Sample 导入/构建及真实 QuickStart 加载通过；QuickStart 自然退出冒烟通过 |

ARPG dotnet：0 警告、0 错误。本轮 ARPG 构建另作 60 秒启动观察，无匹配脚本异常；超时后仅停止工具自己启动的进程，**不把它描述为自然退出或人工验收**。

最终 Lab/空项目 XML 中明确存在且通过 `QuickStartExampleTests.ExampleDisabledAndRemoved_NavigationContinues`；不是条件编译隐藏测试后的较小绿色总数。验证入口同时要求 Runtime 边界用例通过，并拒绝非法版本表达式日志。

空 Library Lab/ARPG 的运行时代码与最终代码相同；其测试在新增 Sample 测试条件前执行。条件修正后重新跑了真实 Lab 全量和全新有/无测试框架消费者；没有用前一轮日期的旧证据替代本轮验证。

### 本轮证据路径

均以 UnityGameSystems 为根，ARPG 路径另标：

- `Artifacts/Logs/Navigation2D/20260923-152904/`：首次完整真实 Lab/ARPG、ARPG 构建与 dotnet、启动观察。
- `Artifacts/Logs/Navigation2D/20260923-153946/`：最终条件修正后真实 Lab 48/25、四构建与冒烟。
- `Artifacts/Logs/Navigation2D/0.2.0/navigation-only-isolated-20260923/`：首次空 Library Lab/ARPG 及独立 Sample，含初始无 Library 清单。
- `Artifacts/Logs/Navigation2D/0.2.0/navigation-only-verified-20260923/`：最终冻结包、全新有/无测试框架项目、导入/测试/构建/类型审计与冒烟 JSON。`initial-state.json` 证明 Assets 初始仅验证工具。
- `ARPG/Artifacts/Logs/Navigation2D/audit-20260923-073405/`：真实 ARPG 资产及 DLL。
- `Artifacts/Logs/Navigation2D/0.2.0/navigation-only-audit-20260923/summary.json`：最终状态、内容哈希、GUID、图与必需测试核对。

## 实际 DLL、资产与隔离

- Runtime 实际依赖仅 `netstandard, UnityEngine.CoreModule, UnityEngine.Physics2DModule`；完整 `runtime-types.txt` 无任何 Mover 类型，没有反向 Sample/ARPG 依赖。
- Editor 仅提供导航 Inspector；依赖 Runtime、UnityEditor/Core/IMGUI 与 .NET。无运动器 Inspector。
- Sample 运动类型仅在 `Computerzhuxi.Navigation2D.QuickStartSample`。Sample 测试只有安装 Test Framework 时启用，不要求普通消费者安装测试框架。
- ARPG.Enemy 实际 DLL 依赖保持 Actor/Abilities/Combat/Perception/World、Navigation2D/Perception2D、UnityHFSM 与 Unity 模块。
- 真实 ARPG 加载全部 Scene/Prefab/ScriptableObject，检查 73 个 GameObject，Missing Script/缺失托管类型 0。
- 有/无测试框架消费者均真实加载 QuickStart 的 5 个序列化 GameObject，缺失脚本/托管类型 0；其他三个示例在本轮分别构建及运行验证。
- 当前正式包**全部文件**与最终冻结包逐字节一致；四 Sample 与 Lab 镜像的文件集合/哈希一致。
- ARPG Assets + 包 740 个 meta、Lab Assets + 包 67 个 meta：缺失/孤立/重复 GUID 均 0/0/0。Samples~ / Documentation~ 按 Unity 非导入目录排除。
- ARPG 22 张查看器图与 Markdown 逐字一致；本轮生成两次，结果与生成前一致，幂等 SHA-256：`3d84cce97ee1b283143cd62db0bb2dddb660e0400b6e1b98c4eac9f82583b9c1`。
- 5 个 Unity 自动修改的时间/渲染/项目设置文件先备份，再证明 HEAD 字节等于本轮基线哈希后恢复；证据 `navigation-only-audit-20260923/generated-backups/proof.json`。Bangers 未覆盖或恢复。
- 两仓库 `git diff --check` 通过，暂存区为空。

## 失败、修正与证据限制

1. 第一次 Lab 迁移批处理因用户编辑器占用返回 1；用户保存并关闭后重试通过。没有关闭用户进程或删除锁文件。
2. 无 Test Framework 的消费者暴露 Sample 测试缺少 NUnit/UnityTest 依赖；保留失败工程 `navigation-only-isolated-20260923/ConsumerWithoutTests`。
3. 首次条件表达式 `[1.0.0,)` 不被 Unity 支持，触发 ExpressionNotValidException，并误排除了示例测试。该阶段 Lab 14/14、Sample 13/13 **不作为最终通过证据**，保留在 `navigation-only-final-20260923/`。
4. 修正为 `1.0.0`（大于等于该版本），与 defineConstraints 配合；重新从空工程验证两种消费者，并检查必须出现的示例用例。表达式语义见 [Unity 官方程序集定义手册](https://docs.unity3d.com/es/2021.1/Manual/ScriptCompilationAssemblyDefinitionFiles.html)。最终数据在 `navigation-only-verified-20260923/` 及真实 Lab `20260923-153946/`。

## QuickStart 人工验收与未完成项

用户已在 Navigation2DLab 的 `Assets/Samples/QuickStartNavigation2D/QuickStartNavigation2D.unity` 场景完成人工检查，并明确回复“正常”。**以下结论来自用户实际观察确认，不是自动化结果，也不是代理视觉观察。**

- 默认场景绕墙到达正常。
- 正式 Navigator 没有移动职责。
- 禁用 Sample 专属 QuickStartExampleMover2D 后，角色停止，但 Navigator 仍更新导航。
- 重新启用示例移动器后恢复移动。
- 相关控制正常。

独立验收已由委托任务基于源码、实际 DLL 类型与依赖、测试 XML 及工作区状态复核完成，结论通过，无阻断项。复核确认正式 Runtime/Editor 无 Mover 引用，Navigator 不写位置或外部速度，示例运动器仅属于 Sample/Lab，ARPG 保持底层 API 与自有移动系统；关键所有权及禁用/移除示例移动器用例实际存在且通过，两仓库差异检查通过、暂存区为空，Bangers 哈希保持不变。本次人工确认仅覆盖上述 QuickStart 检查，不扩展为组件移除、所有 Inspector 参数/Gizmo 或 ARPG 的新增人工观察。

说明：QuickStart 使用 Kinematic 是示例选择，正式导航不限制项目运动方式。暂停/停止导航不会替项目清除外力；项目应自行管理移动优先级、受击、冲刺、碰撞与停止。跨层楼梯、地面检测、脱困、战斗站位及自动群体协调仍不在本轮范围。

QuickStart 用户人工验收与委托任务独立验收均已通过。保持未暂存、未提交、未打标签、未推送；本次不执行发布操作。

最终整体工作区（包含本轮前既有候选）：UnityGameSystems 11 个已跟踪修改、109 个未跟踪文件；ARPG 19 个已跟踪修改、2 个未跟踪文件。
