# Navigation 2D

二维同步网格 A* 路径查询与 Physics2D 圆形净空检查。**0.2.0** 增加 Agent、局部避让及 Inspector 导航组件；正式 Runtime 不执行移动。版本以 `package.json` 和固定 Git 标签为准。

## 选择接入方式

| 场景 | 使用方式 |
|---|---|
| 挂组件直接使用 | NavigationNavigator2D 配置身体、障碍、目标并输出路径，由项目移动系统执行 |
| 已有运动器 | 读取导航组件的只读路径/建议，或通过公开查询、Agent、Avoidance API 自行组合 |

全部快速使用参数在 Inspector 配置，运行中修改最迟下个固定帧生效。正式 Runtime 只负责导航，不提供移动组件；项目移动系统负责实际位置和速度。完整步骤见 [Inspector 使用指南](Documentation~/UnityGuide.md)。

导入 **Quick Start Navigation 2D** 后直接打开 `QuickStartNavigation2D.unity` 即可观看移动演示；QuickStartExampleMover2D 仅是 Sample/Lab 适配器，不是正式 API 或生产移动能力，实际项目使用自己的运动系统。下面的固定 0.2.0 标签包含这些功能；开发本包时仍使用本地唯一源码。

## 安装与最小接入

消费项目在 Package Manager 选择 **Add package from Git URL**，使用固定标签：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.navigation2d#navigation2d-v0.2.0
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

直接查询路线由调用方负责目的地、重算频率、进度和到达容差；可选 Agent 路线由包唯一持有这些状态。两种路线都不控制角色实际运动。输出路径包含安全连接所需的网格中心，最后一点为精确终点；不要把路径点随意连成未经扫掠检查的新捷径。

## 指南

- [Inspector 使用与运动器接入](Documentation~/UnityGuide.md)
- [API 与失败语义](Documentation~/API.md)
- [架构与边界](Documentation~/Architecture.md)
- [维护与故障排查](Documentation~/Maintenance.md)
- [第三方调研与来源](Documentation~/Research.md)
- [版本变化](CHANGELOG.md)

Package Manager 中导入 **Basic Navigation 2D** Sample，把 `NavigationDemo` 添加到空场景物体，并放置正交相机（位置 0,0,-10，Size 5）。运行后显示墙与移动标记；Scene 视图开启 Gizmos 可见路径与净空。按钮支持重置和开关障碍。

## 限制

同步主线程查询；圆形占位、四/八方向、最近中心网格。未提供自动脱困、路径平滑、异步或跨层图。没有碰撞体不等于有地面：地图洞口和边缘必须由障碍源明确限制。导航不替代物理碰撞和移动控制。

## 可选 Agent 路线（0.2.0）

```csharp
var agent = new NavigationAgent2D(
    new GridSettings2D(Vector2.zero, .5f), new PathOptions2D(.2f, 4096),
    new AgentSettings2D(.08f, .2f, repathInterval: .1f, failureRetryInterval: .25f));
agent.SetDestination(destination);
// 每帧先更新目标，再提供真实位置、时间与稳定障碍源。
agent.UpdateDestination(latestDestination);
agent.Tick(position, deltaTime, obstacles, domainRevision, domainValid: true);
// 运动由调用方执行；限制本步距离以免越过拐点。
position += agent.DesiredDirection * Mathf.Min(speed * deltaTime, agent.RemainingWaypointDistance);
```

`CurrentPath` 是固定只读视图；不要另存索引或时钟。位置、速度、AI 与层级依旧属于消费者。
可选 `NavigationAgent2DComponent` 提供 Inspector 和 Physics2D 入口；显式调用 `Tick(position, deltaTime)`，不会自动 Update 或写 Transform。程序配置掩码调用 `ConfigurePhysics(mask)`，清除原任务；Inspector 运行期变化后需显式 `Reinitialize()`。

Package Manager 导入 **Agent Navigation 2D**，添加 `AgentNavigationDemo` 与正交相机。示例演示移动目标、暂停、封路、恢复和 Gizmos。生产消费项目使用上方固定 Git 标签；包开发与 Lab 使用本地唯一源码。

## 多角色避让（0.2.0）

可选 AvoidanceWorld2D 接收整批圆形角色快照，只计算建议速度；角色位置、动作、空间层和战斗目标由项目拥有。使用 Agent 时可启用安全前视，避免追着被邻居占据的中间路径点。

导入 **Crowd Navigation 2D** Sample 可测试迎面、停止角色、八人交叉与窄道。窄道可能等待，多个角色追同一个终点可能拥堵；这不替代战斗站位或自动脱困。接口与数值/预算边界见 Documentation~/API.md，许可证见 Third Party Notices.md。
