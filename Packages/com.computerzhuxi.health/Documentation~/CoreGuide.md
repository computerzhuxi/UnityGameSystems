# Health Core 使用指南

## 创建

    Health health = new Health(current: 10, maximum: 10);

maximum 必须至少为 1，current 必须位于 0 到 maximum 之间。构造不会发布事件。

## 常用命令

    health.Damage(3);
    health.Heal(2);
    health.Kill();
    health.Revive(5);
    health.ChangeMaximum(20, MaximumHealthPolicy.Refill);
    health.Restore(8, 20);

- Damage 和 Heal 只对存活实例生效。
- Revive 只复活死亡实例。
- PreserveCurrent 保留当前生命并在必要时截断。
- Refill 将当前生命设置为新上限。
- Restore 用于同步完整状态，只发布 Changed，不重放死亡或复活玩法事件。

每个命令返回 HealthChange。HasChanged 表示是否真的发生数值变化；Before 和 After 可用于日志、表现和测试。

## 只读访问

    IReadOnlyHealth state = health;

把 state 交给 UI、AI 或表现层。它们可以读取 Current、Maximum、Normalized 和监听事件，但不能调用写命令。

## 事件

    health.Changed += change => RefreshBar(change.After);
    health.Died += change => EnterDeadState();

事件同步执行。事件回调内不能再次提交生命命令；需要连锁操作时，在回调外排队处理。

精确事件顺序和无变化规则见 [API 契约](API.md)。
