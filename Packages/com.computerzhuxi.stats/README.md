# 📊 Stats

Stats 为 Unity 项目提供可复用的属性计算与绑定。业务模型持有权威的基础值（Base）和最终值（Final）；`StatRegistry` 管理定义、修正、重算和变化通知。包不决定角色成长、装备、Buff、存档格式或 UI。

## 📦 安装

要求 **Unity 6000.3** 或更高版本。通过 Unity Package Manager 使用已发布的固定 Git 标签安装 0.1.0：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.stats#stats-v0.1.0
```

开发本仓库源码时可通过 `Projects/StatsLab` 的本地 UPM 路径引用该包；固定标签的内容不会随工作区修改。

## 🚀 选择入口

| 方式 | 适用场景 |
| --- | --- |
| **StatCollectionComponent** | 在 Inspector 配置属性定义，并通过业务组件提供数值绑定 |
| **StatRegistry** | 已有角色模型，需要自行管理注册、修正及生命周期 |

两种入口遵循相同的计算规则。**同一项业务属性只保留一个权威模型和一个负责写入的 Registry**，避免组件与程序式入口同时修改它。

### 直接使用 StatRegistry

业务模型同时保存 Base 和 Final；绑定负责将 Registry 的读写连接到模型。将下面代码保存为 `AttackExample.cs`；在业务代码中调用 `AttackExample.Run()` 可得到 `180`，该静态类不需要挂到 GameObject。以下示例中，装备通过稳定的 `Source` 添加修正，卸下时按来源清理：

```csharp
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

    /// <summary>读取业务模型持有的基础攻击力。</summary>
    public double GetBase() => model.BaseAttack;

    /// <summary>写入业务模型持有的基础攻击力。</summary>
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
        using (StatRegistration registration =
            registry.Register(new StatDefinition("attack"), new AttackBinding(model)))
        {
            using (registry.BeginBatch())
            {
                registry.AddModifier("attack", new StatModifier(StatModifierCategory.Flat, 50, "sword"));
                registry.AddModifier("attack", new StatModifier(StatModifierCategory.Multiplier, 1.2, "buff"));
            }

            double finalAttack = registry.GetFinal("attack"); // (100 + 50) × 1.2 = 180
            registry.RemoveSource("sword");
            return finalAttack;
        }
    }
}
```

在实际角色中保存 `StatRegistry` 与 `StatRegistration`，角色销毁时释放注册。业务模型直接改变 Base 后调用 `registry.Invalidate("attack")`；通过 `registry.SetBase("attack", value)` 修改时会自动重算。可将 `IReadOnlyStatRegistry` 交给只需读取数值和订阅 `Changed` 的 UI。

### 使用 StatCollectionComponent

先保存上面的 `AttackExample.cs`，让项目中存在 `AttackStats` 和 `AttackBinding`；再将下面代码保存为 `AttackBindingProvider.cs`。在角色 GameObject 上同时添加 `StatCollectionComponent` 与 `AttackBindingProvider`，提供者显式按 ID 连接该角色自己的业务模型：

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

    /// <summary>在 Play 模式安装演示装备，并输出最终攻击力。</summary>
    [ContextMenu("Demo/Equip Attack Modifier")]
    private void DemoEquip()
    {
        // 编辑模式不执行命令；重复演示先清理同来源修正，避免叠加装备。
        if (!Application.isPlaying) return;
        var collection = GetComponent<StatCollectionComponent>();
        collection.Initialize();
        using (collection.BeginBatch())
        {
            collection.RemoveSource("demo-equipment");
            collection.AddModifier("attack", new StatModifier(StatModifierCategory.Flat, 50, "demo-equipment"));
            collection.AddModifier("attack", new StatModifier(StatModifierCategory.Multiplier, 1.2, "demo-equipment"));
        }
        Debug.Log($"Attack: {collection.Stats.GetFinal("attack")}");
    }
}
```

在 Project 窗口通过 **Create > Computerzhuxi > Stats > Definition** 创建 `StatDefinitionAsset`，将 `Stat Id` 设为 `attack`。首次尝试时保留默认策略、关闭 `Use Minimum` / `Use Maximum`，并将 `Rounding` 保持为 `None`；之后可按需配置上下限、取整和策略资产。把定义资产加入 `StatCollectionComponent` 的 `Definitions` 列表，再把同对象的 `AttackBindingProvider` 组件拖入 `Binding Provider`。

进入 Play 模式，在 Inspector 中右键点击 `AttackBindingProvider` 组件标题，选择 **Demo > Equip Attack Modifier**。Console 应显示 `Attack: 180`，即 `(100 + 50) × 1.2`；重复执行仍为 `180`。卸装时可调用 `collection.RemoveSource("demo-equipment")`，最终攻击回到 `100`。

组件在 `Start` 首次调用 `Initialize()`；需要更早使用时可显式初始化。`Stats` 在初始化前为 `null`，初始化后返回只读的 `IReadOnlyStatRegistry`，用于 `GetBase`、`GetFinal` 和 `Changed`；修改属性时使用组件的 `SetBase`、`AddModifier`、`RemoveSource` 等命令。

已有运行时定义时，也可调用 `component.Initialize(provider, runtimeDefinitions)`；它使用非空 `IReadOnlyList<StatDefinition>`，不读取 Inspector 字段。重复初始化保留首次创建的 Registry。禁用再启用组件保留状态，销毁组件时释放注册及修正，但不重置业务模型里的 Base/Final。

## 🧮 计算与修正

默认策略只接受 `StatModifierCategory.Flat` 和 `StatModifierCategory.Multiplier`，公式为 **(Base + Flat 合计) × Multiplier 乘积**。倍率 `1.2` 表示乘以 1.2。`StatDefinition` 可设置可选上下限和 `StatRounding.None`、`Floor`、`Ceiling`、`Nearest`；计算后先限幅再取整，`Nearest` 的中点向远离零的方向取整。启用整数取整时，上下限也必须是整数。

每项 `StatModifier` 都有类别、数值和 `Source`。`AddModifier` 返回只在当前 Registry 中有效的 `StatModifierHandle`；使用 `UpdateModifier` 或 `RemoveModifier` 精确操作一项修正，或用 `RemoveSource` 跨属性清理同一来源。后者按完全相同的来源字符串匹配。装备、Buff 等临时影响应使用修正；永久成长应改 Base。

`Register(definition, binding)` 返回需要在生命周期结束时释放的 `StatRegistration`。通常立即计算首次最终值；在批处理中注册时，首次计算延迟到最外层作用域释放。同一 Registry 中重复 ID 会被拒绝。无效或跨 Registry 的修正句柄在更新、移除时返回 `false`；释放注册会清除其修正，但不会重置业务模型。

需要百分比加算或其他类别时，实现 `IStatCalculationStrategy.SupportsCategory` 和 `Calculate`，作为 `new StatDefinition("attack", strategy)` 的策略传入。Inspector 入口可继承 `StatStrategyAsset`，在 `CreateStrategy()` 中返回策略实例，再赋给定义资产。自定义策略返回约束前的有限数，上下限和取整仍由定义统一处理。

## 🔔 读取、事件与快照

```csharp
double baseAttack = registry.GetBase("attack");
double finalAttack = registry.GetFinal("attack");
registry.Changed += change =>
{
    // change 含 StatId、PreviousBase、CurrentBase、PreviousFinal、CurrentFinal。
};
```

`GetBase` 每次从业务绑定读取当前值；`GetFinal` 返回最近一次成功提交并从绑定读回的值。基础值或最终值发生变化才发送 `Changed`，事件到达时新值已经提交。`TryGetBase` / `TryGetFinal` 在 ID 不存在时返回 `false` 并输出 `0`；`StatIds` 返回按 ID 排序的快照。

`BeginBatch()` 可嵌套，最外层作用域释放时才统一重算。批处理中，新的 Base 可立即读到，Final 仍是上次提交值。`CaptureBaseSnapshot()` 返回按 ID 排序的基础值快照，不包含修正、最终值或来源；存档格式和恢复临时修正由游戏负责。

## ⚠️ 使用规则

1. **权威状态**：`IStatBinding` 的业务模型持有 Base/Final。不要在 Registry 外修改 Final 后继续把旧的已提交读数当作新结果。
2. **初始化**：组件命令在初始化前会抛异常。缺少定义、绑定提供者或所需 ID 的绑定会使初始化失败；修正配置后可重试，绑定已有的外部副作用由业务方处理。
3. **输入**：属性 ID、修正类别与来源不能空白；Base、修正值、策略结果和绑定读回的 Final 必须为有限数。默认策略拒绝其他修正类别。
4. **回调**：绑定和策略执行期间不能同步调用同一个 Registry 的公共 API。`Changed` 观察者可以引发新一轮变更；连续超过 64 轮会被视为循环并抛异常。
5. **失败**：批处理合并重算，不提供事务回滚。一项提交失败不妨碍其他项提交；失败项保留失效状态供后续显式重试。提交或观察者错误以 `AggregateException` 汇报，已提交值及已发送事件不会回滚。

非批处理的首次注册计算失败会撤销该注册；`AddModifier` / `UpdateModifier` 在本次调用中抛出提交错误时，会恢复对应修正记录，但已经发生的绑定副作用不会回滚。批处理作用域释放时才出现的计算失败不会撤销此前已返回的注册或修正。观察者逐个收到已提交变化，一个观察者抛错不会阻止其他观察者收到通知。之后可显式调用 `Invalidate(id)` 重试失败属性。

## 🎮 示例与更新记录

通过 Package Manager 导入 **Basic Stats Demo**，打开 `BasicStatsDemo.unity`，查看组件与纯 C# 两种独立角色示例。操作步骤和预期数值见 [Sample README](Samples~/BasicStatsDemo/README.md)。版本变化见 [CHANGELOG](CHANGELOG.md)。
