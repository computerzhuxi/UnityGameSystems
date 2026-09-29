# 上手：两种入口

使用 `StatCollectionComponent` 时，在 Inspector 装配定义与业务绑定；已有角色模型需要自行控制生命周期时，可直接使用纯 C# 的 `StatRegistry`。两条入口都把业务域的 Base/Final 作为权威状态，按相同的定义与策略计算。请避免让两套 Registry 同时写入同一项业务属性。

## 编程式入口

下例可放在消费项目的 C# 代码中。业务模型同时保存 Base 和 Final，绑定负责双向读写；装备加成使用稳定来源，卸下时按来源删除。实际生命周期中保留 `StatRegistration`，角色销毁时释放。

```csharp
using System;
using Computerzhuxi.Stats;

public sealed class AttackStats
{
    public double BaseAttack = 100;
    public double FinalAttack = 100;
}

public sealed class AttackBinding : IStatBinding
{
    private readonly AttackStats model;

    /// <summary>将属性绑定到角色业务模型。</summary>
    public AttackBinding(AttackStats model) { this.model = model; }

    /// <summary>读取业务模型持有的永久攻击力。</summary>
    public double GetBase() => model.BaseAttack;

    /// <summary>写入业务模型持有的永久攻击力。</summary>
    public void SetBase(double value) => model.BaseAttack = value;

    /// <summary>读取业务模型持有的最终攻击力。</summary>
    public double GetFinal() => model.FinalAttack;

    /// <summary>提交计算后的最终攻击力。</summary>
    public void ApplyFinal(double value) => model.FinalAttack = value;
}

public static class AttackExample
{
    /// <summary>演示注册、装备修正和解除注册的完整周期。</summary>
    public static double Run()
    {
        var model = new AttackStats();
        var registry = new StatRegistry();
        using (registry.Register(new StatDefinition("attack"), new AttackBinding(model)))
        {
            using (registry.BeginBatch())
            {
                registry.AddModifier("attack", new StatModifier(StatModifierCategory.Flat, 50, "sword"));
                registry.AddModifier("attack", new StatModifier(StatModifierCategory.Multiplier, 1.2, "buff"));
            }
            // (100 + 50) × 1.2 = 180；最终值从业务绑定读回。
            double result = registry.GetFinal("attack");
            registry.RemoveSource("sword");
            return result;
        }
    }
}
```

若业务模型直接改变 `BaseAttack`，随后调用 `registry.Invalidate("attack")`。若经 Registry 写入，使用 `registry.SetBase("attack", value)`；批处理中新 Base 可立即读到，而 Final 在最外层 `Dispose()` 时更新。`IReadOnlyStatRegistry` 可交给 UI 等只需读数和 `Changed` 的消费者。

## 组件式入口

在角色 GameObject 上挂载 `StatCollectionComponent` 和实现 `IStatBindingProvider` 的组件。下面的提供者沿用上节的 `AttackStats` 与 `AttackBinding`，把 `attack` ID 显式连接到模型，无需按字段名反射：

```csharp
using Computerzhuxi.Stats;
using UnityEngine;

public sealed class AttackBindingProvider : MonoBehaviour, IStatBindingProvider
{
    private readonly AttackStats model = new AttackStats();
    private AttackBinding binding;

    /// <summary>按属性 ID 返回本角色持有的权威数值绑定。</summary>
    public bool TryGetBinding(string statId, out IStatBinding result)
    {
        if (statId != "attack") { result = null; return false; }
        if (binding == null) binding = new AttackBinding(model);
        result = binding;
        return true;
    }
}
```

在 Project 窗口通过 **Create > Computerzhuxi > Stats > Definition** 建立 `StatDefinitionAsset`，把 `Stat Id` 设为 `attack`，按需设置上下限、取整与策略资产。将资产加入组件的 `Definitions` 列表，并把 `AttackBindingProvider` 拖入 `Binding Provider`。组件在 `Start` 自动执行首次 `Initialize()`，此时读取资产配置并向提供者取得绑定；`Stats` 是只读的 `IReadOnlyStatRegistry`，可用于显示 `GetBase/GetFinal` 和订阅 `Changed`。修改时调用组件的 `SetBase`、`AddModifier`、`RemoveSource` 等命令。组件命令在初始化前会抛出异常；如需在 `Start` 前使用，可先调用 `Initialize()`。

已有定义对象时，也可调用 `component.Initialize(provider, runtimeDefinitions)` 显式装配同一组件；`runtimeDefinitions` 是非空的 `IReadOnlyList<StatDefinition>`。此入口不读取 Inspector 的定义列表或绑定提供者字段。重复初始化保持首次 Registry，不重新读取资产。禁用与再次启用组件保留状态；`OnDestroy` 释放注册与修正，业务模型本身的 Base/Final 不会被重置。初始化失败时组件仍未初始化，可修正配置后重试；绑定内部已发生的副作用需要业务方自行处理。

## 自定义计算

默认策略只认识 `Flat`（固定加成）和 `Multiplier`（倍率），公式为 `(Base + Flat 合计) × Multiplier 积`。需要百分比加算、分组倍率或其他类别时，实现 `IStatCalculationStrategy` 的 `SupportsCategory` 与 `Calculate`，并作为 `StatDefinition` 的 `strategy` 参数传入。Inspector 入口可继承 `StatStrategyAsset`，在 `CreateStrategy()` 中返回策略实例，再赋给定义资产的 `Strategy` 字段。策略返回约束前的有限数，由定义统一限幅和取整。策略回调中不要同步调用当前 `StatRegistry`。

例如自定义 `Percent` 类别，把它解释为百分比加算；`0.2` 表示增加 20%：

```csharp
using System;
using System.Collections.Generic;
using Computerzhuxi.Stats;

public sealed class PercentBonusStrategy : IStatCalculationStrategy
{
    /// <summary>声明策略接受固定加成和百分比加算。</summary>
    public bool SupportsCategory(string category) =>
        category == StatModifierCategory.Flat || category == "Percent";

    /// <summary>先累加固定加成，再将百分比合计作用于基础结果。</summary>
    public double Calculate(double baseValue, IReadOnlyList<StatModifier> modifiers)
    {
        double flat = 0;
        double percent = 0;
        foreach (StatModifier modifier in modifiers)
        {
            if (modifier.Category == StatModifierCategory.Flat) flat += modifier.Value;
            else if (modifier.Category == "Percent") percent += modifier.Value;
            else throw new ArgumentException("不支持修正类别：" + modifier.Category);
        }
        return (baseValue + flat) * (1 + percent);
    }
}

// 注册时传入：new StatDefinition("attack", new PercentBonusStrategy())
// 加入时传入：new StatModifier("Percent", 0.2, "passive-skill")
```

完整成员及错误行为见 [API](API.md) 与 [契约](Contracts.md)。
