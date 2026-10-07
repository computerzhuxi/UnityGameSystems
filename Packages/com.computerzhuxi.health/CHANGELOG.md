# Changelog

## Unreleased

- 修复 `HealthComponent` 销毁后仍可能转发 Core 事件的问题。
- **Basic Health Demo** 增加 `Restore` 和纯代码 `Health` 使用示例。
- 补全 README 的首次组件接入和事件订阅示例，修正 Core 的 `Restore` 调用示例及降低上限时的说明。
- 整理 **Basic Health Demo** 的组件、独立 Core 与冒烟入口，保留原有演示行为。

## 1.0.1

- 加强 `HealthComponent` 初始配置校验。
- 非法的最大生命值或初始生命值会在初始化时明确报错。

## 1.0.0

- 提供独立的 `Health` 生命核心。
- 提供 `HealthComponent` Inspector 组件和 UnityEvent。
- 支持伤害、治疗、死亡、复活和状态恢复。
- 支持最大生命值修改策略。
- 提供生命变化事件和 `HealthChange` 结果。
- 提供 **Basic Health Demo**。
