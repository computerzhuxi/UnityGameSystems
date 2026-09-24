using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Computerzhuxi.Navigation2D;

/// <summary>验证代理在真实物理场景中的绕障、组件生命周期和多角色查询预算。</summary>
public sealed class AgentPhysicsTests
{
    private Scene scene;
    /// <summary>创建隔离二维物理场景。</summary>
    [SetUp] public void Setup() => scene = SceneManager.CreateScene("AgentPhysics-" + Guid.NewGuid(), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
    /// <summary>卸载所有独占运行期测试对象。</summary>
    [UnityTearDown] public IEnumerator Cleanup() { yield return SceneManager.UnloadSceneAsync(scene); }
    /// <summary>建立属于隔离场景的物体。</summary>
    private GameObject Create(string label) { var go = new GameObject(label); SceneManager.MoveGameObjectToScene(go, scene); return go; }
    /// <summary>按真实圆形净空跟随薄墙转角，不同半径都必须安全抵达。</summary>
    [UnityTest] public IEnumerator CornersAndRadii_ReachWithoutUnsafeSegments()
    {
        var wall = Create("Thin wall"); wall.layer = 8;
        wall.AddComponent<BoxCollider2D>().size = new(.03f, 1);
        yield return null; Physics2D.SyncTransforms();
        var source = new PhysicsTraversalSource2D(scene.GetPhysicsScene2D(), 1 << 8);
        foreach (float radius in new[] { .02f, .2f })
        {
            var agent = new NavigationAgent2D(new(Vector2.zero, .1f), new(radius), new(.15f, .1f, .1f, .25f, 3));
            Vector2 position = new(-1, 0); agent.SetDestination(new(1, 0));
            for (int i = 0; i < 500 && agent.State != NavigationAgentState2D.Arrived; i++)
            {
                agent.Tick(position, .02f, source);
                if (agent.HasPath) Assert.That(source.IsSegmentClear(position, agent.CurrentPath[agent.CurrentPathIndex], radius));
                Vector2 next = position + agent.DesiredDirection * Mathf.Min(.02f, agent.RemainingWaypointDistance);
                Assert.That(source.IsSegmentClear(position, next, radius), $"radius={radius:R} step={i} from=({position.x:R},{position.y:R}) to=({next.x:R},{next.y:R}) waypoint={(agent.HasPath ? agent.CurrentPath[agent.CurrentPathIndex] : Vector2.zero)} startClear={source.IsPositionClear(position, radius)} endClear={source.IsPositionClear(next, radius)}"); position = next;
            }
            Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.Arrived), "radius " + radius);
        }
    }
    /// <summary>禁用组件停止建议，重启使旧路径失效；组件自身不会写入 Transform。</summary>
    [UnityTest] public IEnumerator Component_DisableResumeAndNoImplicitMovement()
    {
        var go = Create("Component agent"); var component = go.AddComponent<NavigationAgent2DComponent>();
        component.ConfigurePhysics(1 << 8); component.Agent.SetDestination(Vector2.right * 3);
        component.Tick(Vector2.zero, .02f); Assert.That(component.Agent.HasPath);
        Vector3 original = go.transform.position; yield return null; Assert.That(go.transform.position, Is.EqualTo(original));
        component.enabled = false; Assert.That(component.Agent.IsPaused); Assert.That(component.Agent.DesiredDirection, Is.EqualTo(Vector2.zero));
        int count = component.Agent.QueryCount; component.Tick(Vector2.zero, 10); Assert.That(component.Agent.QueryCount, Is.EqualTo(count));
        component.enabled = true; Assert.That(component.Agent.HasPath, Is.False);
        component.Tick(Vector2.zero, .02f); Assert.That(component.Agent.QueryCount, Is.EqualTo(count + 1));
    }
    /// <summary>实际刚体连续消费移动建议与目标更新时保持前进，不因重算产生方向反转。</summary>
    [UnityTest] public IEnumerator MovingTarget_RigidbodyDoesNotReverse()
    {
        var go = Create("Kinematic motor"); var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        body.position = new(.2f, 0); var source = new PhysicsTraversalSource2D(scene.GetPhysicsScene2D(), 0);
        var agent = new NavigationAgent2D(new(Vector2.zero, .5f), new(.1f), new(.05f, .1f, .1f));
        for (int i = 0; i < 120; i++)
        {
            Vector2 before = body.position; agent.UpdateDestination(before + Vector2.right * 3); agent.Tick(before, .02f, source);
            Assert.That(agent.DesiredDirection.x, Is.GreaterThan(0));
            body.MovePosition(before + agent.DesiredDirection * Mathf.Min(.04f, agent.RemainingWaypointDistance));
            scene.GetPhysicsScene2D().Simulate(.02f); Assert.That(body.position.x, Is.GreaterThan(before.x));
        }
        yield return null;
    }
    /// <summary>记录多角色真实物理查询、跟随和请求节流的预热性能，不作跨机器耗时承诺。</summary>
    [UnityTest] public IEnumerator MultipleAgents_ReportWarmBudgetAndAllocation()
    {
        var source = new PhysicsTraversalSource2D(scene.GetPhysicsScene2D(), 0);
        var agents = new NavigationAgent2D[16];
        for (int i = 0; i < agents.Length; i++)
        {
            agents[i] = new NavigationAgent2D(new(Vector2.zero, .5f), new(.1f, 4096), new(.05f, .1f, .1f));
            agents[i].SetDestination(Vector2.right * 3); agents[i].Tick(Vector2.zero, .02f, source);
        }
        var watch = new System.Diagnostics.Stopwatch();
        // 预热查询集合后再测量，数据包括 Agent 输出与真实 Physics2D 验证。
        for (int f = 0; f < 100; f++) foreach (var agent in agents) { agent.UpdateDestination(new(3 + f % 2, 0)); agent.Tick(Vector2.zero, .02f, source); }
        long before = GC.GetAllocatedBytesForCurrentThread(); watch.Start();
        for (int f = 0; f < 100; f++) foreach (var agent in agents) { agent.UpdateDestination(new(3 + f % 2, 0)); agent.Tick(Vector2.zero, .02f, source); }
        watch.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        int queries = 0; foreach (var agent in agents) { queries += agent.QueryCount; Assert.That(agent.LastResult.Value.ExpandedNodes, Is.LessThanOrEqualTo(4096)); }
        Assert.That(allocated, Is.Zero); Assert.That(queries, Is.LessThan(16 * 100));
        Debug.Log($"NAVIGATION_AGENT_PERF agents=16 measured_frames=100 elapsed_ms={watch.Elapsed.TotalMilliseconds:F4} allocated={allocated} cumulative_queries={queries}");
        yield return null;
    }
}
