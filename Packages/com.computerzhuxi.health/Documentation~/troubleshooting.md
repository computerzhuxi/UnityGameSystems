# 故障排查 (Troubleshooting)

本页整理了 Health System 中最常见的行为疑问和接入问题。为了方便查阅，问题已按模块分类。

---

## 💊 伤害与治疗 (Damage & Heal)

### ❓ Heal 为什么不能复活死亡对象？
这是预期行为。`Heal` 只作用于仍然存活的对象。
例如：`0 / 100` ➔ `Heal(50)` ➔ `0 / 100`。
死亡对象不会因为普通治疗重新存活。如果需要复活，应显式调用 `health.Revive(50)`。这样可以明确区分**治疗**和**复活**。

### ❓ 为什么 Damage / Heal 没有生效？
先检查以下情况：
- `amount` 小于等于 0：非正数操作（如 `Damage(0)` 或 `Damage(-10)`）不会产生变化。
- **对象已经死亡**：死亡对象不会继续受到普通 `Damage`。
- **满血状态**：当前生命已等于 Maximum 时，`Heal` 不会生效。

可以通过返回值检查：如果 `change.HasChanged == false`，说明这次命令没有产生实际状态变化。

### ❓ 为什么实际伤害和传入值不一样？
`Damage` 的输入值不一定等于实际生命减少量。
例如，`Current = 10`，执行 `Damage(100)`，最终生命只会减少 10。
返回值中的 `ActualAmount` 表示实际发生的 `Current` 变化量（在这个例子中 `ActualAmount = 10`）。

### ❓ 为什么没有触发 Damaged / Healed？
`Damaged` 和 `Healed` 只在对应命令**产生实际变化时**发布。
例如满血时调用 `Heal(20)`，或对死亡对象调用 `Damage(20)`，生命值没有发生实质变化，因此不会触发对应事件。

---

## 💀 死亡与复活 (Kill & Revive)

### ❓ 为什么 Kill 没有触发 Damaged？
因为 `Kill()` 不是伤害命令，它表示**直接令对象死亡**。
事件顺序是 `Changed` ➔ `Died`。如果需要表达“这次死亡是由伤害造成的”，应使用致死的 `Damage()`。

### ❓ 为什么重复 Kill 没有再次触发 Died？
如果对象已经死亡（`0 / 100`），再次调用 `health.Kill()`，最终状态没有发生变化（`HasChanged = false`），Health System 只为真实状态变化发布事件，因此不会重复发布 `Died`。

### ❓ 为什么 Revive 没有修改当前生命值？
`Revive` **只作用于死亡对象**。如果对象当前仍然存活（例如 `30 / 100`），调用合法的 `Revive(80)`，结果仍然是 `30 / 100`。修改存活对象请使用其他命令。

### ❓ 为什么 Revive 抛出 ArgumentOutOfRangeException？
`Revive(value)` 要求传入的值必须合法：`1 <= value <= Maximum`。这是为了防止产生非法生命状态。

---

## ⚙️ 上限修改与状态恢复 (Maximum & Restore)

### ❓ 为什么 ChangeMaximum(..., Refill) 会复活对象？
因为 `Refill` 策略的语义是：`Maximum = 新值` 且 `Current = 新值`。
例如 `0 / 100` ➔ `ChangeMaximum(200, Refill)` ➔ `200 / 200`。
这发生了 `Dead ➔ Alive`，因此会触发 `Revived` 事件。如果只希望修改上限而保持死亡状态，应使用 `MaximumHealthPolicy.PreserveCurrent`。

### ❓ 为什么降低 Maximum 后 Current 也变小了？
这是 `PreserveCurrent` 策略的边界行为。为了保证合法的 `Current <= Maximum`，如果新的最大生命值低于当前生命，Current 会被自动截断。

### ❓ 为什么 Restore 没有触发 Died 或 Revived？
这是 `Restore` 的核心设计语义。`Restore` 表示**直接恢复到一个权威状态**（常用于读档、网络同步）。它不是游戏中发生的一次真正生死事件，因此即使跨越了生死状态，**有实际变化时也只发布 `Changed`**，不会发布 `Died` 或 `Revived`。

### ❓ 为什么 Restore 没有任何事件？
如果恢复前后的状态完全相同，则 `HasChanged = false`，此时不会发布任何事件。

### ❓ 为什么创建 Health 或 Restore 时传入非法值直接报错，而不是自动 Clamp？
因为构造函数和 `Restore` 表示调用方提供**完整的权威数据**。如果状态本身非法（如 `new Health(120, 100)`），应当报错尽早暴露问题；而 `Damage` 和 `Heal` 是 Gameplay 行为，截断（Clamp）是自然的玩法语义。

---

## 📡 事件机制 (Events)

### ❓ 为什么 Changed 触发了，但 ActualAmount 是 0？
因为 `Changed` 不只代表 `Current` 变化。如果只修改了最大生命值（例如上限 100 改为 200，但生命值还是 50），此时 `HasChanged = true`，但 `Current` 没变，因此实际变化量 `ActualAmount = 0`。

### ❓ 为什么事件回调里调用 Damage / Heal 会报错？
抛出 `InvalidOperationException` 是因为 Health System 有**同步重入保护**。不允许在一个命令正在发布事件的途中，回调内部又同步提交第二条命令，否则会导致事件顺序不可预测。

### ❓ Died 事件里不能直接 Revive 吗？
**不能同步执行**。如果需要自动复活，应由上层系统把复活指令安排到当前事件调用栈结束之后（如在 Unity 中放入后续帧或其他调度流程中执行）。Health Core 本身不负责调度。

### ❓ 为什么事件回调里读取到的是新生命值？
因为 Health 的执行顺序是：`计算新状态` ➔ `提交新状态` ➔ `发布事件`。
在事件回调中读取 `health.Current` 会得到新状态，如果需要对比，可以通过事件参数 `change.Before` 获取旧状态。

---

## 🧩 Unity 组件层 (HealthComponent)

### ❓ 为什么 HealthComponent 禁用再启用后没有重置生命值？
`HealthComponent` 不会在每次 `OnEnable` 时重新创建内部 Health，运行时生命状态会保持。如果希望重新启用时重置，应明确调用 `health.Restore()`，或对死亡对象调用 `health.Revive()`。

### ❓ 为什么修改生命值不会改变 Starting Health？
`Starting Health` 是 Inspector **初始配置图纸**，不代表运行时的 `Current`。HealthComponent 绝不会因为运行时受到伤害就把变化写回 Prefab 或 Scene 初始值中。

### ❓ 为什么 Start With Full Health 打开时，非法 Starting Health 仍然报错？
所有序列化配置本身都必须合法（`0 <= Starting Health <= Maximum`）。即使启用了满血启动，填入非法的 `Starting Health` 依然会导致初始化报错，这是为了防止场景中长期遗留无效的配置数据。

### ❓ Initialize 失败/重复调用问题
*   **失败后重试**：如果配置非法导致初始化异常（`IsInitialized = false`），修正后再次调用 `Initialize()` 可正常完成。
*   **重复调用**：`Initialize()` 是幂等的。Health 创建后，再次调用**不会**重置生命值，也**不会**重复绑定事件。

### ❓ UnityEvent 和强类型事件怎么选？
*   **UnityEvent** (无参数)：适合在 Inspector 里直接拖拽表现逻辑方法（如 `OnDied` ➔ `PlayDeathAnimation()`）。
*   **强类型事件**：如果需要读取变化量、进行逻辑判断（如检查 `ActualAmount`、`Before / After`），请监听 `IReadOnlyHealth` 上的强类型事件（如 `component.State.Changed`）。

### ❓ HealthComponent 销毁后为什么 UnityEvent 不再触发？
组件销毁时会**解除自己建立的 Core 事件转发关系**。因此，即使你的代码里仍然保留了底层 Core 实例，也不会再通过这个已销毁的 Component 触发 UnityEvent。

### ❓ 可以同时使用 Health 和 HealthComponent 吗？
可以在同一个项目中使用，但对于同一个游戏实体，**不推荐**同时挂载 `HealthComponent` 并创建独立的 `Health` 然后在两者间手动同步。请选择其中一个作为唯一权威状态。

---

👉 **找不到答案？**
如果不确定某个 API 应该怎么接入，建议通过 Package Manager 导入 **Basic Health Demo** 运行参考。