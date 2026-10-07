# 直接使用 Health Core

如果你的项目已经有自己的角色、Entity、Model 或 Gameplay 架构，通常可以直接使用 `Health`，而不需要依赖 `HealthComponent`。

**适用场景**：
- 不希望依赖 `MonoBehaviour`
- 需要自主管理对象的生命周期
- 已有自定义的角色数据模型
- 希望通过强类型事件接入系统
- 希望明确控制“谁有权限修改生命状态”

---

## 📦 实例化与只读暴露

### 创建 Health
直接创建一个 `Health` 实例，构造参数必须满足 `maximum >= 1` 且 `0 <= current <= maximum`：

```csharp
using Computerzhuxi.Health;

// 初始化：当前生命值 80，最大生命值 100
var health = new Health(current: 80, maximum: 100);
```
> ⚠️ **注意**：传入非法状态（如 `new Health(-1, 100)` 或 `new Health(120, 100)`）会抛出 `ArgumentOutOfRangeException`。

### 为什么推荐对外暴露 IReadOnlyHealth？
`Health` 实现了 `IReadOnlyHealth` 接口。为了防止外部模块绕过业务逻辑直接修改生命值，推荐**业务拥有者持有可写的 `Health`，而对外只暴露只读接口**。

```csharp
// UI 只需要读取生命状态，不需要修改权限
public sealed class HealthViewModel
{
    private readonly IReadOnlyHealth health;

    public HealthViewModel(IReadOnlyHealth health)
    {
        this.health = health;
    }

    public float Normalized => health.Normalized;
}
```
**推荐的单向依赖关系**：
`Character / Gameplay Owner` ➔ 拥有 ➔ `Health`
`UI / Observer` ➔ 观察 ➔ `IReadOnlyHealth`

---

## ⚔️ 提交命令与处理结果

可以直接调用以下命令修改生命状态。所有命令都会返回一个 `HealthChange` 结构，准确记录本次操作的 `Before`、`After`、`Reason`、`Delta` 和 `ActualAmount`。

```csharp
health.Damage(20);
health.Heal(10);
health.Kill();
health.Revive(50);
health.ChangeMaximum(150, MaximumHealthPolicy.PreserveCurrent);
health.Restore(80, 120);
```

### 使用 HealthChange 代替重新推断
当发起命令的代码需要立即知道结果时，直接使用返回的 `HealthChange`，**不需要手动计算实际变化量**。

```csharp
HealthChange change = health.Damage(50);

if (change.HasChanged)
{
    // 例如：当前 20/100，受到 50 点伤害。
    // 这里 change.ActualAmount 会精准返回 20。
    Debug.Log($"实际伤害: {change.ActualAmount}");
}
```

---

## 📡 监听状态变化

`Health` 提供了强类型事件供外部订阅。

### 订阅与解除订阅
谁建立事件订阅，谁就应该负责在生命周期结束时解除订阅（`Health` 本身不知道观察者何时销毁）：
```csharp
health.Changed += OnChanged;
// 生命周期结束时：
health.Changed -= OnChanged;
```

### 监听 Changed 还是专用事件？
- **只关心“状态变了”**：监听 `Changed`。适合血条 UI、调试面板、状态同步标记等。
- **关心“变化语义”**：监听 `Damaged` / `Healed` / `Died` / `Revived`。适合受击特效、死亡动画、治疗跳字等。

### 🚫 严禁事件回调中同步重入
**绝对不允许在事件回调中同步修改同一个 Health 实例。**
```csharp
health.Died += change =>
{
    health.Revive(100); // ❌ 抛出 InvalidOperationException
};
```
**正确做法**：上层系统记录“需要复活”的状态，待当前事件调用栈结束后，再（如在下一帧）执行新的生命命令。

---

## 📐 架构最佳实践

### 1. Health 不应该替代战斗结算
Health 接收的是**最终生命变化命令**。不要把攻击力、防御力、暴击等计算逻辑塞进 Health。
> **推荐流程**：命中判定 ➔ 计算攻击/防御/暴击 ➔ 得出最终伤害 ➔ `Health.Damage(finalDamage)`

### 2. 不要用 Gameplay 命令恢复存档
恢复存档或网络同步时，应使用 `Restore` 而不是 `Damage` / `Heal` / `Kill` / `Revive`。
```csharp
// ✅ 正确：只触发 Changed，静默恢复状态
health.Restore(savedCurrent, savedMaximum);

// ❌ 错误：如果 savedHealth 为 0 调用 Kill()，会错误触发 Died 事件
// 导致重复播放死亡动画或重复发放击杀奖励。
```

### 3. 典型的职责划分
每个 `Health` 实例都是彼此独立的。一个清晰的角色架构通常如下划分职责：
- **`Health`**：只负责管理和校验最终生命状态。
- **`Combat`**：负责计算最终伤害，调用 `Damage`。
- **`HealthView`**：持有 `IReadOnlyHealth`，观察状态更新 UI。
- **`SaveSystem`**：读取 `health.Snapshot` 保存，调用 `Restore` 恢复。

---

## 🎮 完整角色类示例

这是一个标准的、将 Health 封装在角色类中的使用模板：

```csharp
using Computerzhuxi.Health;

public sealed class Character
{
    // 内部持有可写的核心实例
    private readonly Health health;

    // 对外暴露只读观察接口
    public IReadOnlyHealth Health => health;

    public Character(int current, int maximum)
    {
        health = new Health(current, maximum);

        health.Died += OnDied;
        health.Revived += OnRevived;
    }

    // 战斗系统调用的统一入口
    public HealthChange ReceiveDamage(int finalDamage)
    {
        return health.Damage(finalDamage);
    }

    public HealthChange RestoreHealth(int amount) => health.Heal(amount);
    public HealthChange Revive(int value) => health.Revive(value);

    // 存档与读档支持
    public HealthSnapshot CaptureHealth() => health.Snapshot;
    public void RestoreHealthState(int current, int maximum) => health.Restore(current, maximum);

    // 响应状态变化驱动 Gameplay 逻辑
    private void OnDied(HealthChange change)
    {
        // 通知上层角色状态系统（如：禁用移动、播放动画）
    }

    private void OnRevived(HealthChange change)
    {
        // 恢复角色可用状态
    }
}
```

---

## 🚀 接下来

- 如果你的项目重度依赖 GameObject、Inspector 和 UnityEvent，可以阅读：👉 **[使用 HealthComponent](unity-component.md)**
- 如果需要查询具体 API 签名和行为：👉 **[API 参考](../reference/api.md)**