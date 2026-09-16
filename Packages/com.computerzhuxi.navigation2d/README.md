# Navigation 2D

二维同步网格 A* 路径查询与 Physics2D 圆形净空检查。探索期版本 0.1.0，版本事实见 `package.json`。

## 安装与最小接入

消费项目在 Package Manager 选择 **Add package from Git URL**，使用固定标签：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.navigation2d#navigation2d-v0.1.0
```

开发本包时使用仓库中的 Navigation2DLab，通过本地路径引用唯一包源码。

```csharp
using System.Collections.Generic;
using Computerzhuxi.Navigation2D;
using UnityEngine;
using UnityEngine.SceneManagement;

var finder = new GridPathfinder2D();
var path = new List<Vector2>(); // 消费者唯一拥有的输出
var obstacles = new PhysicsTraversalSource2D(
    gameObject.scene.GetPhysicsScene2D(), obstacleMask, includeTriggers: false);
var result = finder.FindPath(start, destination,
    new GridSettings2D(Vector2.zero, 0.5f),
    new PathOptions2D(0.3f, 4096), obstacles, path);
if (result.Succeeded) { /* 消费者跟随 path；移动前检查下一段 */ }
```

包只计算路径，不控制角色。调用方负责目的地、重算频率、进度、到达容差和运动。输出路径包含安全连接所需的网格中心，最后一点为精确终点；不要把路径点随意连成未经扫掠检查的新捷径。

## 指南

- [API 与失败语义](Documentation~/API.md)
- [架构与边界](Documentation~/Architecture.md)
- [维护与故障排查](Documentation~/Maintenance.md)
- [第三方调研与来源](Documentation~/Research.md)
- [版本变化](CHANGELOG.md)

Package Manager 中导入 **Basic Navigation 2D** Sample，把 `NavigationDemo` 添加到空场景物体，并放置正交相机（位置 0,0,-10，Size 5）。运行后显示墙与移动标记；Scene 视图开启 Gizmos 可见路径与净空。按钮支持重置和开关障碍。

## 限制

同步主线程查询；圆形占位、四/八方向、最近中心网格。未提供自动脱困、路径平滑、异步、群体避让或跨层图。没有碰撞体不等于有地面：地图洞口和边缘必须由障碍源明确限制。导航不替代物理碰撞和移动控制。
