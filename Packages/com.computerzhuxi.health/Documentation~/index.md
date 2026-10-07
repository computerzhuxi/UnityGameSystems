# Health System 文档首页

Health System 是一个独立的整数生命值模块，用于管理生命值、伤害、治疗、死亡、复活、生命上限变化和状态恢复。

它将纯 C# 的生命核心与 Unity 场景接入分离，你可以根据项目架构选择直接使用 `Health`，或者使用 `HealthComponent`。

---

## 🏁 从这里开始
如果你是第一次使用 Health System，建议先从快速入门开始，它会带你完成从创建生命状态到监听生命变化的完整基础流程：
👉 **[快速入门 (Getting Started)](getting-started.md)**

---

## 🧠 核心概念
如果你已经能运行基本示例，接下来建议深入了解 Health System 的状态模型和行为规则。

- 🧬 **[生命模型 (Health Model)](concepts/health-model.md)**
  *探讨 `Current` 与 `Maximum` 的关系、死亡判定机制、只读接口 `IReadOnlyHealth` 以及状态快照的所有权分配。*
- ⚔️ **[命令与事件 (Commands and Events)](concepts/commands-and-events.md)**
  *详解 `Damage` / `Heal` / `Kill` / `Revive` / `ChangeMaximum` / `Restore` 命令的区别，以及它们对应的事件触发顺序。*

---

## 🧩 接入项目
Health System 提供两种主要接入方式。请根据你的项目架构进行选择：

### 方案 A：[直接使用 Health Core](guides/core-usage.md)
适合已经有成熟 Entity、Model 或 Gameplay 架构的项目。直接在代码中创建并持有 `Health` 实例。
- **优势**：不依赖 `MonoBehaviour`、生命周期自主管理、强类型事件、状态所有权高度清晰。

### 方案 B：[使用 HealthComponent](guides/unity-component.md)
适合希望直接在 Inspector 中配置生命初始值，并深度依赖 Unity 场景逻辑的项目。
- **优势**：GameObject 直接挂载、提供可视化的参数配置、原生支持 `UnityEvent`。

> ⚠️ **最佳实践**：对于同一个游戏实体，通常**只应该存在一个权威生命实例**。
> 强烈不建议同时维护一个 `HealthComponent` 和另一个独立的 `Health` 然后手动同步两份状态。

---

## 📖 API 参考
如果你已经知道自己要使用哪个类型或方法，可以直接查阅技术字典：
👉 **[API 参考 (API Reference)](reference/api.md)**

这里详细记录了 `Health`、`IReadOnlyHealth`、`HealthChange`、各个命令方法的签名、事件委托定义以及可能抛出的异常行为。

---

## 🎮 示例 (Samples)
我们提供了 **Basic Health Demo**，你可以通过 Unity Package Manager 导入。
该示例直观地展示了：
- `HealthComponent` 与 独立 `Health` 的实际使用对比。
- 完整命令流转（Damage, Heal, Kill, Revive, Restore 等）的实机演示。
- UnityEvent 与强类型状态事件的响应方式。

---

## ❓ 常见问题 (FAQ)
如果你在开发中遇到以下疑问：
- `Heal` 为什么不能复活目标？
- `Kill` 为什么没有触发 `Damaged` 事件？
- `Restore` 为什么不会触发 `Died` 或 `Revived`？
- 为什么在事件回调里再次调用 `Damage` 抛出了异常？
- 为什么禁用再启用 GameObject 后，生命值没有重置？

请查阅 👉 **[故障排查 (Troubleshooting)](troubleshooting.md)** 寻找详细解答。

---

## 🗺️ 推荐阅读路线

为不同阶段的开发者提供的推荐导航路径：

**🔰 第一次接触：** 
[快速入门](getting-started.md) ➔ [生命模型](concepts/health-model.md) ➔ [命令与事件](concepts/commands-and-events.md)

**🛠️ 准备接入项目：** 
阅读 [直接使用 Health Core](guides/core-usage.md) 或 [使用 HealthComponent](guides/unity-component.md)

**🔍 开发中查阅：** 
跳转 [API 参考](reference/api.md) 或 [故障排查](troubleshooting.md)