# Perception 2D 实施与验收报告

> 本文记录首轮验收；9 月 14 日的三项复核修复及最新测试结果见 [复核修复报告](Perception2DReviewFixesReport.md)。

日期：2026-09-13。状态：本地候选已实现并完成下列自动化验收，等待复核。没有提交、推送或创建发布标签。

## 交付

- 正式源码：`Packages/com.computerzhuxi.perception2d`；版本 0.1.0，程序集与命名空间 `Computerzhuxi.Perception2D`。
- 显式 World、目标代次身份、视觉与事件听觉、独立启停、分感官记忆、只读列表与单目标查询、综合位置、异常隔离的批次事件、调试 Gizmos。
- `Projects/Perception2DLab` 本地引用包；独立 Sample 和 Windows 演示构建。
- ARPG 已替换旧观察者，目标 GUID 保留；Actor 无感知依赖，通过窄适配提交朝向。Enemy 每帧过滤生命与阵营，优先可见目标，随后调查记忆。调查完成只登记已消费信息，保留包内记忆；听觉关闭。
- [API 文档](../Packages/com.computerzhuxi.perception2d/Documentation~/API.md)、[行为契约](../Packages/com.computerzhuxi.perception2d/Documentation~/Contracts.md)、[接入与行为差异](Perception2DImplementation.md)。ARPG 侧另有 `Docs/Architecture/09-perception-architecture.md`。

## 最终验证

Unity 6000.3.21f1，Windows；所有 Editor 实例串行运行。

| 项目 | 结果 | 原始证据 |
| --- | --- | --- |
| Lab EditMode | 18/18 通过 | [lab-final-editmode.xml](../Artifacts/Perception2D/lab-final-editmode.xml) |
| Lab PlayMode | 3/3 通过 | [lab-final-playmode.xml](../Artifacts/Perception2D/lab-final-playmode.xml) |
| ARPG EditMode | 171/171 通过 | [arpg-final-editmode-02.xml](../Artifacts/Perception2D/arpg-final-editmode-02.xml) |
| ARPG PlayMode | 6/6 通过 | [arpg-final-playmode-03.xml](../Artifacts/Perception2D/arpg-final-playmode-03.xml) |
| Lab Windows 构建 | 成功，退出码 0 | [构建日志](../Artifacts/Perception2D/lab-final-build.log) |
| Lab 独立程序 | 视觉、听觉、关闭听觉验证通过，退出码 0 | [运行日志](../Artifacts/Perception2D/lab-player-smoke.log) |
| ARPG Windows 构建 | 成功，退出码 0 | [构建日志](../Artifacts/Perception2D/arpg-final-build.log) |
| 空项目 UPM Sample 导入 | 官方 Sample.Import 接口成功 | [导入日志](../Artifacts/Perception2D/sample-import.log) |
| 导入后重新打开编译 | 成功，生成 Sample DLL；导入源码与包内样例逐字节一致 | [编译日志](../Artifacts/Perception2D/sample-compile.log) |

Lab 首次从空 Library 完成导入和编译，首次测试中的注册断言失败及后续修正日志均保留。额外 Sample 项目也从无 Library 的最小项目开始，未依赖 ARPG 或 Lab 缓存。

覆盖纯视觉、纯听觉、组合记录、分别过期、独立启停、无来源声音、多观察者和 World 隔离、40 个子 Collider 去重、遮挡与 Trigger、视角和发现/丢失距离、重入与异常隔离、暂停、销毁、对象池代次、恢复首批过期。ARPG 真实 SampleScene 覆盖绑定、追击→调查→返回、调查保留记忆、死亡销毁，以及感知版本不变时排除死亡目标。

实际 DLL 元数据检查通过：[引用图](../Artifacts/Perception2D/dll-references.tsv)。包运行时只引用 UnityEngine.CoreModule、UnityEngine.Physics2DModule 和 netstandard；Actor 不引用感知包。11 个 ARPG 生产模块的引用图与无环检查保持通过。

构建产物：系统仓库 `Artifacts/Perception2D/Build/Perception2DLab.exe`，ARPG 仓库 `Artifacts/Perception2D/Build/ARPG.exe`。上述程序验证使用无图形模式；调试 Gizmos 与交互面板的人工视觉复核留给本次候选复核，不将自动化运行表述为人工试玩。

## 迁移和保护

迁移先在 `.perception-stage/ARPG-Isolated` 独立工程通过 Unity 编辑器 API 完成，再将白名单变更复制到正式工程。隔离工程移除旧实现和迁移桥后 EditMode 171/171、PlayMode 4/4 通过；最终正式工程已覆盖新增回归。临时迁移代码归档在证据目录，不进入正式运行时。

原目标 GUID `a2b5e42013744c79a317b54c608d13ac` 当前唯一归属包内 Target 脚本，保留历史 MovedFrom 信息。Prefab 与 Scene 已通过真实资源加载。全量基线 Prefab/Scene 未发现旧感知字段 Override，因此本次没有需要转写的感知字段 Override；不将一次性迁移脚本声明为支持任意未来 Prefab Variant 的通用迁移工具。

基线记录 885 个资产及 meta。最终相对基线仅 Enemy.prefab、SampleScene 改动，及六个已移除旧实现的 meta 离开原目录；Target meta 已随源码搬入包。无脚本 GUID 冲突，无脚本 meta 缺失。详见 [资产审计](../Artifacts/Perception2D/asset-audit.json)、[基线差异](../Artifacts/Perception2D/final-baseline-diff.json)。

Unity 测试/构建自动产生的字体缓存、URP 预过滤数据、运行时渲染配置、PlayerSettings 批处理字段及 TimeManager 序列化升级已备份并恢复到初始内容。未修改已有 Systems README 和 SystemExtractionPlaybook。保留迁移前备份 `arpg-pre-migration.zip` 和全部失败证据。

双方 HEAD 保持不变：ARPG `9d77293df3d1762a9e1b2c9e2b67078805dea657`；UnityGameSystems `cf3a00ca160a65eaec7f27c27040d04b49931a0e`。ARPG 原未推送提交未改写。

## 验证中修复的问题

- 旧声音跨重置通知、Bind 吞掉绑定后的命令、同批重获仍误发遗忘：新增回归先得到 14/17，再修复通过。
- 超龄当前视觉在长间隔扫描确认丢失后多保留一批：新增回归先得到 17/18，再修复为本批过期；恢复前结束旧持续视觉。
- 项目仅按感知版本筛选，死亡目标继续调查：真实场景先复现 Investigate，改为每帧筛选战斗事实。测试允许返回中或已返回巡逻。
- 死亡状态立即销毁宿主，跨帧读取状态图的原测试断言不适用：改为验证销毁生命周期。
- 首次 EditMode 生命周期模拟、隔离工程 solution 名、空初始 Scene 恢复测试问题已修正。历史许可退出 198、UPM IPC 超时保留日志，串行重跑通过。

## 复核边界

首版仅 2D、单检测点、无声音墙体衰减、无空间索引。目标集合较大时视觉扫描和每帧游戏筛选成本需要按目标项目规模测量；本轮没有承诺大规模性能指标。配置使用组件序列化字段，包不负责阵营、生命、目标评分、导航或行为决策。

本地 UPM 路径要求两个正式仓库维持当前相对位置；候选复核后才能另行安排发布。未来发布必须使用新建不可变标签，并更新消费项目的 manifest/lock。
