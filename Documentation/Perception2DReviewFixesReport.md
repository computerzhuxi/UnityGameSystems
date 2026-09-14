# Perception 2D 复核问题修复报告

日期：2026-09-14。三项复核问题及后续查看器入口问题已修复。用户已于本对话逐项确认人工视觉验收通过。尚未提交、推送或创建标签。

## 修复内容

1. **交互架构图仍旧版**：重新生成 `Docs/Architecture/viewer/diagram-data.js` 的全部 21 个条目，并逐项对照 Markdown Mermaid 源块。项目总图已包含通用包及依赖边；敌人依赖图与感知决策流程图同步明确包、窄适配、每帧战斗过滤、已调查标记及新刺激的职责。生成器的感知图标题也已更新。
2. **通用包向 ARPG 测试开放内部实现**：移除 `InternalsVisibleTo("ARPG.Runtime.Tests")`，只保留包自身 EditMode 测试授权。未公开记录构造器，也未用反射构造记录。ARPG 不再调用内部快照构造器；死亡、恢复和新刺激重新调查的断言已迁入真实 Scene 的公开组件链路。包测试及 ARPG 架构测试均检查友元边界。
3. **编辑模式 Gizmo 朝向错误**：绘制时通过统一方向取值，在非运行模式直接读取序列化 `initialFacing`，运行时继续使用最后提交的 `facing`；零初始向量回退向右，与新实例运行时初始化一致。新增自动测试覆盖朝上、朝左和零向量的编辑器修改。

## 本轮验证

| 检查 | 结果 | 证据 |
| --- | --- | --- |
| Lab EditMode | 20/20 | [lab-editmode.xml](../Artifacts/Perception2D/ReviewFixes/lab-editmode.xml) |
| Lab PlayMode | 3/3 | [lab-playmode.xml](../Artifacts/Perception2D/ReviewFixes/lab-playmode.xml) |
| ARPG EditMode | 169/169 | [arpg-editmode.xml](../Artifacts/Perception2D/ReviewFixes/arpg-editmode.xml) |
| ARPG PlayMode | 6/6 | [arpg-playmode.xml](../Artifacts/Perception2D/ReviewFixes/arpg-playmode.xml) |
| dotnet build ARPG.sln --no-restore -m:1 | 0 警告、0 错误，退出码 0 | [编译日志](../Artifacts/Perception2D/ReviewFixes/dotnet-build.log) |
| 交互图源数据一致性 | 21/21 | [核对清单](../Artifacts/Perception2D/ReviewFixes/diagram-sync.json) |

Lab EditMode 从 18 增至 20，新增内部访问与 Gizmo 方向回归。ARPG EditMode 从 171 降至 169：删除两项依赖包内快照构造的测试，其关键行为断言并入 6 项 PlayMode 中的真实游戏链路。新 PlayMode 已验证死亡后排除、生命恢复且重新可见后追击、完成的信息不重复调查、重新可见的新刺激允许再次调查，以及整个过程中包记忆由公开查询读取。

首次新增恢复断言期待巡逻敌人仅因不可见目标恢复生命而进入调查，实际状态图只从巡逻响应可见目标，因此测试失败。已按现有行为改为恢复生命后重新开启视觉，再验证追击；未修改游戏状态图。原失败 XML 和日志保存在 `arpg-playmode-assertion-failure.*`。

本轮没有改变游戏运行时感知或决策规则，也没有重建 Windows Player；9 月 13 日的构建和 Sample 导入结果仍作为初轮证据保留，不冒充本轮构建。自动化复测结束时人工视觉验收尚未完成；随后已由用户在 Lab 与 Unity 编辑器中逐项观察确认，见下方记录。交互图自动化检查的是生成数据与源文档的一致性。

原始日志与本轮改动前备份位于 `Artifacts/Perception2D/ReviewFixes`。Unity 测试产生的 Bangers 字体缓存和 TimeManager 序列化变化已备份并恢复，其余既有工作区改动保持。双方 HEAD 未变。

最终 git diff --check 通过；仅清理 Unity 在已修改 Prefab/Scene 行上生成的 9 处尾随空格，序列化值未变。GUID 审计无冲突、无缺失脚本 meta。


## 用户人工验收记录（2026-09-14）

助手启动 Lab 独立窗口及 ARPG Unity 编辑器，提供操作步骤和预期现象；用户实际观察，每组回复“符合”。这些结论来自用户确认，助手未获取原生窗口截图，不作为助手独立视觉检查结果。

| 检查 | 用户确认结果 |
| --- | --- |
| Lab 面板和视觉关闭 | 按钮切换、visible=False、保留并过期记忆符合预期 |
| 听觉接收、过期和独立关闭 | 有来源声音记录、约 5 秒过期、关闭后不接收均符合 |
| 无来源声音 | 独立计数增加、无目标记录、分别过期符合 |
| 遮挡与重新发现 | 墙后 Lost，恢复位置后 Acquired，面板状态符合 |
| Scene 范围及编辑模式朝向 | 发现、丢失、听觉范围与配置一致；修改初始朝向及零向量回退符合 |
| Play Mode 实时朝向 | Gizmo 跟随敌人实际转向，不固定在初始方向，符合 |

此前运行时修改 Inspector 的 Sight Enabled 无效属于初始化配置语义；有效启停通过 Lab 按钮调用公开 SetSenseEnabled API 验证。查看器入口已补入 09-perception-architecture.md 并核对页面、图 ID 和源图一致性。

本次人工验收项已闭合；提交、不可变版本标签及发布尚未执行。
