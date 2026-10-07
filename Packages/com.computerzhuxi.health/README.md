# ❤️ Health

一个轻量、独立且可复用的 Unity 生命系统。

支持生命值与最大生命值管理、伤害与治疗、死亡与复活，以及状态恢复。**Health** 只负责管理生命状态，不负责伤害计算、防御抗性、UI、动画或具体的存档格式。

## 📦 安装

要求 **Unity 6000.3** 或更高版本。

通过 Unity Package Manager 使用 Git URL 添加：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1
```

## 🚀 快速开始

同一个角色应该只有一个权威的生命状态。

根据项目结构选择一种使用方式：

| 方式 | 适用场景 |
| --- | --- |
| **HealthComponent** | 希望通过 Inspector 配置和绑定事件的普通 Unity 对象 |
| **Health** | 已有自己的角色或状态架构，希望自行管理生命周期 |

### 使用 HealthComponent

在目标 GameObject 上添加 `HealthComponent`。首次尝试时，将 `Maximum` 和 `Starting Health` 都设为 `100`，保持 `Start With Full Health` 勾选。

将下面代码保存为 `Player.cs`，把 `Player` 挂到同一个 GameObject，再将该对象上的 `HealthComponent` 拖入 Player 的 `Health` 引用栏。

```csharp
using Computerzhuxi.Health;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private HealthComponent health;

    /// <summary>将最终伤害交给生命组件处理。</summary>
    public void TakeDamage(int damage)
    {
        health.Damage(damage);
    }

    /// <summary>将治疗量交给生命组件处理。</summary>
    public void Heal(int amount)
    {
        health.Heal(amount);
    }

    /// <summary>在 Play 模式的组件菜单中触发一次伤害，并输出结果。</summary>
    [ContextMenu("Demo/Take Damage 20")]
    private void DemoTakeDamage()
    {
        // 编辑模式不执行生命命令，避免把演示操作当作配置修改。
        if (!Application.isPlaying) return;
        TakeDamage(20);
        Debug.Log($"HP: {health.State.Current} / {health.State.Maximum}");
    }
}
```

进入 Play 模式，在 Inspector 中右键点击 `Player` 组件标题，选择 **Demo > Take Damage 20**。首次执行后，Console 应显示 `HP: 80 / 100`。

读取当前状态：

```csharp
int current = health.State.Current;
int maximum = health.State.Maximum;

bool alive = health.State.IsAlive;
bool dead = health.State.IsDead;

float normalized = health.State.Normalized;
```

### 直接使用 Health

如果项目已经有自己的角色或状态架构，可以直接创建 `Health`。

这种方式不依赖 `MonoBehaviour`，由创建者负责保存实例并管理其生命周期。

```csharp
using Computerzhuxi.Health;

public class CharacterHealth
{
    private readonly Health health =
        new Health(current: 100, maximum: 100);

    public IReadOnlyHealth State => health;

    /// <summary>提交角色受到的最终伤害。</summary>
    public void TakeDamage(int amount)
    {
        health.Damage(amount);
    }

    /// <summary>恢复存活角色的生命。</summary>
    public void Heal(int amount)
    {
        health.Heal(amount);
    }
}
```

## 🎮 常用操作

### 伤害与治疗

```csharp
health.Damage(20);
health.Heal(20);
```

- `Damage` 最低将生命降至 `0`，死亡后不会继续受到伤害。
- `Heal` 不会超过最大生命值，也不能复活死亡对象。

### 死亡与复活

```csharp
health.Kill();
health.Revive(50);
```

`Revive` 的生命值必须大于 `0`，且不能超过当前最大生命值。

### 修改最大生命值

保持当前生命值；如果当前值超过新的上限，会截断到新上限：

```csharp
health.ChangeMaximum(
    150,
    MaximumHealthPolicy.PreserveCurrent);
```

修改上限并回满：

```csharp
health.ChangeMaximum(
    150,
    MaximumHealthPolicy.Refill);
```

`Refill` 会将当前生命设置为新的最大生命，因此也可以使死亡对象恢复。

### 恢复状态

适合读档或状态同步：

```csharp
health.Restore(75, 120);
```

`Restore` 的 `maximum` 必须大于或等于 `1`，`current` 必须处于 `0` 到 `maximum` 之间；非法值会抛出 `ArgumentOutOfRangeException`。它只恢复生命状态，不会重新触发 `Died` 或 `Revived` 事件。

所有生命命令都返回 `HealthChange`，可用结果的 `HasChanged` 判断状态是否实际变化；状态无变化时不派发事件。

## 🔔 监听事件

可以通过 `State` 读取生命状态并监听相关事件。将下面代码保存为 `HealthObserver.cs`，挂到有 `HealthComponent` 的 GameObject 上，并拖入 `Health` 引用：

```csharp
using Computerzhuxi.Health;
using UnityEngine;

public class HealthObserver : MonoBehaviour
{
    [SerializeField] private HealthComponent health;
    private IReadOnlyHealth observed;

    /// <summary>启用观察者时取得组件状态，并开始监听生命变化。</summary>
    private void OnEnable()
    {
        // State 会按需初始化，不依赖两个组件的 Awake 执行顺序。
        observed = health.State;
        observed.Changed += OnChanged;
    }

    /// <summary>禁用或销毁观察者时解除订阅，避免重复监听。</summary>
    private void OnDisable()
    {
        if (observed == null) return;
        observed.Changed -= OnChanged;
        observed = null;
    }

    /// <summary>显示生命变化前后的当前值。</summary>
    private void OnChanged(HealthChange change)
    {
        Debug.Log($"HP: {change.Before.Current} -> {change.After.Current}");
    }
}
```

继续执行上面的伤害操作，可看到变化日志。观察者禁用期间不接收通知，重新启用后继续监听；直接使用 `Health` 时，在该实例上订阅事件，并由持有者在观察结束时解除订阅。其他事件包括 `Damaged`、`Healed`、`Died` 和 `Revived`，回调参数同样为 `HealthChange`。

`HealthChange` 提供本次变化的信息：

```csharp
change.Reason       // 变化原因
change.Before       // 变化前状态
change.After        // 变化后状态
change.Delta        // 当前生命值的实际变化量
change.ActualAmount // 实际变化量的绝对值
```

使用 `HealthComponent` 时，也可以直接在 Inspector 中绑定：

- `On Changed`
- `On Damaged`
- `On Healed`
- `On Died`
- `On Revived`

Inspector 中的 UnityEvent 不包含变化参数。如果需要具体数值或变化原因，请监听 `State` 上的事件。

## ⚠️ 使用规则

1. **唯一状态**：同一个角色只保留一个权威 `Health` 实例，避免生命状态分叉。
2. **复活**：`Heal` 不能复活死亡对象，需要显式调用 `Revive`。
3. **状态恢复**：`Restore` 用于恢复完整状态，不会重新触发死亡或复活事件。
4. **组件禁用**：禁用 `HealthComponent` 不会重置其生命状态。
5. **事件重入**：不要在 Health 事件回调中同步调用 `Damage`、`Heal`、`Kill`、`Revive` 等修改状态的命令，否则会抛出 `InvalidOperationException`。

## 🎮 示例

可以通过 Package Manager 导入 **Basic Health Demo**，查看 `HealthComponent` 的基本使用方式。

## 📝 更新记录

版本变化请查看 [CHANGELOG](./CHANGELOG.md)。
