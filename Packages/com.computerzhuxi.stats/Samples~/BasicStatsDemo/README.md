# Basic Stats Demo

> [安装与使用指南](../../README.md)

通过 Package Manager 导入 **Basic Stats Demo**，在 Project 窗口找到导入目录（通常为 `Assets/Samples/Stats System/0.1.0/Basic Stats Demo`），打开 `BasicStatsDemo.unity` 并进入 Play 模式。该场景展示两名独立角色：一名使用 `StatCollectionComponent`，另一名使用纯 C# `StatRegistry`；每个角色有自己的业务字段、绑定和注册，不共享权威状态。

初始组件角色显示 HP `100/100`、攻击 `30`、移速 `5`；纯 C# 角色显示 HP `80/80`、攻击 `25`、移速 `4`、自定义 Bonus `7`。按以下顺序操作：

| 操作 | 预期结果 |
| --- | --- |
| 安装同来源装备 | 两名角色攻击分别为 `60` 和 `54`，即 `(Base + 20) × 1.2`；重复点击不会叠加 |
| 两名角色永久成长 | 攻击为 `66` 和 `60`；HP 为 `100/110` 和 `80/90`，提高上限不会自动治疗 |
| 查看基础快照 | 保存当前基础攻击 `35` 和 `30`，以及其他属性的 Base |
| 再次点击两名角色永久成长 | 攻击为 `72` 和 `66`；HP 为 `100/120` 和 `80/100` |
| 恢复基础快照 | 攻击回到 `66` 和 `60`；HP 回到 `100/110` 和 `80/90`，装备和自定义修正仍生效 |
| 卸下同来源装备 | 攻击回到当前基础值 `35` 和 `30`；移速和 Bonus 不变 |

未保存快照时点击“恢复基础快照”不会变化。快照仅保存 Base；示例以 `SetBase` 批量恢复，存档格式、当前生命及临时修正的恢复规则由业务方决定。示例中降低生命上限时，业务绑定会将当前生命钳制到新上限；这属于角色绑定职责，Stats 不内置生命系统。

同目录的 `SampleCharacterStats.cs` 实现 `IStatBindingProvider`，为组件角色提供 `maxHealth`、`attack`、`moveSpeed` 的绑定；`ComponentRole.prefab` 已配置提供者和 `Definitions`。`BasicStatsDemo.cs` 的 `Start` 创建纯 C# 角色及自定义 `BonusStrategy`，`AddEquipment` / `RemoveEquipment` 展示修正来源生命周期，`ApplyGrowth` 展示永久成长，`CaptureSnapshot` / `RestoreSnapshot` 展示基础快照；`OnDestroy` 释放纯 C# 注册。

面板由 `OnGUI` 绘制，窄窗口可向下滚动查看按钮和状态；若本机没有受支持的中文系统字体，示例回退到 Unity 默认字体。Standalone 启动参数 `--stats-smoke` 仅检查两名角色的初始攻击与自定义 Bonus，以 `STATS_SMOKE_PASS` 和退出码 0 表示成功；完整操作结果按上述流程人工验收。

本 README 属于本地 **Unreleased** 文档补充，固定 `stats-v0.1.0` 标签未包含此说明；标签 Sample 的现有场景、按钮和数值与上述流程对应。
