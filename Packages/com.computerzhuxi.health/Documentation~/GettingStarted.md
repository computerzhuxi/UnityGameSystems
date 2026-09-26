# Health 上手指南

> [文档导航](Index.md) · [API 精确契约](API.md)

本页以本地 **Unreleased** 工作树为准。组件销毁时的事件解绑、Demo 的 `Restore` 按钮与独立 Core 演示尚未进入固定 `health-v1.0.1` 标签；安装该标签的项目不能按下文的新按钮步骤操作。

## 先选择入口

在简单场景中，给 GameObject 添加 `HealthComponent`，让 Inspector 配置初值与无参数 UnityEvent。已有角色/存档架构可以自行 `new Health`，把 `IReadOnlyHealth` 交给观察者。两个入口使用同一个 Core 实现，但示例中的组件与独立 Core 是两个分别拥有的演示对象；同一角色不要同时创建两个权威生命实例。

## 组件式装配

1. 添加 `HealthComponent`。
2. 设置 `Maximum`（至少 1）、`Start With Full Health` 和 `Starting Health`（0 到 `Maximum`）。即使满血启动，所有序列化初值也必须合法。
3. 按需要在 Inspector 绑定 `On Changed`、`On Damaged`、`On Healed`、`On Died`、`On Revived`。

```csharp
using Computerzhuxi.Health;
using UnityEngine;

public sealed class DamageButton : MonoBehaviour
{
    [SerializeField] private HealthComponent target;

    /// <summary>向目标提交一次最终伤害。</summary>
    public void Hit() => target.Damage(3);
}
```

组件在 `Awake` 或第一次读取 `State`、提交命令时幂等初始化。初始化从序列化字段创建一个 `Health`，不会发布初始事件；配置非法则抛出异常，修正后可以再次 `Initialize`。运行时操作不写回 Inspector 初值。禁用再启用不会重置状态；销毁时组件解除对 Core 的事件转发。组件没有每帧 `Update`。

`State` 是 `IReadOnlyHealth`：可读取 `Current`、`Maximum`、`Normalized`、`Snapshot`，也可订阅带 `HealthChange` 参数的事件。Inspector 的 UnityEvent 没有参数，适合连接音效或表现；要判断原因、实际变化量或前后数值，应订阅 `State` 的强类型事件。

## 自行组合 Core

```csharp
using Computerzhuxi.Health;

public sealed class CharacterLife
{
    private readonly Health health = new Health(current: 6, maximum: 8);
    public IReadOnlyHealth State => health;

    /// <summary>订阅由当前实例拥有的生命变化。</summary>
    public CharacterLife() => health.Changed += OnHealthChanged;

    /// <summary>提交最终伤害，由核心负责状态与事件。</summary>
    public void Damage(int amount) => health.Damage(amount);

    /// <summary>读取存档时一次恢复当前值和上限。</summary>
    public void Restore(int current, int maximum) => health.Restore(current, maximum);

    /// <summary>所有者结束使用时解除自己的订阅。</summary>
    public void Release() => health.Changed -= OnHealthChanged;

    /// <summary>根据变化前后快照更新项目自己的表现。</summary>
    private void OnHealthChanged(HealthChange change)
    {
        // change.Before / After 是本次命令的不可变快照。
    }
}
```

Core 不依赖 MonoBehaviour，不会自行更新或销毁；创建者决定何时创建、保存引用、解除订阅以及丢弃实例。伤害计算、存档格式和游戏业务事件由项目适配层负责。`Restore` 一次恢复当前值与上限，只发布 `Changed`，不会重放死亡或复活结算。

命令在当前线程同步提交并发事件；事件回调内不允许同步提交另一条生命命令。零变化命令不发事件。完整输入范围、通知顺序和异常行为见 [API 契约](API.md)。

## 运行示例

通过 Package Manager 导入 `Basic Health Demo`，打开 `BasicHealthDemo.unity`。上半部分的按钮驱动 `HealthComponent`，下半部分的按钮驱动独立创建的 `Health`，各自显示事件。`HealthDemo.prefab` 保存 Inspector 初值和 `OnChanged` 的持久化绑定。示例说明与操作结果见 [Sample README](../Samples~/BasicHealthDemo/README.md)。
