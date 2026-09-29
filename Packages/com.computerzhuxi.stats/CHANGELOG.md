# Changelog

## 0.1.0

- 新增纯 C# `StatRegistry`，支持绑定业务域基础值与最终值、修正句柄、来源清理和批处理。
- 默认按 `(Base + Flat 合计) × Multiplier 积` 计算；可通过 `IStatCalculationStrategy` 定义其他修正类别和公式。
- 提供基础值与最终值读数、变化事件、最终值约束及基础值快照。
- 提供组件式与编程式入口、完整 Sample、StatsLab 以及随包上手、API 和契约文档。

发布状态由不可变 Git 标签和发布记录证明，不能仅由本文件或 `package.json` 推断。
