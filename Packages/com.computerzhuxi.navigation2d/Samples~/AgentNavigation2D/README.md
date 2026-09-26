# Agent Navigation 2D

**入口：保留的手动 Tick 组件。**在空二维场景添加正交 Camera 和 `AgentNavigationDemo`，进入 Play；或打开 Navigation2DLab 的 `Assets/NavigationAgentLab.unity`。
样例先 UpdateDestination，再 Tick，最后按 DesiredDirection 移动，用 RemainingWaypointDistance 限幅。
组件只配置 Physics2D 来源，代理拥有目标、路径与进度；示例自己移动 Transform。代理不修改 Transform、刚体或速度。也可不用组件，直接构造公开 `NavigationAgent2D` 与自己的障碍域。

按钮：Reset 重启任务；Moving target 开关目标移动；Pause / Resume 冻结与恢复；Toggle obstacle 修改障碍并显式失效路径。
黄色 Gizmo 应绕过灰墙；青色角色追随移动目标时不应反复后退。暂停期间位置及 QueryCount 不变。

手动组件跨独立 PhysicsScene 后在下一次 Tick 改用新物理源、丢弃旧路径并保留任务。默认自动 Inspector 入口见 Quick Start；直接查询见 Basic。不要在同一角色上并行驱动本组件与 Navigator，也不要并行维护两个运动执行器。
