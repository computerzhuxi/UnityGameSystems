# Basic Navigation 2D

**入口：直接查询。**将 `NavigationDemo` 加入空二维场景物体，添加正交相机（0,0,-10；Size 5），进入 Play。示例用 `GridPathfinder2D.FindPath` 写入自己持有的列表，自行维护进度、重算和标记运动；不依赖 Agent 或导航组件。

开启 Scene Gizmos 观察黄色路径与圆形净空。点击 **Reset / Repath** 重新查询，点击 **Toggle obstacle** 开关中间墙；角色应绕墙并在障碍变化后得到新路径。若采用这种入口，调用方须验证移动段、处理真实身体碰撞及目标变化。

希望由包持有任务与路径时，改看 **Agent Navigation 2D** 或 Inspector **Quick Start Navigation 2D**。同一角色不要另外驱动一套 Agent 路径状态。正式源码在本 Sample，Navigation2DLab 的同名文件是受校验镜像。
