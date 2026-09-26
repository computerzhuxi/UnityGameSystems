# Health System

独立整数生命系统，目标 Unity 版本为 6000.3。它提供当前生命与上限、伤害、治疗、死亡、复活、上限变化、状态恢复、变化结果和同步事件。

包不计算命中、防御、无敌或攻击力，也不拥有游戏的奖励、存档格式、UI 或动画。这些规则由消费项目接入。

## 安装

在 Unity Package Manager 中安装固定的已发布版本示例：

    https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1

下文提到的组件销毁解绑、Demo 的 `Restore` 按钮和独立 Core 演示属于本地 **Unreleased** 工作树；固定 `health-v1.0.1` 标签不包含这些改动。要体验新增内容，请在本仓库的 HealthLab 使用本地 `file:` 包。私有仓库需要开发机或 CI 配置只读凭据；不要将凭据写入 URL 或 manifest。

## 两种入口

|入口|适用场景|生命状态归属|
|---|---|---|
|`HealthComponent`|在 Inspector 配置初值和 UnityEvent，直接给 GameObject 装配|组件创建并持有一个 `Health`|
|`Health` 与 `IReadOnlyHealth`|现有角色架构自行组合命令、事件和生命周期|调用方创建并持有一个 `Health`|

两者执行相同的 Core 规则。一个角色应选择一个权威生命实例；已有业务门面可以直接组合 Core，无需额外挂 `HealthComponent`。组件禁用与重启不重置生命；组件销毁时解除事件转发。Core 实例的生命周期由持有它的代码管理。

## 阅读与示例

- [从安装到双入口的完整上手](Documentation~/GettingStarted.md)
- [文档导航](Documentation~/Index.md)
- [精确 API 契约](Documentation~/API.md)
- [版本迁移](Documentation~/Migration.md)
- [更新记录](CHANGELOG.md)

本地 **Unreleased** Sample 在同一界面中演示组件入口、`Restore` 和独立 Core；HealthLab 保存其镜像用于本地验证。固定 `health-v1.0.1` 的 Sample 仍是旧界面。
