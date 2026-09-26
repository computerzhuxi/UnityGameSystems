# Inspector Quick Start

**入口：默认的 Inspector 自动导航组件。**导入后打开 `QuickStartNavigation2D.unity` 并运行。场景已序列化身体、目标、障碍和正式 Navigator 与示例适配器，无需另写 Tick。`QuickStartControls` 只负责显示、按钮和冒烟读数；禁用它，角色仍会自动寻路移动。

选中 Navigator，在 Inspector 修改 Target、Body Collider、Obstacle Mask、Cell Size、预算与重算参数；选中 Quick Start Example Mover 2D 修改 Speed。运行中修改于下个固定帧生效。停止清除目标，点击 Follow target again 才恢复。

仅供示例的 QuickStartExampleMover2D 要求同一刚体的 Kinematic Rigidbody2D；导航组件本身也支持子物体独立刚体，不限制项目运动实现。已有运动器只使用导航组件的只读建议，不挂示例运动器。本示例不自动执行群体 ORCA，批量避让见 Crowd Navigation 2D。

人工检查：绕墙到达、暂停/恢复、停止后保持停止、移动目标、移除/恢复障碍、Inspector 修改速度/净空、Gizmo 剩余路径。自定义身体应使用独立角色层，不能包含在障碍层中。

QuickStartExampleMover2D 仅存在于本 Sample/Lab 的独立示例程序集，不是正式 Runtime/API 或生产能力。实际项目使用自己的移动系统。禁用/移除此示例组件后，修改目标仍会更新 Navigator 路径，但不再移动角色。同一角色不要同时挂载并驱动手动 `NavigationAgent2DComponent`。
