using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Computerzhuxi.Navigation2D;
using Computerzhuxi.Navigation2D.Samples;

/// <summary>仅验证导入 QuickStart 的示例适配链，不能将示例运动能力宣称为正式 API。</summary>
public sealed class QuickStartExampleTests
{
    private Scene scene;
    private NavigationNavigator2D navigator;
    private Rigidbody2D body;
    private QuickStartExampleMover2D mover;
    /// <summary>创建独占场景，使用正常物理帧执行组件。</summary>
    [SetUp] public void Setup() { scene = SceneManager.CreateScene("QuickNavigation-" + Guid.NewGuid()); }
    /// <summary>卸载本测试独占场景并销毁全部组件。</summary>
    [UnityTearDown] public IEnumerator Cleanup() { yield return SceneManager.UnloadSceneAsync(scene); }
    /// <summary>建立隔离内容物体。</summary>
    private GameObject Create(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
    /// <summary>模拟 Inspector 写入字段，验证序列化配置生效，无需新增测试专用公共 API。</summary>
    private static void Set(object instance, string name, object value) => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
    /// <summary>装配正式组件，运动和导航仍由自身固定帧运行。</summary>
    private void MakeAgent(bool withMover = true)
    {
        var go = Create("Configured navigator"); go.SetActive(false);
        body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        var shape = go.AddComponent<CircleCollider2D>(); shape.radius = .2f;
        navigator = go.AddComponent<NavigationNavigator2D>(); Set(navigator, "bodyCollider", shape);
        Set(navigator, "obstacleMask", (LayerMask)(1 << 8)); Set(navigator, "cellSize", .2f);
        if (withMover) mover = go.AddComponent<QuickStartExampleMover2D>();
        go.SetActive(true);
    }
    /// <summary>只挂导航也能自动产出只读路径，但绝不移动 Transform 或刚体。</summary>
    [UnityTest] public IEnumerator NavigatorOnly_ProducesPathWithoutMovement()
    {
        MakeAgent(false); navigator.SetDestination(new(2, 0));
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(navigator.HasPath); Assert.That(navigator.CurrentPath.Count, Is.GreaterThan(0));
        Assert.That(navigator.DesiredDirection.x, Is.GreaterThan(0)); Assert.That(body.position, Is.EqualTo(Vector2.zero));
    }
    /// <summary>Inspector 目标驱动真实刚体到达，目标移动后无需调用 Tick 或写移动代码。</summary>
    [UnityTest] public IEnumerator InspectorTarget_MovesAndFollowsChanges()
    {
        MakeAgent(); var target = Create("Target"); target.transform.position = new(2, 0);
        Set(navigator, "target", target.transform);
        for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(body.position, new(2, 0)), Is.LessThan(.12f));
        target.transform.position = new(2, 1);
        for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(body.position, new(2, 1)), Is.LessThan(.12f));
        Assert.That(navigator.State, Is.EqualTo(NavigationAgentState2D.Arrived));
    }
    /// <summary>暂停不被禁用重启解除；Stop 移除跟踪源，恢复也不能重启旧目标。</summary>
    [UnityTest] public IEnumerator PauseDisableStop_PreserveExplicitSemantics()
    {
        MakeAgent(); var target = Create("Target"); target.transform.position = new(4, 0); navigator.SetTarget(target.transform);
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        navigator.Pause(); navigator.enabled = false; navigator.enabled = true;
        yield return new WaitForFixedUpdate(); Vector2 stopped = body.position;
        for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
        Assert.That(body.position, Is.EqualTo(stopped)); Assert.That(navigator.IsPaused);
        navigator.Resume(); for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.That(body.position.x, Is.GreaterThan(stopped.x));
        navigator.Stop(); yield return new WaitForFixedUpdate(); stopped = body.position;
        navigator.Resume(); for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
        Assert.That(navigator.HasDestination, Is.False); Assert.That(body.position, Is.EqualTo(stopped));
    }
    /// <summary>运行时网格/速度/身体尺寸修改立即生效，非法参数恢复后继续原任务。</summary>
    [UnityTest] public IEnumerator LiveInspectorChanges_RebuildSafelyAndRecover()
    {
        MakeAgent(); navigator.SetDestination(new(4, 0));
        yield return new WaitForFixedUpdate();
        Set(navigator, "cellSize", 0f); yield return new WaitForFixedUpdate();
        Assert.That(navigator.ConfigurationError, Is.Not.Null); Assert.That(navigator.DesiredDirection, Is.EqualTo(Vector2.zero));
        Set(navigator, "cellSize", .25f); Set(mover, "speed", 0f);
        navigator.BodyCollider.GetComponent<CircleCollider2D>().radius = .4f;
        for (int i = 0; i < 2; i++) yield return new WaitForFixedUpdate();
        Assert.That(navigator.ConfigurationError, Is.Null); Assert.That(navigator.EffectiveRadius, Is.EqualTo(.42f).Within(.001f));
        Vector2 stopped = body.position; for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(body.position, Is.EqualTo(stopped));
        Set(mover, "speed", 3f); for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.That(body.position.x, Is.GreaterThan(stopped.x + .3f));
    }
    /// <summary>真实薄墙动态出现时不能穿透，移除后自动完成原任务。</summary>
    [UnityTest] public IEnumerator DynamicWall_StopsAndRecoversWithoutPenetration()
    {
        MakeAgent(); navigator.SetDestination(new(3, 0));
        for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
        var wall = Create("Wall"); wall.layer = 8; wall.transform.position = new(1, 0);
        wall.AddComponent<BoxCollider2D>().size = new(.05f, 50); Physics2D.SyncTransforms();
        Set(navigator, "maxExpandedNodes", 32);
        for (int i = 0; i < 20; i++) { yield return new WaitForFixedUpdate(); Assert.That(body.position.x, Is.LessThan(.78f)); }
        wall.SetActive(false); Set(navigator, "maxExpandedNodes", 4096);
        for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(body.position, new(3, 0)), Is.LessThan(.12f));
    }
    /// <summary>示例运动器拒绝 Dynamic 且不改其类型；导航仍独立提供路径。</summary>
    [UnityTest] public IEnumerator DynamicBody_IsNotSilentlyTakenOver()
    {
        MakeAgent(); body.bodyType = RigidbodyType2D.Dynamic; body.gravityScale = 0; navigator.SetDestination(new(2, 0));
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Assert.That(mover.ConfigurationError, Is.Not.Null); Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
        Assert.That(navigator.HasPath); Assert.That(body.position, Is.EqualTo(Vector2.zero));
    }
    /// <summary>目标销毁后停止任务，不沿最后目标继续移动。</summary>
    [UnityTest] public IEnumerator DestroyedTarget_CancelsTask()
    {
        MakeAgent(); var target = Create("Target"); target.transform.position = new(3, 0); navigator.SetTarget(target.transform);
        yield return new WaitForFixedUpdate(); UnityEngine.Object.Destroy(target); yield return null;
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Assert.That(navigator.HasDestination, Is.False); Assert.That(mover.AppliedVelocity, Is.EqualTo(Vector2.zero));
    }

    /// <summary>禁用和移除示例移动器后导航仍持续计算，正式 DLL 不含移动类型。</summary>
    [UnityTest] public IEnumerator ExampleDisabledAndRemoved_NavigationContinues()
    {
        MakeAgent(); navigator.SetDestination(new(2, 0));
        yield return new WaitForFixedUpdate(); mover.enabled = false;
        yield return new WaitForFixedUpdate(); Vector2 stopped = body.position;
        navigator.SetDestination(new(0, 2));
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Assert.That(navigator.HasPath); Assert.That(navigator.DesiredDirection.y, Is.GreaterThan(0));
        Assert.That(body.position, Is.EqualTo(stopped));
        UnityEngine.Object.Destroy(mover); yield return null;
        navigator.SetDestination(new(-2, 0));
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Assert.That(navigator.HasPath); Assert.That(navigator.DesiredDirection.x, Is.LessThan(0));
        Assert.That(body.position, Is.EqualTo(stopped));
        Assert.That(typeof(QuickStartExampleMover2D).Assembly.GetName().Name, Is.EqualTo("Computerzhuxi.Navigation2D.QuickStartSample"));
        foreach (var type in typeof(NavigationNavigator2D).Assembly.GetTypes()) Assert.That(type.Name, Does.Not.Contain("Mover"));
    }
    /// <summary>身体层误入障碍时给出配置诊断，修正 Inspector 后恢复任务。</summary>
    [UnityTest] public IEnumerator SelfLayerMask_IsRejectedAndCanBeCorrected()
    {
        MakeAgent(); navigator.SetDestination(new(2, 0)); Set(navigator, "obstacleMask", (LayerMask)1);
        yield return new WaitForFixedUpdate();
        Assert.That(navigator.ConfigurationError, Does.Contain("Layer")); Assert.That(navigator.HasPath, Is.False);
        Set(navigator, "obstacleMask", (LayerMask)(1 << 8));
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Assert.That(navigator.ConfigurationError, Is.Null); Assert.That(body.position.x, Is.GreaterThan(.1f));
    }
    /// <summary>不属于寻路障碍的实体仍由真实身体扫掠挡住，不能因路径查询通过而穿透。</summary>
    [UnityTest] public IEnumerator PhysicalBodyOutsideObstacleMask_BlocksMovement()
    {
        MakeAgent(); var wall = Create("Physical body"); wall.layer = 9; wall.transform.position = new(.8f, 0);
        wall.AddComponent<BoxCollider2D>().size = new(.05f, 3); Physics2D.SyncTransforms();
        navigator.SetDestination(new(2, 0)); bool blocked = false;
        for (int i = 0; i < 30; i++) { yield return new WaitForFixedUpdate(); blocked |= mover.IsBlocked; }
        Assert.That(blocked); Assert.That(body.position.x, Is.LessThan(.6f)); Assert.That(navigator.LastResult.Value.Succeeded);
        mover.enabled = false; Vector2 stopped = body.position;
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Assert.That(body.position, Is.EqualTo(stopped));
    }
    /// <summary>切换独立物理场景后查询新障碍域，旧路径不能继续使用。</summary>
    [UnityTest] public IEnumerator PhysicsSceneMigration_InvalidatesOldDomain()
    {
        MakeAgent(false); navigator.SetDestination(new(2, 0)); yield return new WaitForFixedUpdate();
        Assert.That(navigator.HasPath);
        Scene other = SceneManager.CreateScene("QuickNavigationDomain-" + Guid.NewGuid(), new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        try
        {
            SceneManager.MoveGameObjectToScene(navigator.gameObject, other);
            var obstacle = new GameObject("New scene obstacle"); SceneManager.MoveGameObjectToScene(obstacle, other);
            obstacle.layer = 8; obstacle.AddComponent<BoxCollider2D>().size = Vector2.one;
            Physics2D.SyncTransforms(); other.GetPhysicsScene2D().Simulate(.02f);
            yield return new WaitForFixedUpdate();
            Assert.That(navigator.HasPath, Is.False); Assert.That(navigator.LastResult.Value.Status, Is.EqualTo(PathStatus.StartBlocked));
        }
        finally
        {
            // 测试只拥有这个独占场景；异步卸载由 Unity 清理其全部对象。
            SceneManager.UnloadSceneAsync(other);
        }
    }
}
