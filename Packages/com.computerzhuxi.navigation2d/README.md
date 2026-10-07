# 🧭 Navigation 2D

一个可复用的 Unity 2D 导航包：同步网格 A* 路径查询、单角色导航代理、可选批量局部避让，以及 Physics2D 净空检查。正式 Runtime **只计算路径与移动建议**；目标决策、实际位置和速度、碰撞后的运动结果由项目负责。

## 📦 安装

要求 **Unity 6000.3** 或更高版本，以及 Physics2D 模块。通过 Unity Package Manager 添加 Git URL，安装已发布的 **0.2.0**：

```text
https://github.com/computerzhuxi/UnityGameSystems.git?path=/Packages/com.computerzhuxi.navigation2d#navigation2d-v0.2.0
```

本地工作树的 [Unreleased 修复](CHANGELOG.md) 尚未包含在该固定标签中。需要验证这些修复时，在仓库的 `Projects/Navigation2DLab` 使用本地 UPM 包路径。

## 🚀 快速开始

同一角色只保留一个导航任务和路径进度的拥有者。选择与现有项目结构相符的入口：

| 入口 | 适用场景 | 谁持有任务与路径 |
| --- | --- | --- |
| `NavigationNavigator2D` | Inspector 装配，固定帧自动更新导航 | 组件内部唯一的 `NavigationAgent2D` |
| `NavigationAgent2D` | 自有运动器、空间域或更新顺序 | 项目创建并保存的 Agent |
| `GridPathfinder2D.FindPath` | 只需一次查询或自行管理跟随 | 调用方的路径列表和进度 |
| `NavigationAgent2DComponent` | 保留已有手动 `Tick` 调度 | 组件内部唯一的 Agent |

不要在同一角色上同时驱动 `NavigationNavigator2D` 和 `NavigationAgent2DComponent`，也不要为 Agent 另存第二份路径索引或重算计时。

### 用 Inspector 装配

1. 在角色上添加 **Navigation Navigator 2D**。将非 Trigger、已启用的身体 `Collider2D` 指定给 **Body Collider**；未指定时使用 **Radius**。**Clearance** 会额外扩大查询半径。
2. 给地图障碍分配 Layer，并选入 **Obstacle Mask**。身体自身的 Layer 不能选入。设置 **Cell Size**、**Max Expanded Nodes** 和四/八方向；预算耗尽只表示本次搜索未完成。
3. 将目标 `Transform` 指定给 **Target**，或在代码中调用 `SetDestination(Vector2)` 设置固定位置。组件在 `FixedUpdate` 自动推进 Agent，运行时可在 Inspector 查看状态、最后结果和路径 Gizmo。
4. 让项目运动器在导航组件之后读取建议，检查本步净空并执行运动。**只添加导航组件不会移动角色。**

例如，项目明确由 Kinematic `Rigidbody2D` 执行移动时，可使用下列最小运动器。将代码保存为 `Assets/ProjectNavigationMotor.cs`，挂到角色上，将同一角色的 Navigator 和 Kinematic Rigidbody2D 分别拖入 **Navigator**、**Body**，并启用刚体的 **Simulated**。它只演示消费建议；实际项目仍需按自身碰撞体、运动优先级和刚体模式处理运动：

```csharp
using Computerzhuxi.Navigation2D;
using UnityEngine;

[DefaultExecutionOrder(-50)] // Navigator 的执行顺序为 -100
public sealed class ProjectNavigationMotor : MonoBehaviour
{
    [SerializeField] private NavigationNavigator2D navigator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField, Min(0)] private float speed = 2;

    /// <summary>在导航更新后读取建议，并验证本步路径再提交项目运动。</summary>
    private void FixedUpdate()
    {
        if (navigator == null || body == null || navigator.IsPaused) return;
        Vector2 direction = navigator.DesiredDirection;
        if (direction == Vector2.zero) return;

        float distance = Mathf.Min(speed * Time.fixedDeltaTime, navigator.RemainingWaypointDistance);
        Vector2 next = body.position + direction * distance;
        // 路径点之间仍可能出现新障碍，实际运动前必须检查当前移动段。
        if (navigator.IsMovementClear(body.position, next)) body.MovePosition(next);
    }
}
```

Navigator 不设置刚体类型或速度，也不会停止外部运动器已有的速度。暂停、停止或禁用导航时，项目运动器需要处理自己拥有的运动。`Body Collider` 有附属 `Rigidbody2D` 时，本地工作树以该刚体的物理位置作为导航圆心，包括子物体的独立刚体；否则以 Navigator 所在物体位置为圆心。偏移或非圆身体使用保守包围圆，可能拒绝狭窄通道。**子物体独立刚体支持属于 Unreleased，固定 0.2.0 标签不能据此使用。**

### 在现有运动系统中使用 Agent

项目已有自己的固定帧、障碍域和运动器时，创建 `NavigationAgent2D`，每步提供真实位置、时间与稳定的 `ITraversalSource2D`。下例保存为 `Assets/ProjectAgentDriver.cs` 并挂到角色上，在 Inspector 绑定 **Body**、**Target** 和 **Obstacle Mask**；Body 使用启用 Simulated 的 Kinematic Rigidbody2D，角色自身层排除在障碍掩码外。示例查询半径为 0.2 世界单位，必须覆盖身体相对刚体位置的完整占位。若身体更大或有偏移，应调整 `Radius`。例如：

```csharp
using Computerzhuxi.Navigation2D;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ProjectAgentDriver : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private Transform target;
    [SerializeField, Min(0)] private float speed = 2;

    private const float Radius = .2f;
    private NavigationAgent2D agent;
    private PhysicsTraversalSource2D obstacles;

    /// <summary>为当前二维物理场景创建障碍源与单角色导航状态。</summary>
    private void Awake()
    {
        obstacles = new PhysicsTraversalSource2D(gameObject.scene.GetPhysicsScene2D(), obstacleMask);
        agent = new NavigationAgent2D(
            new GridSettings2D(Vector2.zero, .5f),
            new PathOptions2D(Radius, 4096),
            new AgentSettings2D(.08f, .2f, .1f, .25f));
    }

    /// <summary>更新移动目标，用身体真实位置取得建议，并校验本步位移。</summary>
    private void FixedUpdate()
    {
        if (body == null || target == null) { agent.Stop(); return; }
        agent.UpdateDestination(target.position);
        agent.Tick(body.position, Time.fixedDeltaTime, obstacles);

        Vector2 direction = agent.DesiredDirection;
        if (direction == Vector2.zero) return;
        float distance = Mathf.Min(speed * Time.fixedDeltaTime, agent.RemainingWaypointDistance);
        Vector2 next = body.position + direction * distance;
        // 项目必须验证并执行实际位移；此处仅适用于项目已选定的 Kinematic 刚体运动。
        if (obstacles.IsSegmentClear(body.position, next, Radius)) body.MovePosition(next);
    }
}
```

此例假定物理场景和障碍掩码固定。物理场景或障碍域变化时，项目应重建 `PhysicsTraversalSource2D`，并在下一次 `Tick` 提交新来源；空间层级可用性和版本分别通过 `domainValid`、`revision` 传入。瞬移或已知地图变化调用 `InvalidatePath()`。移动目标用 `UpdateDestination`，明确替换任务才用 `SetDestination`；每帧调用后者会不断丢弃路径。

只需查询时，可使用 `GridPathfinder2D.FindPath(start, destination, grid, options, source, outputList)`。调用方负责保存输出列表、跟随索引和重新查询时机；失败时列表为空。`NavigationAgent2DComponent` 则保留 Inspector 配置和显式 `Tick(position, deltaTime)` 入口，适合已有手动调度；它不会自行移动。跨独立 PhysicsScene 后自动重建来源并保留任务的修复属于 **Unreleased**。

## 🎮 常用操作与诊断

`NavigationNavigator2D` 提供以下命令和只读结果；直接使用 Agent 时，除 `SetTarget` 和 `IsMovementClear` 外也有对应的任务与建议 API。

| 操作 | 用途 |
| --- | --- |
| `SetDestination(Vector2)` | 设置固定目的地；Navigator 同时解除 Transform 跟踪，Agent 将其视为新任务 |
| `SetTarget(Transform)` | Navigator 开始跟踪目标；传入 `null` 停止 |
| `Stop()` | 清除任务、路径与跟踪目标，不会在下一帧自动重启 |
| `Pause()` / `Resume()` | 保留任务、暂停或恢复建议；外部运动器的速度由项目处理 |
| `InvalidatePath()` | 传送或已知地图变化后丢弃旧路径并重新查询 |
| `IsMovementClear(start, end)` | Navigator 检查本步圆形占位的整段净空，不执行移动 |

读取 `State`、`IsPaused`、`DesiredDirection`、`RemainingWaypointDistance`、`CurrentPath`、`CurrentPathIndex` 和 `LastResult` 可了解本步状态。`LastResult` 可能为空；查询失败时查看 `LastResult.Value.Status`。Navigator 还提供 `Position`、`EffectiveRadius`、`ConfigurationError` 和 `QueryCount`。运行中修改 Inspector 查询配置最迟在下一个固定帧生效；非法配置会清除建议，修复后可恢复。

包不提供导航事件。需要响应状态变化时，项目在自身更新循环比较 `State` 或 `LastResult` 并发布自己的事件；不要在回调中重入同一寻路器或 Agent。

## ⚠️ 局部避让与使用规则

`AvoidanceWorld2D` 是可选的批量速度建议器。每步先让各 Agent `Tick`，再从**同一时刻**的真实位置、实际速度与期望速度建立 `AvoidanceAgent2D` 快照，调用 `Solve(inputs, deltaTime, outputs)`，最后对所有建议位移检查环境并统一执行。`Group` 隔离互不影响的空间层，`Id` 在同批中唯一；停止或由动作控制的身体可设为 `Locked`。输出的 `Velocity` 是建议，`TruncatedNeighbors` 或 `Infeasible` 表示需要项目决定降级方式。包不保证拥堵脱困或战斗站位，详细数值与失败契约见 [API 契约](Documentation~/API.md)。

1. **路径与运动分工**：包不写 Transform、Rigidbody2D 位置或速度。项目需限制本步距离、检查实际身体与移动段，并把真实结果作为下一步输入。
2. **障碍与物理同步**：障碍源按当前 PhysicsScene2D、LayerMask 和 Trigger 策略查询；身体层不能成为自身障碍。刚改 Transform 或 Tilemap 后，调用方要确保 Physics2D 已同步。
3. **边界与预算**：网格没有内建地图边界，也不验证地面存在；洞口和边界由障碍源限制。`BudgetExceeded` 只表示节点预算耗尽，不能当作“已证明不可达”。
4. **任务状态**：移动目标更新会按阈值和重算间隔节流；路径封闭时建议立即停止，随后按规则重试。到达、暂停和域失效的细节见 [API 契约](Documentation~/API.md)。
5. **能力边界**：包不提供异步、3D、跨层楼梯规划、重力、动作优先级或自动地形平滑。项目负责正式空间层与跨层路线。

## 🎮 示例

可在 Package Manager 中导入以下 Samples：

| Sample | 演示内容 |
| --- | --- |
| [Quick Start Navigation 2D](Samples~/QuickStartNavigation2D/README.md) | 已配置的 Navigator、场景与仅供演示的 Kinematic 运动器；导入后打开 `QuickStartNavigation2D.unity` |
| [Basic Navigation 2D](Samples~/BasicNavigation2D/README.md) | 空二维场景挂载导入的 `NavigationDemo.cs`，添加正交相机；直接查询并自行管理路径与跟随进度 |
| [Agent Navigation 2D](Samples~/AgentNavigation2D/README.md) | 空二维场景挂载导入的 `AgentNavigationDemo.cs`，添加正交相机；手动 `Tick` 组件、移动目标、暂停与动态障碍 |
| [Crowd Navigation 2D](Samples~/CrowdNavigation2D/README.md) | 空二维场景挂载导入的 `CrowdNavigationDemo.cs`，添加正交相机；Agent 与批量避让的程序式组合 |

`QuickStartExampleMover2D` 仅在 Sample 程序集，不属于正式 Runtime。各 Sample 的操作与预期表现见其目录中的 README；仓库 `Projects/Navigation2DLab` 的导入资产是验证镜像。

## 📝 更多资料

- [API 契约](Documentation~/API.md)：失败状态、预算、重入、路径/Agent 状态及避让数值边界。
- [CHANGELOG](CHANGELOG.md)：已发布版本与本地 Unreleased 修复。
- [Third Party Notices](Third%20Party%20Notices.md)：第三方版权、许可与修改说明。
