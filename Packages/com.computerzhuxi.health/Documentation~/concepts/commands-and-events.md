# 命令与事件 (Commands and Events)

Health System 不允许外部直接修改 `Current` 或 `Maximum`。所有的生命状态变化都必须通过明确的命令完成：
`Damage` | `Heal` | `Kill` | `Revive` | `ChangeMaximum` | `Restore`

每个命令都会返回一个 `HealthChange` 结构，用来精确描述这次操作前后的状态变化。

---

## 📦 核心数据结构：HealthChange

每次命令执行后都会返回该结构：
```csharp
HealthChange change = health.Damage(50);
```

`HealthChange` 包含以下核心字段：
- `Reason`：触发变化的命令类型（如 Damage, Heal, Kill 等）
- `Before`：操作前的生命状态
- `After`：操作后的生命状态
- `HasChanged`：状态是否发生了实质性变化
- `Delta`：生命值变化的数学差值
- `ActualAmount`：**实际生命变化量**（非命令输入绝对值）

**💡 示例解析**：
当前状态为 `10 / 100`，执行 `Damage(50)`：
- `Before.Current` = 10，`After.Current` = 0
- `Delta` = -10
- `ActualAmount` = 10（虽然传入 50，但实际只扣除了 10）
- `Reason` = Damage
- `HasChanged` = true

---

## ⚔️ 核心命令 (Commands)

### 1. Damage (伤害)
用于对存活对象造成伤害。
- **生效条件**：`amount > 0` 且 `IsAlive == true`
- **下限保护**：伤害不会让 Current 低于 0。
  `10 / 100` ➔ `Damage(50)` ➔ `0 / 100`
- **无效伤害**：非正伤害（`<= 0`）或对已死亡对象造成伤害，不会产生变化（`HasChanged = false`），也不发布任何事件。
- **事件顺序**：
  - 普通伤害：`Damaged` ➔ `Changed`
  - 致死伤害：`Damaged` ➔ `Changed` ➔ `Died`

### 2. Heal (治疗)
用于恢复存活对象的生命值。
- **生效条件**：`amount > 0` 且 `IsAlive == true`
- **上限保护**：治疗不会超过 `Maximum`。
  `90 / 100` ➔ `Heal(50)` ➔ `100 / 100`
- **无效治疗**：非正治疗（`<= 0`）无变化、无事件。
- **⚠️ 核心规则：Heal 不能复活**
  死亡对象不会被 Heal 修改，必须通过 `Revive` 显式复活。
  `0 / 100` ➔ `Heal(50)` ➔ `0 / 100`
- **事件顺序**：`Healed` ➔ `Changed`（绝不触发 `Revived`）

### 3. Kill (强制死亡)
直接令对象进入死亡状态（`Current = 0`）。
- **⚠️ 核心规则：Kill 不是 Damage**
  Kill 表示直接结束生命，而不是造成了一次巨大伤害，因此 **不会** 发布 `Damaged` 事件。
- **重复调用**：对已死亡对象调用 Kill，`HasChanged = false`，不重复触发 `Died`。
- **事件顺序**：`Changed` ➔ `Died`

### 4. Revive (复活)
用于显式复活死亡对象。
- **合法范围**：`1 <= value <= Maximum`。传入越界值会抛出 `ArgumentOutOfRangeException`。
- **对存活对象无效**：`Revive` 只负责 `Dead ➔ Alive`，不能用来重设存活对象的生命值。
  `30 / 100` ➔ `Revive(80)` ➔ `30 / 100`
- **事件顺序**：`Changed` ➔ `Revived`

### 5. ChangeMaximum (修改生命上限)
修改最大生命值（新值必须 `>= 1`），提供两种策略：
- **`PreserveCurrent` (尽可能保留当前生命)**
  如果降低上限，会截断当前生命以满足 `Current <= Maximum`。此策略**不会**复活死亡对象。
  `80 / 100` ➔ 改为 `50` ➔ `50 / 50`
- **`Refill` (回满)**
  直接将 `Current` 设置为新的 `Maximum`。此策略**可以**复活死亡对象（触发 `Revived`）。
- **Reason**：无论哪种策略，产生的 `Reason` 均为 `Maximum`。

### 6. Restore (状态恢复)
用于一次性将系统恢复到一个指定的**完整权威状态**。不会自动 Clamp 非法数据（非法会抛异常）。
- **典型场景**：读档、Checkpoint 恢复、网络状态同步。
- **⚠️ 核心规则：有实际变化时只发布 Changed**
  `Restore` 不是游戏行为（Gameplay）。状态发生变化时只发布 `Changed`；状态完全相同时不发布事件。即使跨越生死状态，也绝不发布 `Died` 或 `Revived`。

---

## 📡 事件系统 (Events)

### 通用观察入口：`Changed`
只要一次命令真正修改了 `Current` 或 `Maximum`，就会发布 `Changed`。UI 血条、日志、状态同步等只关心“状态变了”的系统，仅需监听此事件。

### 专用语义事件
| 事件名称 | 触发条件 | 备注 |
| :--- | :--- | :--- |
| `Damaged` | `Damage` 导致实际扣血时 | 致死也会触发；`Kill` 不触发。 |
| `Healed` | `Heal` 导致实际回血时 | 满血再 Heal 不触发。 |
| `Died` | 非 Restore 命令导致 `Alive ➔ Dead` 时 | 由致死 `Damage` 或 `Kill` 触发。 |
| `Revived` | 非 Restore 命令导致 `Dead ➔ Alive` 时 | 由 `Revive` 或 `ChangeMaximum(Refill)` 触发。 |

### 事件触发顺序速查
| 触发命令 | 产生的事件序列 |
| :--- | :--- |
| **普通 Damage** | `Damaged` ➔ `Changed` |
| **致死 Damage** | `Damaged` ➔ `Changed` ➔ `Died` |
| **Heal** | `Healed` ➔ `Changed` |
| **Kill** | `Changed` ➔ `Died` |
| **Revive** | `Changed` ➔ `Revived` |
| **ChangeMaximum** (普通) | `Changed` |
| **ChangeMaximum** (复活) | `Changed` ➔ `Revived` |
| **Restore** | `Changed` (无附加事件) |

---

## 📐 核心设计原则

### 1. 没有变化就没有事件
如果命令导致 `HasChanged = false`，Health 系统不会发布任何事件。

### 2. 状态先行，事件在后
Health 会**先提交完整的新状态，再发布事件**。
当你收到事件回调时，读取到的已经是最新状态。`change.Before` 和 `change.After` 提供了清晰的对比。

### 3. 🚫 不允许同步重入 (No Synchronous Re-entrancy)
Health **严格禁止**在自己的事件回调中同步再次提交生命命令。
```csharp
health.Changed += change => {
    health.Heal(10); // ❌ 抛出 InvalidOperationException
};
```
**原因**：防止事件顺序不可预测、操作边界模糊以及递归死循环。如果确实需要链式反应，请将下一条命令延迟到当前事件调用栈结束之后执行。

---

## 🎯 最佳实践：使用返回值 还是 使用事件？

**使用返回值 (`HealthChange`)**：适合命令的**发起方**。
发起方通常需要立即知道命令的结果（如：计算本次实际吸血量、判断怪物是否因此次攻击死亡）。
```csharp
HealthChange result = health.Damage(100);
if (result.HasChanged) { /* 结算逻辑 */ }
```

**使用事件 (`health.Changed += ...`)**：适合**解耦的观察者**。
如 UI、动画、音效等无需关心谁造成的伤害，只需响应状态的变化。

---
**下一步建议阅读**：
👉 [直接使用 Health Core](../guides/core-usage.md) 
或者：
👉 [使用 HealthComponent](../guides/unity-component.md)