# HealthComponent 使用指南

> 第一次使用请先阅读 [上手指南](GettingStarted.md)。本页进一步说明组件配置与 UnityEvent 接入。

## 添加组件

1. 给目标 GameObject 添加 HealthComponent。
2. 设置 Maximum、Start With Full Health 和 Starting Health。
3. 按需要绑定 On Changed、On Damaged、On Healed、On Died、On Revived。

合法初值要求 maximum ≥ 1，startingHealth 位于 0 到 maximum 之间。满血开关只决定实际初始当前值，不会跳过配置校验。

## 代码调用

    HealthComponent health = GetComponent<HealthComponent>();
    health.Damage(3);
    health.Heal(2);

通过 State 获取 IReadOnlyHealth：

    float fill = health.State.Normalized;

## 生命周期

组件在 Awake 中幂等初始化，没有 Update。禁用和重新启用不会创建第二个 Core，也不会重置状态。销毁时解除其对 Core 的事件转发（本地 Unreleased，固定 `health-v1.0.1` 尚未包含）。非法配置抛出 ArgumentOutOfRangeException；修正字段后可以再次 Initialize。完整的双入口生命周期示例见 [上手指南](GettingStarted.md)。

## UnityEvent

UnityEvent 用于无参数的场景和 Prefab 表现绑定。需要变化前后数值时，应在代码中订阅 State 的强类型事件。

## 示例

从 Package Manager 导入 Basic Health Demo，然后打开 BasicHealthDemo.unity。示例包含全部命令、状态显示、持久化 UnityEvent 和 Prefab。
