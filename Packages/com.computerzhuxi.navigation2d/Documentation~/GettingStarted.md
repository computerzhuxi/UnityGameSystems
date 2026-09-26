# Getting Started：两种导航入口

本页描述当前仓库的本地工作树。子刚体身体与手动组件跨物理场景的修复属于 **Unreleased**，尚未包含在固定 `navigation2d-v0.2.0` 标签中。

正式包只负责导航查询、任务与移动建议。项目拥有目标决策、实际位置、速度、物理运动以及执行后的反馈。同一角色只选一个导航任务拥有者；不要同时驱动 `NavigationNavigator2D` 与 `NavigationAgent2DComponent`，也不要再为它们保存第二份路径索引或重算计时。

## 入口一：Inspector 组件装配

1. 在角色上添加 `NavigationNavigator2D`；设置 **Body Collider**、**Obstacle Mask**、格距与容差。身体自身 Layer 不可放入障碍掩码。Body Collider 可位于角色或子物体：有附属 Rigidbody2D 时，Navigator 使用该刚体的物理位置作为导航圆心；没有附属刚体时使用 Navigator 所在物体的位置。偏移或非圆身体采用保守包围圆。
2. 在 **Target** 放入目标 Transform，或从项目代码调用 `SetDestination(Vector2)`。Navigator 每个固定帧自行推进唯一 Agent，路径和状态由其内部 Agent 持有。
3. 在项目运动器的固定帧读取 `DesiredDirection` 与 `RemainingWaypointDistance`；移动前检查 `IsMovementClear`，并按实际身体和运动模式完成碰撞控制。下面仅演示建议消费，不替代项目的运动规则：

```csharp
using Computerzhuxi.Navigation2D;
using UnityEngine;

[DefaultExecutionOrder(-50)] // Navigator 默认是 -100，先让它更新导航建议
public sealed class ProjectNavigationMotor : MonoBehaviour
{
    [SerializeField] private NavigationNavigator2D navigator;
    [SerializeField] private Rigidbody2D body;
    [SerializeField, Min(0)] private float speed = 2;

    /// <summary>在导航组件更新后消费本帧建议，并由项目运动器提交位移。</summary>
    private void FixedUpdate()
    {
        if (navigator == null || body == null || navigator.IsPaused) return;
        float distance = Mathf.Min(speed * Time.fixedDeltaTime, navigator.RemainingWaypointDistance);
        Vector2 next = body.position + navigator.DesiredDirection * distance;
        if (navigator.DesiredDirection == Vector2.zero || !navigator.IsMovementClear(body.position, next)) return;
        // 生产运动器还需按自身碰撞体、运动优先级和刚体类型检查并提交这一步。
        body.MovePosition(next);
    }
}
```

此示例的 MovePosition 适合由项目明确接管的刚体运动。Navigator 不设置刚体类型，也不停止外部已有速度；暂停、停止或禁用导航时，运动器负责停止自己拥有的运动。可运行的 Kinematic 演示见 **Quick Start Navigation 2D** Sample；其 `QuickStartExampleMover2D` 不属于正式 Runtime。

## 入口二：公开类与接口自行组合

已有 AI、运动器、跨层地图或自定义更新顺序时，直接构造 `NavigationAgent2D`，给它真实位置、稳定障碍源及域版本。下面以固定目标与 Physics2D 为例：

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
    private NavigationAgent2D agent;
    private PhysicsTraversalSource2D obstacles;
    private const float Radius = .2f;

    /// <summary>为当前二维物理场景创建障碍源和单角色导航任务。</summary>
    private void Awake()
    {
        obstacles = new PhysicsTraversalSource2D(gameObject.scene.GetPhysicsScene2D(), obstacleMask);
        agent = new NavigationAgent2D(
            new GridSettings2D(Vector2.zero, .5f),
            new PathOptions2D(Radius, 4096),
            new AgentSettings2D(.08f, .2f, .1f, .25f));
    }

    /// <summary>提交真实位置与移动目标，并在验证当前移动段后执行项目运动。</summary>
    private void FixedUpdate()
    {
        if (body == null || target == null) { agent.Stop(); return; }
        agent.UpdateDestination(target.position);
        agent.Tick(body.position, Time.fixedDeltaTime, obstacles);
        float distance = Mathf.Min(speed * Time.fixedDeltaTime, agent.RemainingWaypointDistance);
        Vector2 next = body.position + agent.DesiredDirection * distance;
        if (agent.DesiredDirection == Vector2.zero || !obstacles.IsSegmentClear(body.position, next, Radius)) return;
        // 项目先核查实际身体，再按自己的运动模型执行。
        body.MovePosition(next);
    }
}
```

此例假定物理场景和障碍掩码固定。若场景、层级或障碍域变化，项目应重建 `PhysicsTraversalSource2D`，在 `Tick` 提交新的 source、revision 和 domainValid，或调用 `InvalidatePath()`；直接移动 Transform 后须处理 Physics2D 同步。移动目标用 `UpdateDestination`，明确替换任务才用 `SetDestination`。

若只需一次查询，可改用 `GridPathfinder2D.FindPath(start, destination, grid, options, source, outputList)`，此时调用方拥有输出列表、路径索引和重算策略。若需群体协调，在所有 Agent Tick 后采集同一时刻的真实位置/速度与期望速度，批量调用 `AvoidanceWorld2D.Solve`，再验证并执行每个建议位移。见 **Basic Navigation 2D** 与 **Crowd Navigation 2D** Samples。

旧 `NavigationAgent2DComponent` 继续支持 Inspector 配置与显式 `Tick(position, deltaTime)`。它适合保留已有手动调度；禁用期间不推进，跨 PhysicsScene 后在下一次 Tick 重建物理源、丢弃旧路径并保留目的地。它和 Navigator 都使用公开 Agent 的规则，但分别拥有自己的单角色任务，不能在同一角色并行使用。
