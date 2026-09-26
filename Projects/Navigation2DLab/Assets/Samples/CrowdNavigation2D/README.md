# Crowd Navigation 2D

**入口：公开类程序式组合。**此示例演示 Agent 路径建议 → AvoidanceWorld2D 批量速度 → 地图段校验 → 样例自身运动器。Agent 是路径与进度的唯一拥有者；样例拥有每个角色的真实位置、速度及目标。

创建空对象添加 CrowdNavigationDemo；添加正交相机（位置 0,0,-10、Size 5）。运行时按钮切换迎面、停止角色、交叉群体、窄道；蓝色/橙色是移动角色，灰色是停止角色，小方块标记目标。显示最小圆间隙与地图阻止步数。

依次点击 **Head-on**、**Stopped actor**、**8-way crossing**、**Narrow corridor**，再试 **Pause / Resume**。观察两角色绕让与到达、停止身体不被推动、交叉时不穿身，以及窄道不穿墙；窄道不能会车时允许等待。演示只处理圆形代理，不实现战斗站位或强行脱困。正式源码在包 Samples~，Lab 镜像通过哈希验证。
