> 历史范围提示：本文记录 2026-09-16 的单角色 Agent 候选。用户随后已确认该 Lab 效果正常，并批准追加局部避让；新增群体能力请以 [AvoidanceAcceptance.md](AvoidanceAcceptance.md) 为准，不能沿用本文自动验收结论。

# Navigation2D 0.2.0 候选验收

日期：2026-09-16。状态：实现与全部自动验收已完成，**本轮人工验收和独立复核尚未完成**。未提交、未打标签、未推送；0.1.0 标签保持不变。

## 基线

|仓库|HEAD|初始状态|
|---|---|---|
|UnityGameSystems|81b912c903179cf965dd9893c67ce0cae631e6d7|仅未跟踪的已批准设计 Documentation/Design/|
|ARPG|2727bfddb325eab1fc51bc4b2c24502b44bbdaf5|仅 Bangers 既有修改|

Unity 6000.3.21f1。基线清单：两个仓库的 Artifacts/Logs/Navigation2D/0.2.0/baseline-20260916-151757/inventory.json 与 initial.patch。Bangers SHA-256 必须保持 F551412F6599D7D2327E18E8004DE9B5CB0601EF2AE29F2C066DA5A8E1E07321。

已发布 navigation2d-v0.1.0 仍指向 8ebe60c54a7db122cd153c5ca26979bcbb4c2b57，当前候选版本未创建标签。

## 实现与职责

[范围/旧行为兼容矩阵](ScopeAndCompatibility.md)。新增 NavigationAgent2D、AgentSettings2D、NavigationAgentState2D 与可选 NavigationAgent2DComponent；保留全部直接查询契约。新增 Agent Sample、独立 Lab 场景、公开 API 状态测试、真实物理与刚体测试、Prefab 序列化和多角色预算测量。

ARPG 的 EnemyNavigation 只保留速度倍率与执行器引用，GridPathfinder2D 薄封装将 World 正式障碍域提供给 Agent。AI、层级、攻击取消及刚体运动仍在项目；没有包反向依赖和双份路径状态。原私有路径反射测试已替换为公开 Agent + 实际 Physics2D 连续跟随验证。生产 EnemyAIController 跨层和移动追击测试保留。

成功移动目标重算新增最小间隔（ARPG 0.25 秒）；失败重试、调查到达玩法、序列化字段和 GUID 保持。包自身到达检查更严格，要求完整净空。状态、数据归属与流程图已同步。

## 最终自动验证

|工程|EditMode|PlayMode|构建/运行|
|---|---|---|---|
|真实 Navigation2DLab|31/31|10/10|直接查询及 Agent 两个 Windows 构建、退出码 0 与成功标记|
|空 Library Lab|31/31|10/10|首次导入通过|
|独立空 Sample 项目|30/30|10/10|通过公开 Sample.Import 导入两种示例；分别构建并运行成功|
|真实 ARPG|176/176|10/10|Standalone 构建成功；dotnet 0 警告/0 错误；实际播放器观察 30 秒无异常|
|隔离 ARPG|176/176|10/10|从空 Library 起始，Git 网络失败重试后成功|

真实 ARPG 启动观察由测试进程在 30 秒后主动结束，naturalExit=false、harnessStopped=true、exitCode=-1；不是自然退出，也不替代人工游玩。Lab 额外 1 项为宿主直接 Sample 序列化测试，独立 Sample 的30项包含包全部 EditMode 用例。

### 最终证据路径

下列未写仓库前缀的路径均相对于 UnityGameSystems：

- `Artifacts/Logs/Navigation2D/20260916-195837/`：长期验证入口的真实 Lab/ARPG 全部 XML、Unity 编译/构建、Lab 双冒烟；ARPG 子目录含 dotnet.log 和 standalone-startup.log/json。
- `Artifacts/Logs/Navigation2D/0.2.0/isolation-final-20260916/`：initial-state.json 证明初始三份 Library 不存在，Sample 初始只有独立验证脚本；package-snapshot.json 记录最终包 54 文件，与正式源码完全一致。隔离 ARPG 全部 Assets/Scripts 内容哈希也与真实项目一致。
- 上述隔离根的 `Evidence/Lab`、`Evidence/Sample`、`Evidence/ARPG-retry`：对应最终通过记录；Sample 的 `SampleCandidateValidation.Import.log` 和 Build.log 证明真实公共导入与双构建。
- `Artifacts/Logs/Navigation2D/0.2.0/sample-direct-retry.log` 与 `sample-agent-smoke.log`：独立播放器运行成功标记。直接样例首次无日志超时，显式工作目录及短日志路径复试成功；未确定首次超时根因，不将超时当通过。
- **ARPG 仓库** `Artifacts/Logs/Navigation2D/audit-20260916-120624/`：最终真实资产恢复、Missing Script/managed type 与实际 DLL 引用。
- `Artifacts/Logs/Navigation2D/0.2.0/final-audit-20260916/summary.json`：基线内容、GUID、源码快照、图数据、Git 与所有最终 XML 汇总。
- `Artifacts/Logs/Navigation2D/0.2.0/generated-assets-recovery/`：5 项非目标自动改写的完整备份、补丁和恢复值证明。

保留失败尝试：早期 Lab 测试误用传送代替连续运动，已改成连续跟随；ARPG 初次 PlayMode 的立即重算断言与误加严调查判定已修正。最终隔离 ARPG 首次因既有 Perception2D Git 依赖 TLS 握手失败，在不改变其固定标签的情况下重试通过。审计脚本初次将 ARPG 无标签的退出码误当失败，已以可正确返回空集合的 for-each-ref 核对，不涉及产品修改。

## DLL、资产与 Git 收口

- 真实加载全部 Scene、Prefab 和 ScriptableObject；检查 73 个 GameObject，Missing Script / missing managed type = 0。
- 实际 `Computerzhuxi.Navigation2D.dll` 引用仅 netstandard、UnityEngine.CoreModule、UnityEngine.Physics2DModule，无 ARPG、Editor、测试、Lab 或友元反向依赖。
- 实际 `ARPG.Enemy.dll` 引用：netstandard、UnityEngine.CoreModule、UnityEngine.Physics2DModule、ARPG.Abilities/Perception/Actor/Combat/World、Computerzhuxi.Perception2D、Computerzhuxi.Navigation2D、UnityHFSM。依赖方向与既有 asmdef 一致。
- ARPG + 包检查 727 个 meta；Lab + 包检查 37 个 meta；两者缺失/孤立/重复 GUID 均为 0/0/0。Samples~ 与 Documentation~ 不当作运行期导入资产；Lab 的已导入 Sample 纳入扫描。
- ARPG 所有既有 Scene/Prefab/asset/meta 与实施前清单内容哈希一致，Bangers 包含原有修改的完整哈希保持不变。
- 恢复 ARPG 的 UniversalRP、UniversalRenderPipelineGlobalSettings、ProjectSettings、TimeManager 以及 Lab ProjectSettings 自动差异。恢复前逐项核对：ARPG 四项 HEAD 工作树字节哈希等于实施前清单，Lab 原本干净且只有空白变化。审批首次拒绝后补齐这些证据，获准恢复；完整当前内容已备份。Bangers 未恢复或覆盖。
- 两仓库 HEAD 与基线相同，暂存区为空，git diff --check 通过。Systems 11 个已跟踪修改、28 个未跟踪文件；ARPG 17 个已跟踪修改（含 Bangers 原修改），0 个未跟踪文件。
- 无新标签，navigation2d-v0.1.0 仍指向原提交；未执行 commit、tag 或 push。
- 22 张 Mermaid 数据与 Markdown 逐字相同，生成两次 SHA-256 均为 `2b7e4d698a51741355ac34e7db57affddb2afebf5e4aa9bd8e3095505d5c8e43`；渲染、缩放和拖动待人工。

真实修改范围：Systems 为 Agent/组件、Agent 测试与 Sample、Lab 新场景/构建入口、双 Sample 验证工具、API/架构/维护/版本/候选文档与索引；ARPG 为 EnemyNavigation、GridPathfinder2D 薄适配、AIDefinition 注释、导航 Edit/Play 测试、本地 manifest/lock、长期架构/归属/流程图。没有 ARPG 场景、Prefab、配置资产或 GUID 迁移。

## 性能

最终 `20260916-195837/Lab/PlayMode.log`：物理 A* 100 次 p50 1.1856ms、p95 1.3877ms、max 1.4826ms；展开148、障碍接口调用1989、预热分配0。16 Agent 测量100帧总耗时21.0310ms、预热分配0；累计查询656包含初始化、100帧预热与100帧测量，不能解释成单帧预算。稳定目标1000 Tick 无额外查询且零分配。

保留原 A*、未引入跨请求缓存；本轮通过重算间隔控制请求密度，不宣称算法单次加速或跨机器固定耗时。

## 人工验收（本轮待确认）

1. Lab 直接查询与 Agent 两场景：Gizmos、绕墙、半径圈；Agent 移动目标无回头抖动，Pause/Resume、Reset 与障碍开关可恢复。
2. ARPG：巡逻、追击、停止、调查、返回与转角绕墙；确认 0.25 秒重算节流手感可接受。
3. 同层障碍隔离、楼梯完整通过及中途返回、异层目标停止追击/攻击。
4. 架构查看器 Navigation2D、Enemy、World 及敌人流程图渲染、缩放与拖动。

图数据 22 张已完成逐字及幂等验证；人工渲染确认仍待记录。此前 0.1.0 人工结果不替代本轮检查。

## 已知限制与后续

2D 同层圆形占位；不检测地面存在、不自动脱困、不自主跨层找楼梯、不处理群体避让、跳跃重力或 3D。移动仍由消费者执行。原始日志在忽略目录，报告随候选保存。

人工确认后再次核对内容/GUID、Bangers、源码与证据、暂存区和图数据，然后等待独立复核。通过并获新发布授权后才依次：提交/推送系统仓库 → 创建新不可变标签 → 干净工程从远端标签复验 → ARPG 切固定标签 → 全量验证 → 提交/推送 ARPG。保护 Bangers，不能夹带到发布提交。
