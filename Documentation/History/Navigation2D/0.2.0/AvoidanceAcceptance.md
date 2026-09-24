# Navigation2D 0.2.0：局部避让候选验收

> **历史阶段报告：本记录已被 [0.2.0 最终候选验收](QuickStartAcceptance.md) 取代，仅保留局部避让阶段的证据和当时状态；当前结论以最终报告为准。**

人工复测更新：用户再次测试后回复“我又去测试了下发现没有问题”，ARPG 单个敌人移动的反馈未再复现。新增原场景回归证明四个原始敌人均能实际巡逻，完整 PlayMode 15/15；无需修改生产实现。Crowd Lab 已通过，架构查看器人工确认仍待完成。

日期：2026-09-22。状态：候选已实现，真实工程、独立 Sample 和空 Library Lab/ARPG 自动验收均通过；用户已确认 Crowd Lab 人工验收通过；ARPG 已反馈复测正常，架构查看器人工确认及最终独立复核尚未完成。保持未提交、未打标签、未推送，暂存区为空。此前 Agent Lab 用户已确认正常，不替代新 Crowd Lab 验收。

## 基线与保护

|仓库|HEAD|当前范围|
|---|---|---|
|UnityGameSystems|81b912c903179cf965dd9893c67ce0cae631e6d7|11 项已跟踪修改、62 个未跟踪文件；包含此前 Agent 候选|
|ARPG|2727bfddb325eab1fc51bc4b2c24502b44bbdaf5|19 项已跟踪修改、2 个未跟踪文件；包含 Bangers 既有修改|

Unity 6000.3.21f1。开工完整哈希、状态与补丁：Systems `Artifacts/Logs/Navigation2D/0.2.0/avoidance-baseline-20260917/`。没有覆盖既有 Agent 候选。Bangers SHA-256：`F551412F6599D7D2327E18E8004DE9B5CB0601EF2AE29F2C066DA5A8E1E07321`，与开工一致。

已发布 navigation2d-v0.1.0 标签对象保持 `1ad688e1612d37c3cdfcfff0c4ea7106da5d849e`，提交保持 `8ebe60c54a7db122cd153c5ca26979bcbb4c2b57`；没有 0.2.0 标签。本轮不继承 0.1.0 的发布授权。

## 边界与真实修改

包唯一正式源码仍在 `Packages/com.computerzhuxi.navigation2d`，版本为未发布 0.2.0。

- 新增 AvoidanceAgent2D / AvoidanceResult2D / AvoidanceWorld2D：完整快照输入、同序速度输出、分组/稳定 ID、锁定身体、邻居预算、不可行与截断诊断。工作区不推进位置、不管理对象、不保存路径。
- 内部 OrcaSolver2D 改编官方 RVO2-CS 的代理 ORCA 与线性规划；来源固定 `a455da254cffd9ebb8d85f8eedb9d9332e69012a`，Apache-2.0 版权/全文/变更说明在 `Third Party Notices.md`。没有复制商业 A* 项目实现。
- AgentSettings2D 新增默认关闭的 LookAheadDistance；Agent 唯一推进路径索引，对前视捷径验证完整净空并额外保留接触余量。最多 8 次附加检查。
- 新增 Crowd Sample、NavigationCrowdLab 场景、Lab 实际示例测试与第三条 Standalone 构建/冒烟路线；三份镜像由验证脚本检查哈希。
- ARPG 新增 internal EnemyAvoidanceCoordinator；修改 EnemyNavigation、EnemyAIController、GridPathfinder2D 和对应测试。没有修改 CharacterMovement2D、World 实现、能力资产或场景/Prefab。
- 包 README/API/架构/维护/CHANGELOG、仓库索引、历史记录，以及 ARPG 长期架构、数据归属、流程和图源同步更新。

## ARPG 调用与数据归属

AI Update 决定目的地；协调器在 FixedUpdate(-50) 统一推进 Agent、采集全体实际状态、Solve，再校验真实刚体与当前层地图移动段，最后提交普通运动意图。CharacterMovement2D 继续唯一写刚体，Forced > Action > locomotion 不变。适配层不会 normalized 避让输出而丢失减速。

World 仍是正式层级唯一权威。只有 GridPathfinder2D 读取 World 类型，用 PhysicsScene2D + CurrentLevel 映射组；楼梯预览/取消不提前换组，场景迁移重建障碍源。路径/目的地/进度/时钟仍全部属于 NavigationAgent2D。

项目当前配置：256 个活动身体、24 邻居、邻域 6、时间范围 1 秒、偏置 0.1、前视 3。实际非触发身体 bounds 包围圆加 0.02 余量，保留胶囊偏移。停止、冻结、禁用运动器或 AI、攻击与强制运动仍为邻居；不要求它们承担互惠让路。动态缺失形状清除旧普通意图。

## 验证结果与证据

真实本轮入口：Systems `Artifacts/Logs/Navigation2D/20260922-130329/`。

|项目|EditMode|PlayMode|构建/运行|
|---|---:|---:|---|
|真实 Lab|47/47|11/11|Direct / Agent / Crowd 三个 Standalone 构建及自然退出冒烟通过|
|真实 ARPG|176/176|15/15|Standalone 构建通过；dotnet 0 警告、0 错误|
|空 Library Lab|47/47|11/11|使用冻结包快照；初始状态证据证明无 Library|
|全新独立 Sample|46/46|10/10|公开 Sample.Import 导入三个示例；三个独立构建与自然退出冒烟通过|
|空 Library ARPG|176/176|14/14|不修改原项目依赖；导航指向冻结包，其他 Git 依赖保留|

隔离记录：`Artifacts/Logs/Navigation2D/0.2.0/crowd-isolated-20260922/`，包括 initial-state、package-snapshot、Evidence。包 Runtime 与 ARPG Assets/Scripts 与冻结副本逐字节一致；后续示例 README 措辞修正单独属于文档，不冒充旧冻结全文相同。

新增回归覆盖：对称迎面、停止身体、不同半径、同位置异组、稳定输入排列、重合/不可行、容量/截断、异常原子性、重入、有限数值上限、零分配、Agent+避让中间路径点停滞，以及真实敌人绕过三类停止身体与禁用形状停止旧速度。薄墙/不同半径物理测试启用前视后仍通过。

128 代理×200 次求解的阶段实测 189.0196 ms，热路径托管分配 0 bytes；这是特定布局/机器数据，不保证最坏耗时。插入排序与密集邻域扫描仍可为 O(N²)。

### 失败与修复记录

- `avoidance-arpg-red`：原候选追击者卡在 x=101.400459，10/11 通过，新增真实回归失败。
- `avoidance-arpg-diagnostics`：初版群体适配仍卡在 x≈100.794；日志证明短网格点位于邻居圆内，新增安全前视后真实 14/14 通过。
- `avoidance-lookahead-red`：45/47 通过，组合跟随和超大有限速度两个新回归按预期失败，修复后 47/47。
- `avoidance-corner-diagnostic-20260922`：长扫掠与短步端点在贴角处接触容差不同；前视捷径增加保守余量后 11/11 PlayMode 通过，没有放宽查询半径。
- ARPG Standalone 首次 30 秒未产生日志，保存失败 JSON；短日志路径重试 60 秒有正常启动记录且未发现匹配的异常。由工具结束自建进程，exitCode=-1、naturalExit=false，不能当作自然退出或人工游玩通过。证据 ARPG `Artifacts/Logs/Navigation2D/crowd-startup-retry.log(.json)`。
- 自动审批拒绝过销毁重复协调器整个宿主的提案；改为仅禁用重复组件，保留对象，已获准实施。无待用户审批项。

## 资产、DLL 与 Git 审计

真实 Unity 审计：ARPG `Artifacts/Logs/Navigation2D/audit-20260922-050847/`，全部 Scene/Prefab/ScriptableObject 真实加载，检查 73 个 GameObject，Missing Script/托管类型为 0。

- 实际包 DLL：netstandard、UnityEngine.CoreModule、UnityEngine.Physics2DModule；无 ARPG、Sample、Lab、Tests 依赖或友元。
- 实际 Enemy DLL：Actor、Combat、Abilities、Perception、World、Perception2D、Navigation2D、UnityHFSM、Unity Core/Physics2D、netstandard；无新增生产程序集依赖。
- ARPG + 包 734 个 meta，缺失/孤立/重复 GUID：0/0/0；Lab + 包 51 个 meta，同为 0/0/0。
- 相对开工基线，既有 Scene/Prefab/asset/meta 内容变化 0；Bangers 与开工哈希一致。
- Unity 自动改写的 5 个渲染/项目/TimeManager 文件先备份，再验证 HEAD 字节与开工哈希一致后恢复。记录 `crowd-final-audit-20260922/generated-backups/proof.json`。
- 两仓库 staged 为空，git diff --check 通过。22 张图的数据与 Markdown 逐字一致，重新生成哈希不变：`02dadbd2a82d70107e244a91d5dda1949835f12f37e788d5473f34d15c16c6c5`。

机器可读总审计：Systems `Artifacts/Logs/Navigation2D/0.2.0/crowd-final-audit-20260922/summary.json`。

## 待人工确认

1. **Crowd Lab 已通过**：用户明确回复“只有 Crowd Lab 已检查通过”。此确认不延伸到 ARPG 或架构查看器。
2. **ARPG 用户复测正常**：在反馈“只有一个敌人会动”后再次测试未发现问题；未把此反馈扩大为对所有楼梯/架构查看器项目的逐项确认。
3. 同层障碍、不同层目标、楼梯完整通过与中途返回保持现有语义。不会自主找楼梯。
4. 架构查看器中 Navigation2D、Enemy、World 图的渲染、缩放和拖动。

自动结果不能替代上述观察。人工完成后再次全量收口，才可交独立复核。

## 已知限制与下一步

圆形保守包围可能比实际胶囊更早停步；运行时新增/删除 Collider 组件需要重建注册，初始 inactive 子形状支持恢复。玩家暂不参与敌人互惠群体。物理裁剪、截断、不可行或初始重叠不承诺无碰撞/脱困，窄道不承诺会车。地面存在检测、战斗站位/攻击名额、跨层路径/楼梯规划、3D 均不包含。

本轮停止点仍是未提交、未标签、未推送。人工确认及独立复核通过并另获发布授权后：Systems 显式白名单提交推送 → 新不可变 navigation2d-v0.2.0 标签 → 干净项目远程标签安装复验 → ARPG 切固定标签与锁文件 → ARPG 全量验证 → 白名单提交推送；Bangers 单独按用户决定处理。


## 原场景巡逻补充回归

证据：Systems `Artifacts/Logs/Navigation2D/0.2.0/original-scene-red/PlayMode.xml` 与日志。目录名称是在复现前预设的 red，但实际运行 **15/15 全通过**，不是失败证据。原出生位置、真实四个控制器均保持，固定随机种子、移开玩家隔离巡逻；观察 600 个物理步，每个敌人的最大位移均超过 0.25。日志显示正常巡逻/等待交替，路径及身体扫掠没有导致全体停步。新增测试属于回归补强，生产代码未变化；隔离副本的 14/14 为新增此项之前的完整测试，运行时代码相同。
