# Health System

Health System 是一个面向 Unity 的独立整数生命值系统。

它提供生命状态、伤害、治疗、死亡、复活、生命上限修改、状态恢复以及同步事件，并将纯 C# Core 与 Unity `MonoBehaviour` 接入层完全分离。

你可以直接将 `Health` 组合进自己的角色架构中，也可以使用 `HealthComponent` 在 Inspector 中配置并通过 `UnityEvent` 接入场景逻辑。

---

## ✨ 功能特性

- **核心状态**：当前生命值与最大生命值管理
- **基础交互**：伤害与治疗、死亡与显式复活
- **高级控制**：动态修改最大生命值、完整状态恢复
- **明确的变化结果**：不可变状态快照、每次命令返回 `HealthChange`
- **事件驱动**：提供强类型 C# 事件，支持 Inspector 与 `UnityEvent` 接入
- **架构解耦**：Core 与 Unity 层分离，提供只读观察接口 `IReadOnlyHealth`

### ⛔ 系统边界
Health System **不负责**以下内容：
- 攻击力计算 / 命中判定 / 防御与减伤 / 无敌状态 / Buff 与 Debuff
- 动画 / UI表现 / 奖励结算 / 存档格式 / 网络协议
> **说明**：保持职责单一，上述逻辑应由上层游戏系统（如战斗系统）负责。

---

## ⚙️ 环境要求

- Unity **6000.3** 或更高版本

---

## 📦 安装指南

通过 Unity Package Manager 使用 Git URL 进行安装：

```bash
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health
```

如果项目需要固定版本，建议在末尾追加明确的 tag 或 commit：

```bash
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#<tag-or-commit>
```

---

## 🚀 快速开始

Health System 提供两种接入方式，请根据你的项目架构进行选择：

### 方式一：使用 `HealthComponent`（推荐快速原型 / 简单架构）
**适用场景**：希望直接通过 Inspector 配置生命值、需要 `UnityEvent` 挂载场景逻辑、直接挂载到 GameObject。
> **注**：组件在运行时会自动创建并持有一个内部的 `Health` 实例。

1. 给 GameObject 添加 `HealthComponent`。
2. 通过代码提交生命命令：

```csharp
using Computerzhuxi.Health;
using UnityEngine;

public sealed class Enemy : MonoBehaviour
{
    [SerializeField]
    private HealthComponent health;

    public void TakeDamage(int amount)
    {
        health.Damage(amount);
    }
}
```

**常用读取与复活操作：**
```csharp
// 读取状态
Debug.Log(health.State.Current);
Debug.Log(health.State.Maximum);
Debug.Log(health.State.IsAlive);

// 复活死亡对象
health.Revive(health.State.Maximum);
```

### 方式二：直接使用 `Health` Core（推荐纯净架构 / 自定义 Entity）
**适用场景**：已有自己的角色架构、不希望依赖 `MonoBehaviour`、需要自行管理生命周期和强类型事件。
> **注**：同一个游戏实体通常只应拥有一个权威生命实例。

```csharp
using Computerzhuxi.Health;

// 初始化：当前生命值 100，最大生命值 100
var health = new Health(100, 100);

// 受到伤害
health.Damage(30);

// 治疗并获取明确的变化结果
HealthChange result = health.Heal(10);
Debug.Log($"治疗前: {result.Before.Current}");
Debug.Log($"治疗后: {result.After.Current}");
Debug.Log($"实际治疗量: {result.ActualAmount}");

// 订阅状态变化事件
health.Changed += change =>
{
    Debug.Log($"生命值变化: {change.Before.Current} -> {change.After.Current}");
};
```

---

## 🧠 核心规则

Health System 遵循一套严格的内部逻辑，确保状态可靠：

1. **数值边界**：
   - `Maximum >= 1`
   - `0 <= Current <= Maximum`
2. **死亡判定**：死亡不是独立保存的状态，由生命值决定。
   - `Current > 0` ➔ **Alive**
   - `Current == 0` ➔ **Dead**
3. **治疗限制**：`Heal` 不会复活死亡对象。
   - 例：`0 / 100` ➔ `Heal(50)` ➔ 结果仍为 `0 / 100`
4. **复活机制**：复活必须显式调用（如 `health.Revive(50)`）。
5. **致死与强制死亡的语义区别**：
   - **强制死亡**：`Kill()` ➔ 触发 `Changed` ➔ 触发 `Died`（不会被解释为一次伤害）
   - **致死伤害**：`Damage()` ➔ 触发 `Damaged` ➔ 触发 `Changed` ➔ 触发 `Died`

---

## 📚 文档与示例

### 📖 详细文档
- [文档首页](Documentation~/index.md) | [快速入门](Documentation~/getting-started.md)
- [生命模型](Documentation~/concepts/health-model.md) | [命令与事件](Documentation~/concepts/commands-and-events.md)
- [直接使用 Core](Documentation~/guides/core-usage.md) | [使用 HealthComponent](Documentation~/guides/unity-component.md)
- [API 参考](Documentation~/reference/api.md) | [故障排查](Documentation~/troubleshooting.md)
- - [更新记录](CHANGELOG.md)

### 🎮 示例 (Samples)
可以通过 Package Manager 导入 **Basic Health Demo**。示例涵盖了：
- `HealthComponent` 与 独立 `Health` 的接入对比
- Damage / Heal / Kill / Revive / ChangeMaximum / Restore 等完整流转
- UnityEvent 与 强类型变化事件的使用示范