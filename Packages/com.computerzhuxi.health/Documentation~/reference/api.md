# API 参考

**命名空间**：`Computerzhuxi.Health`

本页记录 Health System 的公开类型与行为契约。

---

## 📋 枚举类型

### MaximumHealthPolicy
用于指定修改最大生命值时如何处理当前生命值。
```csharp
public enum MaximumHealthPolicy
{
    PreserveCurrent,
    Refill
}
```

*   **`PreserveCurrent`**: 尽量保留当前生命值。结果等价于：`Maximum = value`, `Current = Min(Current, value)`。
    *   *示例 1（提升上限）*：`80 / 100` ➔ `ChangeMaximum(200, PreserveCurrent)` ➔ `80 / 200`
    *   *示例 2（降低上限）*：`80 / 100` ➔ `ChangeMaximum(50, PreserveCurrent)` ➔ `50 / 50`
    *   *示例 3（死亡对象）*：`0 / 100` ➔ `ChangeMaximum(200, PreserveCurrent)` ➔ `0 / 200` (仍然保持死亡)

*   **`Refill`**: 使用新的最大生命值并立即回满。结果等价于：`Maximum = value`, `Current = value`。
    *   *示例 1（回满）*：`30 / 100` ➔ `ChangeMaximum(200, Refill)` ➔ `200 / 200`
    *   *示例 2（复活）*：如果对象原本死亡，`Refill` 会使其重新存活，并可能触发 `Revived`。

### HealthChangeReason
表示产生一次 `HealthChange` 的命令类型。
```csharp
public enum HealthChangeReason
{
    Damage,
    Heal,
    Kill,
    Revive,
    Maximum,
    Restore
}
```

| 枚举值 | 对应触发命令 |
| :--- | :--- |
| `Damage` | `Damage()` |
| `Heal` | `Heal()` |
| `Kill` | `Kill()` |
| `Revive` | `Revive()` |
| `Maximum` | `ChangeMaximum()` |
| `Restore` | `Restore()` |

---

## 📦 结构体与接口

### HealthSnapshot (只读结构体)
表示某一时刻不可变的完整生命状态。
```csharp
public readonly struct HealthSnapshot
```

| 属性 | 类型 | 说明 | 约束 / 计算方式 |
| :--- | :--- | :--- | :--- |
| **Current** | `int` | 当前生命值。 | `0 <= Current <= Maximum` |
| **Maximum** | `int` | 最大生命值。 | `Maximum >= 1` |
| **IsAlive** | `bool` | 是否存活。 | `Current > 0` |
| **IsDead** | `bool` | 是否死亡。 | `Current == 0` |
| **Normalized** | `float` | 当前生命比例。 | `(float)Current / Maximum` (例如 50/100 = 0.5) |

### HealthChange (只读结构体)
表示一次生命命令执行前后的不可变结果。
```csharp
public readonly struct HealthChange
```

*   **`Reason`** (`HealthChangeReason`): 表示产生此次变化的命令。
*   **`Before`** (`HealthSnapshot`): 命令执行前的生命状态。
*   **`After`** (`HealthSnapshot`): 命令执行后的生命状态。
*   **`HasChanged`** (`bool`): 当 `Current` 或 `Maximum` 至少有一个发生变化时为 `true`。如果状态没有变化（`HasChanged = false`），则**不会发布任何生命事件**。
*   **`Delta`** (`int`): 计算方式：`After.Current - Before.Current`。（例如：100 ➔ 80，Delta = -20）。如果只修改了 Maximum 而 Current 不变，则 `Delta = 0`。
*   **`ActualAmount`** (`int`): 计算方式：`Abs(Delta)`。表示此次命令**实际造成的当前生命值变化绝对量**。注意：如果只修改了 Maximum，则 `ActualAmount = 0`。

### IReadOnlyHealth
提供生命状态的只读访问和事件。不包含任何生命修改命令。
```csharp
public interface IReadOnlyHealth
{
    int Current { get; }
    int Maximum { get; }
    bool IsAlive { get; }
    bool IsDead { get; }
    float Normalized { get; }
    HealthSnapshot Snapshot { get; }

    event Action<HealthChange> Changed; // 任何实际生命状态变化都会发布（Current 或 Maximum）
    event Action<HealthChange> Damaged; // 只有 Damage() 导致实际状态变化时发布
    event Action<HealthChange> Healed;  // 只有 Heal() 导致实际状态变化时发布
    event Action<HealthChange> Died;    // 当非 Restore 命令导致 Alive → Dead 时发布
    event Action<HealthChange> Revived; // 当非 Restore 命令导致 Dead → Alive 时发布
}
```

---

## ⚙️ 核心类: Health

独立拥有生命状态的纯 C# Core 类型。使用同步事件模型，并设计用于单线程同步调用。

```csharp
public sealed class Health : IReadOnlyHealth
```

### 构造函数
```csharp
public Health(int current, int maximum)
```
*   **参数要求**: `maximum >= 1`, `0 <= current <= maximum`。
*   **异常**: 非法参数会抛出 `ArgumentOutOfRangeException`。
*   **事件**: 构造过程**不会**发布任何生命事件。

### 公共属性
实现自 `IReadOnlyHealth` 接口：`Current`, `Maximum`, `IsAlive`, `IsDead`, `Normalized`, `Snapshot`。

### 核心方法 (Commands)

#### `Damage`
```csharp
public HealthChange Damage(int amount)
```
*   **行为**: 只有满足 `amount > 0` 且 `IsAlive == true` 时才可能发生变化。伤害不会使 Current 低于 0。
*   **无变化**: `amount <= 0` 或目标已死亡时，`HasChanged = false` 且不发布事件。
*   **事件顺序**: 普通伤害 (`Damaged` ➔ `Changed`)；致死伤害 (`Damaged` ➔ `Changed` ➔ `Died`)。

#### `Heal`
```csharp
public HealthChange Heal(int amount)
```
*   **行为**: 只有满足 `amount > 0` 且 `IsAlive == true` 时才可能发生变化。新的 Current 最大不会超过 Maximum。
*   **规则**: 死亡对象不能通过 `Heal` 复活。
*   **事件顺序**: `Healed` ➔ `Changed`。

#### `Kill`
```csharp
public HealthChange Kill()
```
*   **行为**: 直接将 Current 设置为 0。如果对象已死亡，`HasChanged = false`，不重复发布事件。
*   **事件顺序**: `Changed` ➔ `Died`。*(注意：不会发布 `Damaged`)*。

#### `Revive`
```csharp
public HealthChange Revive(int value)
```
*   **参数要求**: `1 <= value <= Maximum`，否则抛出 `ArgumentOutOfRangeException`。
*   **行为**: 对存活对象调用，`HasChanged = false`。只对死亡对象有效。
*   **事件顺序**: `Changed` ➔ `Revived`。

#### `ChangeMaximum`
```csharp
public HealthChange ChangeMaximum(int value, MaximumHealthPolicy policy)
```
*   **参数要求**: `value >= 1`，`policy` 必须有效，否则抛出 `ArgumentOutOfRangeException`。
*   **事件顺序**: 通常为 `Changed`；如果 `Refill` 导致复活，则为 `Changed` ➔ `Revived`。

#### `Restore`
```csharp
public HealthChange Restore(int value, int maximum)
```
*   **行为**: 原子恢复完整生命状态（适合读档/网络同步）。非法参数抛出 `ArgumentOutOfRangeException`。
*   **事件规则**: 有实际变化时**只发布 `Changed`**。跨越生死状态时**绝不会**发布 `Died` 或 `Revived`。无变化时不发布事件。

### 事件系统与重入限制
完整事件触发顺序速查：

| 操作 | 事件触发顺序 |
| :--- | :--- |
| **普通 Damage** | `Damaged` ➔ `Changed` |
| **致死 Damage** | `Damaged` ➔ `Changed` ➔ `Died` |
| **Heal** | `Healed` ➔ `Changed` |
| **Kill** | `Changed` ➔ `Died` |
| **Revive** | `Changed` ➔ `Revived` |
| **ChangeMaximum** | `Changed` |
| **ChangeMaximum (Refill 导致复活)** | `Changed` ➔ `Revived` |
| **Restore** | `Changed` (无附加事件) |
| *(无实际变化的操作)* | *(不发布事件)* |

> ⚠️ **同步重入限制**：Health 会先提交新状态，再发布事件。**不允许在自己的事件发布过程中同步提交新的生命命令**。例如在 `Changed` 回调中调用 `Heal()` 会抛出 `InvalidOperationException`。

---

## 🎮 Unity 组件层: HealthComponent

Unity GameObject 接入层，内部持有一个 `Health` 实例。

```csharp
public sealed class HealthComponent : MonoBehaviour
```

### Inspector 配置
*   **Maximum**: 必须 `>= 1`。
*   **Start With Full Health**: `bool`。
*   **Starting Health**: 必须 `0 <= Starting Health <= Maximum`（即使启用了满血启动也必须合法）。

### 属性与事件转发

| 属性 / 事件 | 描述 |
| :--- | :--- |
| `bool IsInitialized { get; }` | 内部 Health 是否已经创建。 |
| `IReadOnlyHealth State { get; }` | 获取内部生命状态的只读接口。访问时会确保组件已初始化。 |
| `UnityEvent OnChanged` | 内部 `Health.Changed` 的无参事件转发。 |
| `UnityEvent OnDamaged` | 内部 `Health.Damaged` 的无参事件转发。 |
| `UnityEvent OnHealed` | 内部 `Health.Healed` 的无参事件转发。 |
| `UnityEvent OnDied` | 内部 `Health.Died` 的无参事件转发。 |
| `UnityEvent OnRevived` | 内部 `Health.Revived` 的无参事件转发。 |

### 方法

#### `Initialize()`
```csharp
public void Initialize()
```
*   初始化内部 Health。
*   如果 `Start With Full Health = true`，`Current = Maximum`；否则 `Current = Starting Health`。
*   **幂等性**: 如果 Health 已存在，重复调用不会重建、不重置生命、不重复绑定事件。
*   非法配置会抛出 `ArgumentOutOfRangeException`。

#### 核心命令转发
组件封装了直接转发到内部 `Health` 实例的所有核心命令方法，并返回 `HealthChange`：
`Damage(int)`, `Heal(int)`, `Kill()`, `Revive(int)`, `ChangeMaximum(int, policy)`, `Restore(int, int)`。

### 生命周期说明
*   **Awake**: 组件在 `Awake` 中调用 `Initialize()`。
*   **Disable / Enable**: 禁用/启用 GameObject **不会**重新创建内部 Health，运行时状态保持。
*   **OnDestroy**: 组件销毁时解除自身建立的 Core 事件转发。此时外部即使持有原 Core 实例，也不会触发该组件的 UnityEvent。

---

## 🚨 异常汇总速查

### `ArgumentOutOfRangeException`
*   `Health` 构造时：`maximum < 1`, `current < 0`, `current > maximum`
*   `Revive`：`value < 1`, `value > Maximum`
*   `ChangeMaximum`：`value < 1` 或非法 policy
*   `Restore`：`maximum < 1`, `value < 0`, `value > maximum`
*   `HealthComponent`：Inspector 序列化配置非法时调用 `Initialize()`

### `InvalidOperationException`
*   当 `Health` 正在发布事件时，对同一个实例同步提交新的生命命令。