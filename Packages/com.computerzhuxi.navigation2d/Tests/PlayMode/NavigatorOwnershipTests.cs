using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Computerzhuxi.Navigation2D;

/// <summary>只引用正式 Runtime，证明导航不依赖任何示例或实际移动执行器。</summary>
public sealed class NavigatorOwnershipTests
{
    private Scene scene;
    /// <summary>为导航所有权测试建立独占物理内容。</summary>
    [SetUp] public void Setup() { scene = SceneManager.CreateScene("NavigatorOwnership-" + Guid.NewGuid()); }
    /// <summary>释放本测试场景，不影响其他工程资产。</summary>
    [UnityTearDown] public IEnumerator Cleanup() { yield return SceneManager.UnloadSceneAsync(scene); }
    /// <summary>创建没有运动器的真实导航组件，身体类型由调用方决定。</summary>
    private NavigationNavigator2D Create(RigidbodyType2D type, out Rigidbody2D body)
    {
        var go = new GameObject("Navigation only"); SceneManager.MoveGameObjectToScene(go, scene);
        body = go.AddComponent<Rigidbody2D>(); body.bodyType = type; body.gravityScale = 0;
        var shape = go.AddComponent<CircleCollider2D>(); shape.radius = .2f;
        var navigator = go.AddComponent<NavigationNavigator2D>();
        typeof(NavigationNavigator2D).GetField("bodyCollider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(navigator, shape);
        return navigator;
    }
    /// <summary>反射实际 Runtime 程序集类型及依赖，防止示例运动能力回流正式 API。</summary>
    [Test] public void RuntimeAssembly_HasNoMoverOrSampleDependency()
    {
        var assembly = typeof(NavigationNavigator2D).Assembly;
        foreach (var type in assembly.GetTypes()) Assert.That(type.Name, Does.Not.Contain("Mover"));
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            Assert.That(reference.Name, Does.Not.Contain("Sample"));
            Assert.That(reference.Name, Does.Not.Contain("ARPG"));
        }
    }
    /// <summary>自动重算多个目标时 Kinematic 身体和 Transform 始终保持原位置。</summary>
    [UnityTest] public IEnumerator NavigationWithoutMotor_UpdatesPathButNeverMoves()
    {
        var navigator = Create(RigidbodyType2D.Kinematic, out var body);
        foreach (Vector2 target in new[] { Vector2.right * 3, Vector2.up * 3, Vector2.left * 3 })
        {
            navigator.SetDestination(target);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.That(navigator.HasPath); Assert.That(Vector2.Dot(navigator.DesiredDirection, target), Is.GreaterThan(0));
            Assert.That(body.position, Is.EqualTo(Vector2.zero)); Assert.That(navigator.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        }
    }
    /// <summary>导航不更改 Dynamic 刚体的外部速度；暂停导航不等于停止项目运动系统。</summary>
    [UnityTest] public IEnumerator DynamicMotion_RemainsOwnedByExternalPhysics()
    {
        var navigator = Create(RigidbodyType2D.Dynamic, out var body); body.linearVelocity = Vector2.right;
        navigator.SetDestination(Vector2.up * 3);
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(navigator.HasPath); Assert.That(body.position.x, Is.GreaterThan(0));
        Assert.That(body.linearVelocity, Is.EqualTo(Vector2.right));
        navigator.Pause(); navigator.enabled = false;
        yield return new WaitForFixedUpdate(); Assert.That(body.linearVelocity, Is.EqualTo(Vector2.right));
    }
    /// <summary>子物体独立刚体移动时以其物理位置推进导航，居中圆的净空半径保持稳定。</summary>
    [UnityTest] public IEnumerator ChildRigidbody_UsesItsPhysicsPositionAndStableClearance()
    {
        var root = new GameObject("Navigation root"); SceneManager.MoveGameObjectToScene(root, scene);
        var child = new GameObject("Moving body"); child.transform.SetParent(root.transform);
        var body = child.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate; body.position = new(2, 0);
        var shape = child.AddComponent<CircleCollider2D>(); shape.radius = .2f;
        var navigator = root.AddComponent<NavigationNavigator2D>();
        typeof(NavigationNavigator2D).GetField("bodyCollider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(navigator, shape);
        navigator.SetDestination(new(4, 0));
        yield return new WaitForFixedUpdate();
        Assert.That(navigator.Position, Is.EqualTo(body.position));
        Assert.That(navigator.EffectiveRadius, Is.EqualTo(.22f).Within(.001f));
        Assert.That(navigator.DesiredDirection.x, Is.GreaterThan(0));
        body.MovePosition(new(2.5f, 0));
        yield return new WaitForFixedUpdate();
        Assert.That(navigator.Position, Is.EqualTo(body.position));
        Assert.That(navigator.Position.x, Is.GreaterThan(2));
        Assert.That(navigator.EffectiveRadius, Is.EqualTo(.22f).Within(.001f));
        Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
    }
    /// <summary>根刚体上的偏移矩形仍使用以导航物理位置为圆心的保守包围圆。</summary>
    [UnityTest] public IEnumerator RootRigidbody_OffsetColliderKeepsConservativeRadius()
    {
        var root = new GameObject("Offset body"); SceneManager.MoveGameObjectToScene(root, scene);
        var body = root.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var shape = root.AddComponent<BoxCollider2D>(); shape.offset = new(.4f, 0); shape.size = new(.4f, .4f);
        var navigator = root.AddComponent<NavigationNavigator2D>();
        typeof(NavigationNavigator2D).GetField("bodyCollider", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(navigator, shape);
        navigator.SetDestination(Vector2.right * 2);
        yield return new WaitForFixedUpdate();
        float expected = new Vector2(.6f, .2f).magnitude + .02f;
        Assert.That(navigator.Position, Is.EqualTo(body.position));
        Assert.That(navigator.EffectiveRadius, Is.GreaterThanOrEqualTo(expected - .001f));
        float originalRadius = navigator.EffectiveRadius;
        body.MovePosition(Vector2.right * .5f);
        yield return new WaitForFixedUpdate();
        Assert.That(navigator.Position, Is.EqualTo(body.position));
        Assert.That(navigator.EffectiveRadius, Is.EqualTo(originalRadius).Within(.001f));
    }
}
