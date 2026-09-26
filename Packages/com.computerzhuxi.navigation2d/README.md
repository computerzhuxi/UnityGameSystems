# Navigation 2D

二维同步网格 A* 路径查询、单角色导航代理、可选批量局部避让，以及显式 Physics2D 净空检查。正式 Runtime **只计算路径和移动建议**；实际位置、速度、碰撞后的执行结果由消费项目负责。

## 选择入口

| 需求 | 入口 | 导航任务与路径的拥有者 |
|---|---|---|
| Inspector 装配，固定帧自动更新 | `NavigationNavigator2D` | 组件内唯一的 `NavigationAgent2D` |
| 已有运动器、空间层或自定义更新顺序 | 公开 `NavigationAgent2D`、`GridPathfinder2D`、`ITraversalSource2D`、`AvoidanceWorld2D` 自行组合 | 选用 Agent 时由 Agent 持有；直接 FindPath 时由调用方持有 |
| 保留原有手动组件调用 | `NavigationAgent2DComponent` + 显式 `Tick` | 组件内唯一的 `NavigationAgent2D` |

同一角色只选一个导航任务拥有者。两种组件最终共用 Agent 的路径与状态规则；不要同时挂载并驱动两套组件。两条路线都不接管实际运动，也不要求纯 C# 或指定刚体类型。[Getting Started](Documentation~/GettingStarted.md) 给出两条最小接入路径。

## 安装

Package Manager 可通过固定标签安装已发布的 0.2.0：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.navigation2d#navigation2d-v0.2.0
```

本仓库中的 **Unreleased 修复不在该固定标签中**。开发及验证这些改动时使用 `Projects/Navigation2DLab` 的本地 UPM 路径；发布新版本和更新消费项目的依赖需走独立发布流程。包要求 Unity 6000.3 及 Physics2D 模块。

## 运行方式与边界

`GridPathfinder2D.FindPath` 每次读取障碍源，将完整路径覆盖到调用方列表；失败时清空列表。路径包含经过验证的格中心连接，最后一点是精确终点。Agent 负责目的地、路径、索引、重算节流、暂停与到达状态，输出 `DesiredDirection` 和 `RemainingWaypointDistance`。Navigator 自动在 `FixedUpdate` 提供真实身体位置与 Physics2D 来源，项目运动器须消费建议、限制本步距离，并核查实际移动段。[API 契约](Documentation~/API.md) 描述失败与预算语义。

`AvoidanceWorld2D` 批量计算建议速度，不管理对象或位置；调用方须在同一步先采集所有实际位置和速度，再求解、检查环境并统一执行。群体拥堵、自动脱困、跨层楼梯、战斗站位、重力与动作优先级仍由项目处理。

## Samples 与 Lab

- **Quick Start Navigation 2D**：已序列化的 Navigator + 仅供演示的 Kinematic 运动适配器；适合先看 Inspector 装配。导入后打开 `QuickStartNavigation2D.unity`。
- **Basic Navigation 2D**：直接查询并自行持有路径与跟随进度。
- **Agent Navigation 2D**：保留手动 Tick 组件的现有用法，展示移动目标、暂停与动态障碍。
- **Crowd Navigation 2D**：公开 Agent + Avoidance 的程序式组合。

`QuickStartExampleMover2D` 仅在 Sample/Lab 程序集中，不是正式 Runtime 或生产运动能力。四类 Sample 的正式源位于 `Samples~`；Lab 的 `Assets/Samples` 是逐文件校验的导入镜像。[Lab 操作](../../Projects/Navigation2DLab/README.md) 包含场景与人工验收步骤。

## 文档

- [Getting Started：组件与程序式接入](Documentation~/GettingStarted.md)
- [Unity Inspector 与运动器接入](Documentation~/UnityGuide.md)
- [API、失败语义与数值边界](Documentation~/API.md)
- [架构与状态归属](Documentation~/Architecture.md)
- [维护与验证](Documentation~/Maintenance.md)
- [第三方来源](Documentation~/Research.md) · [版本变化](CHANGELOG.md)

查询为同步主线程操作；圆形占位、四/八方向、最近中心网格。没有碰撞体不代表有地面：地图洞口与边界要由障碍源限制。包不提供异步、3D、自动路径平滑或跨层图。
