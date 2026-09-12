# Health 架构与边界

## 组成

    简单 Unity 项目 ─────▶ HealthComponent ──▶ Health Core
    中型项目适配层 ─────────────────────────▶ Health Core
    纯逻辑代码 ─────────────────────────────▶ Health Core

|程序集|职责|依赖|
|---|---|---|
|Computerzhuxi.Health.Core|生命规则、状态、结果和只读契约|netstandard|
|Computerzhuxi.Health.Unity|MonoBehaviour、Inspector 和 UnityEvent 接入|Core、UnityEngine.CoreModule|

## 数据归属

Health Core 唯一拥有 Current 和 Maximum。IsAlive、IsDead 和 Normalized 都由这两个值推导，不单独存储。适配层和 UI 不应复制一份可写生命状态。

## 接口方向

- 命令提交者持有 Health 或 HealthComponent。
- 观察者优先持有 IReadOnlyHealth。
- HealthChange 携带命令原因以及变化前后的不可变快照。
- Core 先提交完整状态，再同步发布事件。

## 边界

包负责生命本身，不负责伤害计算来源。Combat 应先完成命中、阵营、无敌、防御等判断，再把最终伤害提交给 Health。

等级成长、奖励、状态机、表现和存档格式属于游戏业务。它们可以通过项目适配层调用 Health，但不能成为 Health 包的反向依赖。

## 为什么保留 Unity 层

简单项目可以直接通过组件和 Inspector 使用系统，不必编写适配层；已有成熟架构的项目可以绕过 HealthComponent，直接组合 Core。两种方式共享同一套生命规则。
