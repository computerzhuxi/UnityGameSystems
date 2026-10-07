# 使用 HealthComponent

`HealthComponent` 是 Health System 的 Unity 接入层。

它适合需要以下能力的场景：
- 直接挂载到 GameObject
- 通过 Inspector 配置初始生命值
- 使用 UnityEvent
- 让生命状态跟随场景对象生命周期

> **说明：** `HealthComponent` 内部仍然使用同一套 Health Core 规则。

---

## ⚙️ 初始化配置

给 GameObject 添加 `HealthComponent` 组件后，组件提供以下初始化配置：
- **Maximum**
- **Start With Full Health**
- **Starting Health**

### Maximum
表示最大生命值。必须满足：`Maximum >= 1`
例如：`Maximum = 100`

### Start With Full Health
如果启用 `Start With Full Health = true`，初始化时：`Current = Maximum`
例如：
- `Maximum = 100`
- `Starting Health = 30`
- `Start With Full Health = true`
**实际运行时初始状态为：`100 / 100`**

### Starting Health
如果关闭满血启动 `Start With Full Health = false`，则初始化时：`Current = Starting Health`
例如：
- `Maximum = 100`
- `Starting Health = 30`
- `Start With Full Health = false`
**初始化状态：`30 / 100`**

> ⚠️ **警告：** `Starting Health` 必须满足 `0 <= Starting Health <= Maximum`。即使启用了满血启动，这个配置本身仍然必须合法。

---

## 🔄 初始化时机

`HealthComponent` 会在 `Awake` 中完成初始化。也可以手动调用：
```csharp
component.Initialize();
```

### Initialize 是幂等的
重复调用 `Initialize()` 不会创建多个 Health，也不会重置当前生命状态。
例如：
`10 / 10` ➔ `Damage(3)` ➔ `7 / 10` ➔ `Initialize()` ➔ 结果仍然是：`7 / 10`

### 检查是否已经初始化
可以读取 `component.IsInitialized`：
```csharp
if (component.IsInitialized)
{
    Debug.Log("Health 已初始化");
}
```
正常 Unity 生命周期下，组件在 `Awake` 后已经初始化。

---

## 📊 读取状态

通过 `component.State` 获取只读生命状态。`State` 类型为 `IReadOnlyHealth`。
```csharp
Debug.Log(component.State.Current);
Debug.Log(component.State.Maximum);
Debug.Log(component.State.IsAlive);
Debug.Log(component.State.Normalized);
```
> **注意：** 访问 `State` 时，如果组件还未初始化，会先自动调用 `Initialize()`。

### 为什么 State 是 IReadOnlyHealth
`State` 只负责暴露**生命事实**与**生命事件**，而不暴露写命令。
因此，`component.State.Damage(10);` 这样的调用不存在。修改生命状态应该通过 `component.Damage(10);` 或者其他组件命令完成。

---

## ⚔️ 核心命令

### 提交伤害 (Damage)
```csharp
HealthChange change = component.Damage(20);
```
例如：`100 / 100` ➔ `Damage(20)` ➔ `80 / 100`。
返回值仍然是标准的 `HealthChange`，可以读取 `change.HasChanged`, `change.ActualAmount`, `change.Before`, `change.After` 等信息。

### 治疗 (Heal)
```csharp
component.Heal(20);
```
例如：`50 / 100` ➔ `Heal(20)` ➔ `70 / 100`。
如果已经满血，再次 Heal 不会产生变化。死亡对象也不能通过 Heal 复活。

### Kill
```csharp
component.Kill();
```
直接令当前生命值变为 0。Kill 不属于伤害，因此**不会触发 OnDamaged**。

### Revive
用于显式复活死亡对象。
```csharp
component.Revive(50);
```
例如：`0 / 100` ➔ `Revive(50)` ➔ `50 / 100`。

### 修改最大生命值 (ChangeMaximum)
```csharp
component.ChangeMaximum(150, MaximumHealthPolicy.PreserveCurrent);
// 或者
component.ChangeMaximum(150, MaximumHealthPolicy.Refill);
```
- `PreserveCurrent` 尽量保留当前生命。
- `Refill` 会直接回满，并可能让死亡对象重新存活。

### Restore
直接恢复完整生命状态：
```csharp
component.Restore(50, 120);
```
`Restore` 适合：读档、Checkpoint、网络状态同步、权威状态恢复。
`Restore` 有实际状态变化时**只会触发 `OnChanged`**；如果恢复前后状态完全相同，则不会触发任何 UnityEvent。跨越生死状态也不会额外触发 `OnDied` 或 `OnRevived`。

---

## 📡 事件系统

### 1. UnityEvent
`HealthComponent` 提供以下 UnityEvent，可直接通过 Inspector 绑定：
- `OnChanged`
- `OnDamaged`
- `OnHealed`
- `OnDied`
- `OnRevived`

在代码中监听：
```csharp
private void OnEnable()
{
    health.OnDied.AddListener(OnDied);
}

private void OnDisable()
{
    health.OnDied.RemoveListener(OnDied);
}

private void OnDied()
{
    Debug.Log("Dead");
}
```
> ⚠️ **注意：** `HealthComponent` 的 UnityEvent 是**无参数事件**。例如 `OnChanged` 只表示状态发生变化，不提供 Before、After 等详情。

### 2. 监听强类型事件
如果需要获取变化原因、Before / After、实际变化量等精确信息，应监听 Core 的强类型事件：
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

### UnityEvent 和强类型事件怎么选
- **UnityEvent**：如果只是需要在 Inspector 里拖一个方法（如 `OnDied` ➔ `PlayDeathAnimation()`），使用 UnityEvent 很方便。
- **强类型事件**：如果需要精确逻辑判断、获取变化详情，则应使用 `IReadOnlyHealth` 上的强类型事件。

---

## 🛠️ 生命周期与行为准则

### Disable / Enable 不会重置生命值
`HealthComponent` 不会在 `OnEnable` 时重新创建 `Health`。
例如：`10 / 10` ➔ `Damage(3)` ➔ `7 / 10` ➔ Disable ➔ Enable ➔ 最终仍然是：`7 / 10`。这是预期行为。

如果需要重新开始，应该显式使用 `component.Restore(...)`，或者对死亡对象调用 `component.Revive(...)`。

### Inspector 初始值不会被运行时状态写回
`Starting Health` 描述的是：**下次创建运行时 Health 时，从哪里开始**，而不是此刻当前生命值是多少。
这样运行时受到伤害不会意外修改 Prefab、Scene 初值或下一次实例化的默认值。

### 组件被销毁时
`HealthComponent` 在销毁时会解除它自己建立的 Core 事件转发关系。
组件销毁后，不会继续转发 UnityEvent。外部持有的 `State` (`IReadOnlyHealth`) 是只读观察入口，不要依赖已经销毁组件的 UnityEvent 生命周期。

---

## 🎮 架构与示例

### 基础角色示例

```csharp
using Computerzhuxi.Health;
using UnityEngine;

public sealed class CharacterHealth : MonoBehaviour
{
    [SerializeField]
    private HealthComponent health;

    private void OnEnable()
    {
        health.State.Changed += OnChanged;
        health.State.Died += OnDied;
        health.State.Revived += OnRevived;
    }

    private void OnDisable()
    {
        health.State.Changed -= OnChanged;
        health.State.Died -= OnDied;
        health.State.Revived -= OnRevived;
    }

    public void TakeDamage(int amount)
    {
        HealthChange result = health.Damage(amount);

        if (result.HasChanged)
        {
            Debug.Log($"实际伤害: {result.ActualAmount}");
        }
    }

    public void Heal(int amount) => health.Heal(amount);
    public void Kill() => health.Kill();
    public void Revive() => health.Revive(health.State.Maximum);

    private void OnChanged(HealthChange change)
    {
        Debug.Log($"Health: {change.After.Current}/{change.After.Maximum}");
    }

    private void OnDied(HealthChange change) => Debug.Log("Character died.");
    private void OnRevived(HealthChange change) => Debug.Log("Character revived.");
}
```

### 用 HealthComponent 做什么
`HealthComponent` 适合负责：
**Unity 对象** ➔ **生命 Core 的创建** ➔ **Inspector 初始配置** ➔ **UnityEvent 转发**

它**不应该**负责计算攻击力、防御公式、死亡动画逻辑或 UI 实现，这些系统应该消费 Health 的状态和事件。

### 不要同时维护两份 Health
- 不推荐：同时维护 `HealthComponent` 和独立的 `Health` 并尝试同步它们。
- 推荐：直接使用 `HealthComponent`（包含内部 Health）。
- 推荐（无组件架构）：完全不使用 `HealthComponent`，直接在 `CharacterModel` 中持有 `Health`。

---

## 🚀 接下来

👉 **[导入 Basic Health Demo](../getting-started.md)**：从 Unity Package Manager 导入，体验完整的实机演示。  
👉 **[API 参考](../reference/api.md)**：查询具体的公开接口规范。  
👉 **[故障排查](../troubleshooting.md)**：遇到行为不符合预期时查阅解决方案。