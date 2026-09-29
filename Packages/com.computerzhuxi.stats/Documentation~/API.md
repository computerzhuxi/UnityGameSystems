# Stats 公开 API

命名空间为 `Computerzhuxi.Stats`。Core 使用纯 C#；Unity 装配层另见[上手指南](GettingStarted.md)。

|类型|用途|
|---|---|
|`StatDefinition`|以 `Id`、计算策略、可选上下限和 `StatRounding` 定义一项属性。ID 在同一 Registry 中唯一。|
|`IStatBinding`|连接业务域权威状态：`GetBase`、`SetBase`、`GetFinal`、`ApplyFinal`。|
|`IStatCalculationStrategy`|用 `SupportsCategory` 声明类别，并用 `Calculate` 计算约束前的值。|
|`StatModifier`|不可变的 `Category`、`Value`、`Source`。`Source` 用于按装备或 Buff 等来源整体清理。|
|`IReadOnlyStatRegistry`|只读属性 ID、基础值、已提交最终值和 `Changed` 事件。|
|`StatRegistry`|注册、修改基础值、管理修正、失效重算、批处理及快照。|
|`StatRegistration`|单次注册的 `IDisposable` 句柄；释放会移除该注册及修正。|
|`StatModifierHandle`|只在创建它的 Registry 中有效的单项修正句柄。|
|`StatChange`|一次成功提交的 `StatId`、`PreviousBase`、`CurrentBase`、`PreviousFinal`、`CurrentFinal`。|

## 定义与计算

`new StatDefinition(id, strategy: null, minimum: null, maximum: null, rounding: StatRounding.None)` 使用默认策略。默认只接受 `StatModifierCategory.Flat` 与 `StatModifierCategory.Multiplier`，公式为 `(Base + Flat 合计) × Multiplier 积`。倍率值直接相乘：例如 `1.2` 表示乘以 1.2，`0.8` 表示乘以 0.8。修正按加入顺序传给策略；自定义类别应由自定义策略的 `SupportsCategory` 接受，并在 `Calculate` 中实现语义。

`StatDefinition.Constrain` 先限幅、再按 `None`、`Floor`、`Ceiling` 或 `Nearest` 取整；`Nearest` 的中点向远离零的方向取整。启用整数取整时，配置的上下限必须为整数。策略返回值与基础值、修正数值均需为有限数。

## 注册、读取与命令

|成员|行为|
|---|---|
|`Register(definition, binding)`|读取绑定的初始 Base/Final，注册唯一 ID 并立即重算；返回释放句柄。|
|`StatIds`|按 ID 的序号比较规则排序的标识快照。|
|`GetBase(id)` / `TryGetBase(id, out value)`|实时读取绑定的业务域基础值。|
|`GetFinal(id)` / `TryGetFinal(id, out value)`|读取上次成功提交的最终值。|
|`SetBase(id, value)`|调用绑定写入永久基础值，然后使该属性失效并重算。|
|`Invalidate(id)`|业务域自行改动基础值后，请求重算。|
|`AddModifier(id, modifier)`|验证策略是否支持类别，加入修正并返回句柄。|
|`UpdateModifier(handle, modifier)`|用同一句柄替换修正；句柄无效返回 `false`。|
|`RemoveModifier(handle)`|移除精确句柄；重复或跨 Registry 移除返回 `false`。|
|`RemoveSource(source)`|跨所有属性删除相同来源的修正，返回删除项数。|
|`BeginBatch()`|返回可嵌套的 `IDisposable` 作用域；最外层释放时提交失效属性。|
|`CaptureBaseSnapshot()`|返回按 ID 排序、只包含当前基础值的快照；调用方决定存档格式。|

`GetBase`、`GetFinal` 需要已注册 ID；`TryGet*` 在 ID 不存在时返回 `false` 且输出 `0`。`StatRegistration.Dispose()` 幂等，旧注册句柄不会释放后来复用同一 ID 的注册。详见[契约](Contracts.md)。

## Unity 装配层

|类型或成员|行为|
|---|---|
|`StatDefinitionAsset`|Inspector 配置 ID、可选 `StatStrategyAsset`、上下限与取整；`CreateDefinition()` 读取当时的序列化配置。|
|`StatStrategyAsset`|抽象 `ScriptableObject` 工厂；`CreateStrategy()` 返回一次注册使用的 `IStatCalculationStrategy`。|
|`IStatBindingProvider.TryGetBinding(id, out binding)`|显式提供本角色的业务域绑定；缺少所需 ID 会使组件初始化失败。|
|`StatCollectionComponent.Initialize()`|读取组件的绑定提供者与定义资产；`Start` 在尚未初始化时自动调用。|
|`StatCollectionComponent.Initialize(provider, runtimeDefinitions)`|用代码提供的非空定义列表和绑定提供者装配组件。|
|`StatCollectionComponent.IsInitialized` / `Stats`|查看是否完成初始化；`Stats` 为只读 Registry 接口，初始化前为 `null`。|
|组件命令|代理 `SetBase`、`Invalidate`、`AddModifier`、`UpdateModifier`、`RemoveModifier`、`RemoveSource`、`BeginBatch` 和 `CaptureBaseSnapshot`；初始化前调用抛异常。|

定义资产只在首次初始化时读取；运行中编辑资产不会自动修改已创建的定义。组件销毁时释放自身注册与修正；禁用时不释放。
