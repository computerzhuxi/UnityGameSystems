# 快速入门 (Getting Started)

本指南介绍 Health System 最基础的使用方式。

完成后你将能够：
1. 创建生命状态并读取当前生命值
2. 造成伤害、进行治疗
3. 处理死亡与复活
4. 监听生命变化事件
5. 理解 `Health` 与 `HealthComponent` 的区别

---

## 🧭 1. 核心选择：两种使用方式

Health System 提供两种主要入口，它们底层遵循完全相同的生命规则，区别仅在于**谁负责创建和持有生命实例**：

| 入口 | 适用场景 | 核心特点 |
| :--- | :--- | :--- |
| **`HealthComponent`** | 希望直接在 Unity 场景中配置生命值，使用 Inspector 或 UnityEvent。 | 挂载到 GameObject；负责生命周期跟随；支持可视化配置。 |
| **`Health` (Core)** | 项目已有成熟的角色架构（Entity、Model等），希望自主管理生命周期。 | 纯 C# 对象；不依赖 MonoBehaviour；所有权明确。 |

> ⚠️ **最佳实践**：对于同一个游戏实体，通常只应该存在**一个**权威生命实例。

---

## ⚙️ 2. 初始化与状态读取 (以 HealthComponent 为例)

### 2.1 挂载与配置
给 GameObject 添加 `HealthComponent` 组件。
组件在 Inspector 中提供以下初始化配置：
*   **Maximum**：最大生命值（例如 100）。
*   **Start With Full Health**：是否满血启动（开启时 `Current = Maximum`）。
*   **Starting Health**：若关闭满血启动，则使用此值（例如 50，此时状态为 50 / 100）。

> **注意**：`Starting Health` 必须满足 `0 <= Starting Health <= Maximum`，否则会导致初始化失败。

### 2.2 读取生命状态
通过 `HealthComponent.State` 获取**只读**生命状态接口（`IReadOnlyHealth`），它允许读取状态和监听事件，但**不能**直接修改生命值。

```csharp
using Computerzhuxi.Health;
using UnityEngine;

public sealed class HealthExample : MonoBehaviour
{
    [SerializeField]
    private HealthComponent health;

    private void Start()
    {
        Debug.Log(health.State.Current);     // 当前生命值
        Debug.Log(health.State.Maximum);     // 最大生命值
        Debug.Log(health.State.IsAlive);     // 是否存活
        Debug.Log(health.State.Normalized);  // 生命比例 (0.0 ~ 1.0)
    }
}
```

---

## ⚔️ 3. 核心功能：伤害、治疗、死亡与复活

### 3.1 造成伤害 (Damage)
```csharp
health.Damage(20);
```
*   **常规扣血**：`100 / 100` ➔ `Damage(20)` ➔ `80 / 100`
*   **下限保护**：生命值不会低于 0（如 `10 / 100` ➔ `Damage(50)` ➔ `0 / 100`）。
*   **无效伤害**：非正数伤害（`0` 或 `-10`）或对已死亡对象调用不会产生变化。

### 3.2 治疗 (Heal)
```csharp
health.Heal(20);
```
*   **常规回血**：`50 / 100` ➔ `Heal(20)` ➔ `70 / 100`
*   **上限保护**：过量治疗会被截断（如 `90 / 100` ➔ `Heal(50)` ➔ `100 / 100`）。
*   **不能复活**：`Heal` 不能复活死亡对象（`0 / 100` ➔ `Heal(50)` ➔ `0 / 100`）。

### 3.3 死亡 (Kill) 与 复活 (Revive)
```csharp
// 直接令对象死亡 (Current = 0)
health.Kill();

// 显式复活死亡对象，恢复 50 点生命值
health.Revive(50);
```
> **注意**：`Revive` 的值必须满足 `1 <= value <= Maximum`。如果对象当前仍存活，合法的 `Revive` 调用不会重设当前生命值。

---

## 📡 4. 事件监听与执行结果

### 4.1 监听生命变化
**方式一：使用 UnityEvent (无参数，适合 Inspector 拖拽)**
包含：`OnChanged`, `OnDamaged`, `OnHealed`, `OnDied`, `OnRevived`。

**方式二：监听强类型事件 (含详细参数，适合代码逻辑)**
如果你需要知道变化前后的具体生命值，请监听 `State`：
```csharp
private void OnEnable()
{
    health.State.Changed += OnHealthChanged;
}

private void OnDisable()
{
    health.State.Changed -= OnHealthChanged;
}

private void OnHealthChanged(HealthChange change)
{
    Debug.Log($"{change.Before.Current} -> {change.After.Current}");
}
```

### 4.2 使用命令返回值 (HealthChange)
每个生命命令都会返回一个 `HealthChange` 结构，精确描述了该次操作的前后差异。

```csharp
// 当前状态为 20 / 100
HealthChange result = health.Damage(50);

if (result.HasChanged)
{
    // result.Before.Current 为 20
    // result.After.Current 为 0
    // 虽然请求了 50 点伤害，但实际只扣除了 20 点。
    Debug.Log($"实际伤害: {result.ActualAmount}"); // 输出 20
}
```

---

## 🛠️ 5. 高级操作

### 5.1 修改最大生命值 (ChangeMaximum)
提供两种策略：
1.  **`PreserveCurrent` (保留当前)**：尽量保留当前生命值。
    `80 / 100` ➔ 改为上限 200 ➔ `80 / 200`
2.  **`Refill` (回满)**：使用新上限并立即回满。此操作**可以**让死亡对象重新存活。
    `30 / 100` ➔ 改为上限 200 ➔ `200 / 200`

```csharp
health.ChangeMaximum(200, MaximumHealthPolicy.PreserveCurrent);
```

### 5.2 恢复完整状态 (Restore)
用于一次性恢复 `Current` 和 `Maximum`，结果直接变为指定的数值。
```csharp
// 直接将状态变为 50 / 120
health.Restore(50, 120);
```
> **💡 Restore 的特殊语义**：
> `Restore` 更适合用于**读档**或**网络状态同步**。与 Gameplay 命令不同，**有实际状态变化时只会发布 `Changed`**；即使发生 `Alive ➔ Dead` 或 `Dead ➔ Alive`，也不会额外发布 `Died` 或 `Revived`。如果恢复前后状态完全相同，则不会发布任何事件。

---

## 🚀 6. 不依赖组件：直接使用 Health Core

如果不需要 MonoBehaviour，可以直接实例化并操作：
```csharp
using Computerzhuxi.Health;

// 初始化：当前 80，最大 100
var health = new Health(current: 80, maximum: 100);

health.Damage(20);
health.Heal(10);
health.Kill();

health.Changed += change =>
{
    Debug.Log($"{change.Before.Current} -> {change.After.Current}");
};
```

---

## 🎮 7. 导入 Sample (强烈推荐)

对于第一次接触 Health System 的开发者，强烈建议通过 Unity Package Manager 导入：
👉 **Basic Health Demo**

该 Sample 提供了可视化的完整演示，涵盖了组件挂载、代码监听、各类生命命令（Damage/Heal/Kill/Revive/Restore）的实机效果，以及独立 Core 与 Component 的对比使用。

---

## 🗺️ 下一步阅读建议

完成本基础指南后，你可以根据需要继续深入：
*   想了解更深的机制？👉 [生命模型](concepts/health-model.md) 或 [命令与事件](concepts/commands-and-events.md)
*   准备接入实际项目？👉 [直接使用 Health Core](guides/core-usage.md) 或 [使用 HealthComponent](guides/unity-component.md)