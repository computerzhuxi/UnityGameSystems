# Agent Navigation 2D

在空二维场景添加正交 Camera 和 `AgentNavigationDemo`，进入 Play。
样例先 UpdateDestination，再 Tick，最后按 DesiredDirection 移动，用 RemainingWaypointDistance 限幅。
代理不修改 Transform、刚体或速度；也可完全不用组件，直接构造 NavigationAgent2D。

按钮：Reset 重启任务；Moving target 开关目标移动；Pause / Resume 冻结与恢复；Toggle obstacle 修改障碍并显式失效路径。
黄色 Gizmo 应绕过灰墙；青色角色追随移动目标时不应反复后退。暂停期间位置及 QueryCount 不变。

直接查询方式见 Basic Navigation 2D Sample。不要在同一对象上并行运行两个移动执行器。
