# Inspector 导航组件指南（0.2.0）

## 正式包只负责导航

`NavigationNavigator2D` 自动读取真实位置并更新路径，只提供只读结果和方向建议，永不写 Transform、Rigidbody2D 位置或速度。正式 Runtime 没有运动组件。实际项目必须由自己的移动系统消费建议；已有状态机也可直接组合查询、NavigationAgent2D 与 AvoidanceWorld2D。

旧 `NavigationAgent2DComponent` 保留手动 Tick 契约。不要在同一角色上同时维护两种导航状态拥有者。ARPG 继续底层 API + EnemyNavigation / CharacterMovement2D，不引入 Navigator 或任何示例运动器。

## Inspector 配置，无需修改导航源码

1. 给角色添加 **Navigation Navigator 2D**。
2. 把身体 Collider2D 拖入 Body Collider；不指定身体时使用 Radius，只查询路径也可以。身体必须启用且不是 Trigger。
3. 给地图障碍设置独立 Layer，选入 Obstacle Mask；身体自身层不要选入。
4. 把目标 Transform 拖入 Target。目标不能占住所选障碍域中的终点。
5. 运行并查看 Inspector 状态、最后查询结果、净空半径和剩余路径点；Scene 视图开启 Gizmos。**角色此时不会自动移动**，这是导航职责边界。
6. 让项目移动系统读取下述只读建议，完成移动；如果只想看演示，导入 QuickStart 示例场景。

导航不要求某一种刚体类型，也不擅自设置 Kinematic、速度或约束。身体存在时读取实际物理位置，避免插值显示位置影响路径进度。

## Inspector 参数与实时生效规则

所有配置运行时可修改，最迟下个固定帧生效；暂停时间时等待固定帧恢复。查询配置改变后保留目的地、丢弃旧路径并重算，非法配置显示错误并清除建议，修正后可恢复。

| 字段 | 语义 |
|---|---|
| Target | 跟踪目标；修改引用取消旧任务，移除引用停止 |
| Body Collider | 用于推导世界空间保守圆形净空，不负责移动 |
| Radius / Clearance | 未指定身体时的半径及附加净空 |
| Obstacle Mask / Include Triggers | 障碍层及 Trigger 查询规则 |
| Grid Origin / Cell Size / Directions | 网格原点、格距与四/八方向 |
| Max Expanded Nodes | 单次搜索展开预算；BudgetExceeded 不表示已经证明不可达 |
| Arrival Distance | 到达与路径点容差 |
| Destination Change Threshold | 移动目标累计偏移达到阈值时重算 |
| Repath Interval / Failure Retry Interval | 请求节流与失败重试间隔 |
| Look Ahead Distance | 通过既有 Agent 实现安全路径前视 |
| Draw Path | 绘制剩余路径及净空 |

居中圆使用缩放后的实际半径；矩形、偏移等身体使用保守包围圆，可能拒绝狭窄通道。尺寸变化会触发重新规划。

## 命令与只读结果

- `SetDestination(Vector2)` 设置固定坐标并解除 Transform 跟踪；配置非法时抛出明确异常。
- `SetTarget(Transform)` 跟踪对象，传空等价于停止。
- `Stop()` 清除任务和目标引用，不会下帧自动重启。
- `Pause()` / `Resume()` 保留任务并暂停/恢复建议；禁用再启用不解除显式暂停。
- `InvalidatePath()` 在传送或已知地图变化后失效旧路径。
- 只读 `CurrentPath`、`CurrentPathIndex`、`DesiredDirection`、`RemainingWaypointDistance`、`State`、`LastResult`、`ConfigurationError`、`Position`、`EffectiveRadius`；没有可写 Agent 或路径入口。
- `IsMovementClear(start, end)` 查询移动段净空，不执行移动。

目标暂时失活时取消当前任务，重新激活后重新跟踪；目标销毁后取消跟踪任务。禁用、暂停或停止导航只改变导航状态/建议，**不会停止外部运动系统的既有速度**。实际运动系统负责对应停止行为。

## 自定义移动系统接入

导航默认执行顺序 -100。在其后读取建议，用 `min(speed * deltaTime, RemainingWaypointDistance)` 限制位移；执行前检查 `IsMovementClear` 以及实际身体碰撞。不要另存路径索引、目标重算时钟或第二份权威路径。

复杂运动、冲刺、击退、跳跃、重力及运动优先级由项目决定。需要自定义推进顺序/障碍域时直接使用 NavigationAgent2D API。

局部避让使用整批 `快照 → AvoidanceWorld2D.Solve → 项目检查移动段 → 统一执行`。组件不隐含自动群体求解，不保证战斗站位、拥堵脱困、跨层楼梯或地面存在。

## QuickStart 仅为示例适配器

导入 **Quick Start Navigation 2D** 并打开 `QuickStartNavigation2D.unity`，可以直观看到绕墙移动。运动由 `Samples~/QuickStartNavigation2D/QuickStartExampleMover2D.cs` 执行，类型位于 `Computerzhuxi.Navigation2D.Samples`，所属程序集为 `Computerzhuxi.Navigation2D.QuickStartSample`。

**它不是正式 Runtime/API，也不是包承诺的生产移动能力。** 实际项目应使用自己的移动系统。示例选择 Kinematic、Speed、Collision Margin 与实际身体扫掠，仅用来说明如何消费导航建议，不表示导航对项目运动模型的要求。示例运动器可能在其他身体前停住，不含 ORCA 协调、击退、旋转、外力或脱困能力。

可禁用或移除 QuickStartExampleMover2D：角色停止，但修改 Target 后 Navigator 仍更新只读路径。`QuickStartControls` 只提供显示、按钮及冒烟读数，不实现角色移动。
