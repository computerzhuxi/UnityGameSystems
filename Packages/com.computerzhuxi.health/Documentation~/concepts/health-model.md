# 生命模型 (Health Model)

Health System 将生命状态建模为两个核心整数：`Current`（当前生命值）与 `Maximum`（最大生命值）。
系统中的存活、死亡、生命比例等派生状态，均由这两个基础数值严格推导而来。

---

## 🧬 核心状态与约束

### 状态约束
一个合法的生命状态必须始终满足严格的数学边界：
```text
Maximum >= 1
0 <= Current <= Maximum
```
> ⚠️ **防御性设计**：Health System 不会自动静默修正非法的初始状态。如果你尝试传入非法值（如 `new Health(-1, 100)` 或 `new Health(120, 100)`），系统会立刻抛出 `ArgumentOutOfRangeException`，以尽早暴露上层调用方的数据错误。

### 存活与死亡 (Alive & Dead)
Health **不单独保存**一个表示死亡的布尔状态变量，对象是否存活完全由 `Current` 动态推导：
- `Current > 0` ➔ **IsAlive = true** (IsDead = false)
- `Current == 0` ➔ **IsAlive = false** (IsDead = true)

> 💡 **核心认知：死亡不是一种特殊的对象模式**。
> 死亡仅仅是 `Current == 0` 的一种结果。死亡后的 Health 实例仍然是一个合法对象，它并没有失效，依然可以读取快照、监听事件、修改 Maximum 或接受 Restore/Revive。

### 状态比例 (Normalized)
表示当前生命值占最大生命值的比例，计算方式为 `Current / Maximum`。
```csharp
float value = health.Normalized; // 例如 50/100 -> 0.5
```
因为 `Maximum` 永远至少为 1，合法状态下绝不会出现除以 0 的异常。该值非常适合用于**UI 血条**、**生命值百分比展示**或**基于健康度的表现驱动**。

---

## 📦 数据结构：快照与变化记录

### HealthSnapshot (不可变快照)
表示某一个时间点的完整生命状态快照，包含：`Current`、`Maximum`、`IsAlive`、`IsDead`、`Normalized`。
```csharp
HealthSnapshot snapshot = health.Snapshot;
```
由于它是**不可变（Immutable）**的，即使随后生命值发生了变化，早先获取的 Snapshot 依然保持获取那一刻的值。
**适用场景**：记录操作前后的状态、事件参数传递、日志记录、状态 Diff 比较。

### HealthChange (变化结果)
Health 的每个修改命令都会返回一个 `HealthChange`，详细描述一次操作前后的完整变化，包含：
- `Reason`: 变化的命令原因
- `Before` / `After`: 变化前后的 `HealthSnapshot`
- `HasChanged`: 是否产生实际变化
- `Delta`: 数学差值
- `ActualAmount`: 实际变化绝对量

> 调用方不需要自己在命令前后手动记录生命值，`HealthChange` 已经提供了一切上下文。

---

## 📐 架构设计与权限管理

### 状态和命令严格分离
系统将 **“现在的状态是什么”** 和 **“我要发生什么变化”** 分开处理：
- **状态表达**：`Current`, `Maximum`, `Snapshot` 是只读属性。
- **状态修改**：必须通过明确的命令（`Damage`, `Heal`, `Kill`, `Revive`, `ChangeMaximum`, `Restore`）。

外部**绝对不能**直接给属性赋值（如 `health.Current = 50;`）。这保证了所有的变化都经过统一规则验证、都能产生变化记录（HealthChange）且事件触发顺序绝对一致。

### Health 是状态的真正所有者
`Health` 实例负责保存基础数值、验证命令、提交状态变化、生成结果并发布事件。

### IReadOnlyHealth 是外部观察接口
为了明确**“谁拥有状态”**和**“谁只负责观察”**，系统提供了 `IReadOnlyHealth` 接口：
它只暴露生命事实（`Current` 等）和事件（`Changed` 等），**不暴露**任何修改命令（`Damage`, `Heal` 等）。
> **最佳实践**：例如 UI 的血条组件 (`HealthBar`)，只需要依赖 `IReadOnlyHealth` 即可，防止表现层越权修改核心生命数据。

### 一个实体应只有一个权威生命实例
对于同一个游戏实体，通常只应存在一个权威的生命实例：
* **推荐**：`Character` ➔ `HealthComponent` (内部包含 Health)
* **推荐**：`CharacterModel` ➔ 独立 `Health` Core
* **🚫 强烈不推荐**：同时挂载 `HealthComponent` 并创建独立的 `Health` 然后手动在两者间同步数据。这样极易导致状态撕裂。

---

## ⚙️ 核心语义与规则区分

### 1. Inspector 配置 ≠ 运行时状态
`HealthComponent` 面板中的 `Starting Health` 等配置，仅仅是**创建运行时生命状态的初始图纸**。
运行时的扣血/治疗不会反写修改 Prefab 或 Scene 的初始配置。

### 2. 初始化不会发布生命事件
无论是代码 `new Health()` 还是 `HealthComponent` 的初始化，仅仅是“状态的确立”。这些操作**不会**触发 `Changed`、`Healed` 或 `Revived`。事件只代表**已存在状态发生的变化**。

### 3. Heal 与 Revive 互不干扰
- **Heal (治疗)**：只作用于存活对象。不能让死亡目标复活。
- **Revive (复活)**：显式让死亡对象重新存活。普通治疗不会意外承担复活语义。

### 4. Kill 与 Damage 概念迥异
- **Damage (伤害致死)**：这是一次真实的受击伤害，且伤害过大导致了死亡。
- **Kill (强制死亡)**：直接强行终止生命状态，不是伤害行为（不触发 `Damaged` 事件）。

### 5. Restore 独立于 Gameplay 命令
`Restore(current, max)` 用于原子化地恢复到一个指定的权威状态（适合读档、网络同步）。
它**不会**把生死状态的切换解释为实际的 Gameplay 事件（即：不会触发 `Died` 或 `Revived`），避免读档时误触击杀奖励或复活动画。

---

## ⚡ 执行模型 (Execution Model)

- **状态变化是原子的**：一次命令会一次性提交新的 `Current` 和 `Maximum`，然后发布事件。监听者看到的始终是无中间态的完整新状态。
- **完全同步模型**：Health 不是异步系统。当一行修改代码（如 `health.Damage(20);`）返回时，状态更新、数据记录生成、同步事件分发已经**全部执行完毕**。它被设计为面向单线程的同步操作容器。

---

## 🗺️ 架构关系总结

```text
Health
 ├── 核心状态: Current, Maximum
 ├── 派生状态: IsAlive / IsDead, Normalized, Snapshot
 │
 ├── 提交命令 (Commands): Damage, Heal, Kill, Revive, ChangeMaximum, Restore
 │   └── 生成结果: HealthChange
 │
 └── 观察事件 (Events): Changed, Damaged, Healed, Died, Revived
```

👉 **下一步建议阅读**：[命令与事件 (Commands and Events)](commands-and-events.md)