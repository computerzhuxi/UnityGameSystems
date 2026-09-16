# Navigation2D 0.1.0 候选验收记录

> 本文保留候选阶段快照。2026-09-16 用户确认独立复核通过并授权发布；后续状态见 [发布验证](ReleaseVerification.md)。

日期：2026-09-15。状态：**候选实现、自动验证与人工验收已完成，人工通过后工作区收口完成；未提交、未打标签、未推送，等待再次独立复核。**

## 独立复核整改后的最新证据

本节优先于下方保留的早期执行记录。首次独立复核结论为不通过；不将本次自测等同于独立复核通过。

|复核项|处理与当前结果|
|---|---|
|异层目标仍可能进入 Combat / 卡在 Chase|EnemyAIContext 每帧派生 CanInteractWithCurrentTarget。Roaming、Return、Investigate 的追击入口及 Combat 入口统一要求可交互；Chase/Investigate 异层转 Return；Combat 异层立即退出并取消主动攻击。感知记忆未被改写。|
|真实状态机测试不足|新增测试保持 EnemyAIController 启用，以公开感知配置和正式场景驱动 Roaming、Chase、Investigate、Return、Combat，验证层级改变前后转换、仍可感知但不攻击及恢复同层追击。另经实际 Update → Tick → Rigidbody2D 连续移动目标验证跨网格无倒退。|
|包契约测试不足|新增 null 输出异常与后续恢复、捕获及传播两种重入异常恢复测试；空列表合法。|
|长期架构文档过时|总图、当前 DLL 表、目标架构、Enemy 依赖、决策流程、状态图及抖动人工状态均同步；旧模块化验收表标为历史快照。22 张图逐字一致、幂等，页面渲染、缩放和拖动已由用户确认通过。|
|正式工程验收缺失|下表完整执行于真实工程，包含本轮状态机修复；不再借隔离结果代替。|

最新全量证据根：UnityGameSystems `Artifacts/Logs/Navigation2D/20260915-171613`。

|验证|最新结果|证据|
|---|---|---|
|真实 Lab EditMode / PlayMode|16/16、6/6|证据根/Lab/*.xml；15 项包 EditMode + 1 项 Lab 测试|
|Lab Standalone / 运行|构建成功、NAVIGATION_SMOKE_PASS|证据根/Lab/NavigationLabBuild.Build.log、smoke.log|
|真实 ARPG EditMode / PlayMode|176/176、10/10|证据根/ARPG/*.xml|
|真实 ARPG Standalone|构建成功|证据根/ARPG/NavigationValidation.Build.log|
|最新 ARPG 启动冒烟|60 秒启动观察完成，无异常；由测试脚本结束|ARPG Artifacts/Logs/Navigation2D/review-fixes-01/standalone.log、standalone.result.json；不是自然退出或人工游戏验收|
|完整 dotnet 解决方案|0 警告、0 错误|ARPG Artifacts/Logs/Navigation2D/review-fixes-01/dotnet-solution.log|
|Unity 真实加载与 DLL|73 个 GameObject，Missing Script/托管类型为 0|ARPG Artifacts/Logs/Navigation2D/audit-20260915-091754；包括全部场景、Prefab、ScriptableObject 加载|
|内容/元数据最终核对|ARPG+包 722、Lab+包 27，无重复/孤立/缺失；既有内容哈希与基线一致|UnityGameSystems Artifacts/Logs/Navigation2D/final-audit-20260915-172056|
|图生成器|22 张逐字一致、幂等|ARPG Artifacts/Logs/Navigation2D/review-fixes-01/diagrams.json|

最新隔离副本已核对全部 Assets/Scripts 与 Assets/Tests 文件哈希和真实仓库一致，复测 EditMode 191/191（176 项 ARPG + 15 项包）、PlayMode 16/16（10 项 ARPG + 6 项包）。证据：UnityGameSystems Artifacts/Logs/Navigation2D/review-fixes-isolated；复用此前空 Library 导入的隔离工程，本次不声称再次清空 Library。

Navigation2D 真实 DLL 仍仅依赖 netstandard、UnityEngine.CoreModule、UnityEngine.Physics2DModule。ARPG.Enemy 使用 World 与两项通用包，World 无 ARPG 反向引用。四项 Unity 自动改写逐项审查、归档后恢复；Bangers 原始脏文件哈希不变；两仓库 HEAD 不变、暂存区为空。

人工确认：用户先确认 Lab 正常、追击抖动消失，随后对本轮剩余清单明确回复“这部分人工测试没有问题”。确认范围：同层巡逻、追击、停止、返回和转角绕墙；玩家切层后敌人不攻击且不滞留 Chase，回到同层可重新追击；同层障碍隔离；楼梯完整通过和同端返回；架构查看器图的显示、缩放与拖动。观察者为用户，日期为 2026-09-15；记录来自用户确认，不冒充助手亲自观察或录像证据。

人工通过后再次核对全部已验证生产/测试源码哈希未变，没有新代码需要重复运行相同测试。完成既有内容哈希、GUID/meta、两仓库 HEAD、标签与暂存区检查，证据：UnityGameSystems `Artifacts/Logs/Navigation2D/final-audit-20260915-172450`。既有内容无额外变更、Bangers 哈希保持原值、暂存区为空；候选停在等待再次独立复核状态。

本轮不实现自主楼梯导航。统一决策事实限制 Enemy 选中的目标，不新增通用跨层技能或近战多目标空间过滤规则；这些应按玩法另行设计。记忆仅有 XY 时不推断跨层路径，使用仍存在目标的正式层级保守决定是否调查。


## 基线与停止约束

|仓库|执行前与当前 HEAD|开始时工作区|
|---|---|---|
|D:/Unity/Project/ARPG|e895862c7d281e0586129793163e1e22f7c5d1a6|仅 Bangers SDF.asset 既有修改|
|D:/Unity/Project/UnityGameSystems|83aa0457a9c4dd96243660adce183abfc13591a2|干净|

Unity 6000.3.21f1。未执行 commit、add、tag、push；暂存区为空。所有既有标签保持原指向，没有发布新标签。Bangers SHA-256 保持 `F551412F6599D7D2327E18E8004DE9B5CB0601EF2AE29F2C066DA5A8E1E07321`。

执行基线在两个仓库 `Artifacts/Logs/Navigation2D/20260915-112339-baseline`：HEAD、状态、已有差异、全部内容哈希与 tracked 清单。ARPG 基线 EditMode 169/169、PlayMode 7/7；既有 Standalone 构建入口通过；初始 Enemy dotnet 编译通过。未记录旧算法独立耗时对照，不把候选性能解释为加速比例。

## 真实修改范围

UnityGameSystems：新增唯一正式 `Packages/com.computerzhuxi.navigation2d`、`Projects/Navigation2DLab`、`Tools/InvokeUnityValidation.ps1`、`Tools/ValidateNavigation2D.ps1`、Navigation2D 历史记录；README 增加未发布候选入口。Health/Perception 包及已发布标签未修改。

ARPG：EnemyAIController 注入 owner；内部 GridPathfinder2D 改为公开包调用与 World 障碍域封装；EnemyNavigation 检查线段、域失效及安全拐点；上下文统一本层交互事实，追击/调查/战斗转换与执行共同使用；World 新增只读掩码查询。更新相关 asmdef、项目测试友元、manifest/lock、测试、Editor 审计/构建工具和导航架构/流程/数据图。

不迁移 Scene、Prefab、EnemyAIDefinition、SerializeReference 类型或字段；不更换已有 meta、MovedFrom 或 UnityEvent 身份。实际文件清单见 final-audit 的两个 status 文件，新增文件不能仅通过 git diff --stat 判断范围。

## 包边界与 API

命名空间/程序集 `Computerzhuxi.Navigation2D`。公共契约为 GridSettings2D、PathOptions2D、ITraversalSource2D、PhysicsTraversalSource2D、GridPathfinder2D、PathResult2D / PathStatus。详情见包内 API.md。

同步隐式网格 A*；四/八方向、禁止切角、完整圆形扫掠、稳定最小堆、复用内存、严格展开预算。失败清空输出，区分非法输入、受阻端点、网格连接失败、图耗尽及预算耗尽。不保存角色状态，不引入游戏依赖或任何 InternalsVisibleTo。

EnemyNavigation 唯一拥有目标、路径、索引、重算时钟和速度。World 唯一拥有层级与映射；GridPathfinder2D 只保存派生障碍源。楼梯预览不切域，正式提交才使路径失效；跨层入口/链接/通行编排留给未来项目适配层。当前地图边缘必须由障碍源限制，不提供地面存在检测。

用户确认的有意变化：受阻起点失败、整段净空检查、暂不自动脱困。另保留末端网格中心再追加精确终点，防止未验证的终点捷径；到达容差不能切掉仍必要的安全拐点。

## 早期自动验证记录（最新结果见首节）

正式可复现入口：`Tools/ValidateNavigation2D.ps1 -UnityEditor D:/Unity/Editor/6000.3.21f1/Editor/Unity.exe -ArpgProject D:/Unity/Project/ARPG`。

早期维护脚本证据根：UnityGameSystems `Artifacts/Logs/Navigation2D/20260915-125947`。

|验证|结果|证据|
|---|---|---|
|Lab EditMode|13/13|上述根/Lab/EditMode.xml：12 项包算法与契约 + 1 项真实 Sample Prefab 序列化|
|Lab PlayMode|6/6|上述根/Lab/PlayMode.xml：薄墙、动态障碍、半径、过滤、端点连接、Tilemap、性能|
|Lab Standalone|构建成功，运行标记通过|上述根/Lab/NavigationLabBuild.Build.log、smoke.log|
|ARPG EditMode|174/174|上述根/ARPG/EditMode.xml|
|ARPG PlayMode|8/8|上述根/ARPG/PlayMode.xml|
|ARPG 真实资产与 DLL|审计通过|上述根/ARPG/NavigationValidation.Audit.log；ARPG Artifacts/Logs/Navigation2D/audit-20260915-050125|
|ARPG Standalone|最终构建成功|上述根/ARPG/NavigationValidation.Build.log|
|ARPG dotnet 全解决方案|0 警告、0 错误|ARPG Artifacts/Logs/Navigation2D/candidate-05/dotnet-solution.log|

隔离证据根：UnityGameSystems `Artifacts/Logs/Navigation2D/isolated-20260915-124840`。副本仅含 Assets、ProjectSettings、Packages，无原 Library；候选包作为校验一致的嵌入快照，不成为第二份维护源。仍使用正常全局 UPM 下载缓存，不声称清除了整台机器缓存。

|隔离验证|结果|证据子目录|
|---|---|---|
|Lab 空 Library|EditMode 13/13、PlayMode 6/6|Lab-evidence|
|Sample 空项目 UPM 实际导入|Sample.Import 公共 API 成功|SampleEvidence/NavigationSampleImport.Import.log|
|独立 Sample 构建及运行|构建成功、NAVIGATION_SMOKE_PASS|SampleEvidence|
|ARPG 首次空 Library|EditMode 185/185|ARPG-evidence，173 项项目测试 + 12 项嵌入包测试|
|ARPG 同步最终转角修复后|EditMode 186/186、PlayMode 14/14|ARPG-evidence-final；分别含 12/6 项嵌入包测试|

ARPG 播放器为有界启动观察：15 秒首次尚未完成载入，保留为不充分证据；60 秒观察完成程序集载入、物理启动与场景 UnloadTime，无异常，然后由脚本停止测试进程。它不替代真实 PlayMode 游戏链或人工观察。最终构建对应日志为 ARPG `Artifacts/Logs/Navigation2D/candidate-05/standalone-final.log` 与 result.json。

### 性能与失败记录

固定墙体、格距 .25、半径 .2、预算 4096 的 100 次预热物理查询：一次记录 p50 1.1623 ms、p95 1.6375 ms、max 2.0570 ms，分配 0，展开 148，接口查询 1989。见 package candidate-03/PlayMode.log。后续维护脚本和隔离测试也记录各自机器时序，不混用为同一次采样。

这不是任意地图或大量敌人的帧预算承诺。新增更大地图或更多角色前应测量总调用次数和主线程预算；节点预算不等于毫秒时限。

保留的早期失败：Lab candidate-01 因平台字段类型错误执行 0 测试，不能算通过；ARPG candidate-01 为 3 项旧边界断言失败；candidate-02 为 Editor 内部 SyncVS 调用及 ARPG 自有 PlayMode 可见性编译错误。均修复并复测。一次复制测试被自动审批服务用量限制拒绝，用户继续后成功重试；一次文本锚点不匹配未产生写入。

## DLL、资产与 GUID

实际 Navigation2D DLL 引用仅 `netstandard`、`UnityEngine.CoreModule`、`UnityEngine.Physics2DModule`。ARPG.Enemy 新增 Navigation2D 与 ARPG.World 依赖；World 仍无 ARPG 反向依赖。实际 DLL 图和无环断言通过。包无测试友元；ARPG 自己的测试友元不跨入包内部。

Unity 真正加载所有 Scene、Prefab 和 ScriptableObject：检查 73 个 GameObject，Missing Script / 缺失托管类型为 0。最终内容审计见 UnityGameSystems `Artifacts/Logs/Navigation2D/final-audit-20260915-130233`：ARPG+包 722 个 GUID、Lab+包 27 个 GUID，无重复、孤立或缺失 meta；全部既有 Scene/Prefab/asset/meta 与开始时哈希一致。

Unity 自动改写的 UniversalRP.asset、UniversalRenderPipelineGlobalSettings.asset、ProjectSettings.asset、TimeManager.asset 已保存差异后恢复；Bangers 原有修改未覆盖或恢复。暂存区为空，两仓库 HEAD 未改变。

## 首次人工验收前的历史记录（最终确认见首节）

包 README/API/架构/维护/故障排查/CHANGELOG、调研和历史索引已更新；ARPG 架构、数据归属、流程和新导航图已更新。22 张交互图的数据与 Markdown 原文逐字一致，生成两次哈希一致。

浏览器安全策略禁止助手直接打开本地 file URL，因此**页面实际渲染仍待用户确认**，不以生成器通过冒充渲染通过。

人工观察者：用户。已明确反馈 Lab 没有问题、ARPG 抖动消失；以下详细清单不因这两项反馈而全部视为通过。

- Lab：NavigationLab 场景中路径与半径 Gizmo、绕墙、Reset、Toggle obstacle；不同半径的净空表现。
- ARPG：真实 SampleScene 的巡逻、追击、目标移动重算、停止及转角不卡墙。
- 空间层：当前层障碍隔离、楼梯完整通过及中途返回；不期待本轮敌人自动找楼梯。
- 文档：viewer/index.html 中 Navigation2D、Enemy、World 图正常显示，缩放/拖动可用。

人工确认后重新检查非目标资产与状态；若修复 Runtime，再进行相应全量验证。随后更新本记录为“候选完成，等待独立复核”。当前不能宣称独立复核通过。

## 下一步发布顺序

本任务不执行发布。独立复核通过并获得明确授权后：提交推送系统仓库 → 创建推送不可变 navigation2d-v0.1.0 标签 → 独立工程从远端标签安装验证 → ARPG 切换固定标签并全量测试 → 提交消费项目升级。固定 Git 安装验证尚未发生，不能由本地候选验证代替。

## 人工检查反馈与修复（2026-09-15）

用户确认 Lab 没有问题；ARPG 发现玩家移动时追击敌人抖动，ARPG 人工验收尚未通过。定位到重算路径会重新跟随身后的起点格中心，已在项目跟随层增加完整扫掠检查通过后跳过该中心的处理，并补充双方向连续目标更新回归测试。修复后结果见下文；上文自动化结果属于修复前版本。

修复后隔离副本验证：Artifacts/Logs/Navigation2D/moving-target-fix-02，EditMode 188/188（ARPG 176 + 包 12），PlayMode 14/14（ARPG 8 + 包 6）。moving-target-fix-01 保留新增 EditMode 测试误调用未初始化移动器而产生的两项失败记录，测试已修正为只验证路径重建。Bangers 原始脏文件 SHA256 保持一致，暂存区为空。用户随后明确确认抖动消失；修复后构建及最终全量收口仍待完成。

## 首次转交独立审查时的历史状态（已被首节整改更新）

用户要求现在提供报告交给另一对话审查，故本次交接为当前候选审查，不宣称全部验收完成。用户接受本轮同层导航范围，敌人自主选择楼梯上下楼推迟；异层追击被 CanNavigateTo 主动停止，不应误判为已实现跨层规划。

交接时重新读取两个仓库 HEAD 与工作区：HEAD 仍为上述基线，暂存区均为空；没有提交、打标签、推送。Bangers SHA256 再次与基线一致。当前未发现上述四个 Unity 自动改写资产出现在 Git 修改清单。本次状态核对不替代完整 GUID/资产加载审计重跑。

独立审查应直接检查两个真实仓库的所有修改及未跟踪文件，不使用旧隔离副本或 execution-scripts 作为权威源码。重点审查路径失败契约、端点连接/净空/拐角、移动目标重算后安全跳过起点格中心、同层障碍域选择、异层追击停止与状态机其他分支的一致性、序列化/GUID、依赖边界和测试有效性。

尚未完成：抖动修复后真实消费项目完整测试与 Standalone 重建、最终 DLL/资产/GUID 全量收口；ARPG 其余人工清单的逐项确认；文档图的实际渲染确认。最新隔离 EditMode 188/188 与 PlayMode 14/14 已通过，不能把修复前构建结果标成最新版本结果。

审查期间保持只读，不提交、不暂存、不打标签、不推送，不覆盖用户已有修改；如需启动批处理 Unity，先确认同一工程编辑器未被用户占用。报告发现、风险和证据不足项后，再决定候选是否具备最终验收条件。
