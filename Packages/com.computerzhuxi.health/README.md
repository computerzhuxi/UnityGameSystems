# Health System

独立整数生命系统，当前稳定版本 1.0.1，已在 Unity 6000.3.21f1 验证。

## 它负责什么

- 当前生命和生命上限；
- 伤害、治疗、死亡和复活；
- 修改生命上限和恢复完整状态；
- 不可变变化结果、只读状态和同步事件；
- 可选的 Unity MonoBehaviour 与 UnityEvent 接入。

它不负责命中、攻击力、防御、无敌、护甲、奖励、UI、动画、音效或具体游戏的存档格式。这些规则应位于消费项目或其适配层。

## 安装

在 Unity Package Manager 中使用：

    https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.health#health-v1.0.1

私有仓库需要开发机或 CI 具有最小只读权限。凭据不能写入 URL、manifest 或仓库。

## 纯 C# 快速开始

    using Computerzhuxi.Health;

    Health health = new Health(current: 10, maximum: 10);
    health.Damage(3);
    Console.WriteLine(health.Current); // 7

将 IReadOnlyHealth 提供给 UI 等观察者，可以读取状态和监听事件，但不能提交生命命令。

## Unity 快速开始

给 GameObject 添加 Computerzhuxi.Health.HealthComponent，在 Inspector 配置初值和 UnityEvent：

    HealthComponent health = GetComponent<HealthComponent>();
    health.Damage(3);
    health.Heal(2);

组件没有 Update；禁用再启用不会重置运行时状态。序列化初值必须满足 maximum ≥ 1 且 startingHealth 位于 0 到 maximum 之间，即使选择满血启动也一样。

## 选择接入方式

|场景|建议|
|---|---|
|简单 Unity 项目|直接挂 HealthComponent|
|已有角色架构的中型项目|项目适配层调用 Health Core|
|纯逻辑或非 Unity 测试|直接使用 Health Core|

## 文档

- [文档入口](Documentation~/Index.md)
- [架构与边界](Documentation~/Architecture.md)
- [Core 使用指南](Documentation~/CoreGuide.md)
- [Unity 组件指南](Documentation~/UnityGuide.md)
- [中型项目集成](Documentation~/IntegrationGuide.md)
- [精确 API 契约](Documentation~/API.md)
- [版本迁移](Documentation~/Migration.md)
- [常见问题](Documentation~/Troubleshooting.md)
- [版本记录](CHANGELOG.md)

导入 Basic Health Demo Sample 后，可运行全部命令并查看 UnityEvent 和序列化示例。
