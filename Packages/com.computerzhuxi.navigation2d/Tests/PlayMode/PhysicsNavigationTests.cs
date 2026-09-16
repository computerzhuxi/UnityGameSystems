using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Computerzhuxi.Navigation2D;

public sealed class PhysicsNavigationTests
{
    private Scene scene;
    private PhysicsScene2D physics;
    /// <summary>每个测试创建独立物理场景，避免影响宿主地图和全局设置。</summary>
    [SetUp] public void Setup() { scene=SceneManager.CreateScene("NavigationTest-"+System.Guid.NewGuid(),new CreateSceneParameters(LocalPhysicsMode.Physics2D)); physics=scene.GetPhysicsScene2D(); }
    /// <summary>卸载测试专属场景及其障碍。</summary>
    [UnityTearDown] public IEnumerator Cleanup() { yield return SceneManager.UnloadSceneAsync(scene); }
    /// <summary>创建明确归属于隔离测试场景的障碍。</summary>
    private BoxCollider2D Box(Vector2 point,Vector2 size,bool trigger=false)
    { var go=new GameObject("Obstacle"); SceneManager.MoveGameObjectToScene(go,scene); go.layer=8; go.transform.position=point; var box=go.AddComponent<BoxCollider2D>(); box.size=size; box.isTrigger=trigger; Physics2D.SyncTransforms(); return box; }
    /// <summary>薄墙必须阻挡扫掠，但 A* 可以从两侧绕行。</summary>
    [UnityTest] public IEnumerator ThinWall_RequiresDetour()
    {
        Box(new(.5f,0),new(.05f,2)); yield return null;
        var source=new PhysicsTraversalSource2D(physics,1<<8); var path=new List<Vector2>();
        Assert.That(source.IsPositionClear(Vector2.zero,.1f)); Assert.That(source.IsPositionClear(Vector2.right,.1f));
        Assert.That(source.IsSegmentClear(Vector2.zero,Vector2.right,.1f),Is.False);
        var result=new GridPathfinder2D().FindPath(Vector2.zero,Vector2.right,new(Vector2.zero,.25f),new(.1f),source,path);
        Assert.That(result.Succeeded); Vector2 previous=Vector2.zero;
        foreach(var point in path) { Assert.That(source.IsSegmentClear(previous,point,.1f)); previous=point; }
    }
    /// <summary>移动障碍立即改变新查询结果，不沿用旧缓存。</summary>
    [UnityTest] public IEnumerator DynamicObstacle_AndRadius()
    {
        var obstacle=Box(new(0,.6f),new(4,.2f)); yield return null;
        var source=new PhysicsTraversalSource2D(physics,1<<8);
        Assert.That(source.IsSegmentClear(new(-1,0),new(1,0),.2f));
        Assert.That(source.IsSegmentClear(new(-1,0),new(1,0),.55f),Is.False);
        obstacle.transform.position=new(0,0); Physics2D.SyncTransforms();
        Assert.That(source.IsSegmentClear(new(-1,0),new(1,0),.2f),Is.False);
        obstacle.enabled=false;
        Assert.That(source.IsSegmentClear(new(-1,0),new(1,0),.2f));
    }
    /// <summary>Trigger 与层掩码均由查询实例显式选择。</summary>
    [UnityTest] public IEnumerator Filters_AreExplicit()
    {
        Box(Vector2.zero,Vector2.one,true); yield return null;
        Assert.That(new PhysicsTraversalSource2D(physics,1<<8).IsPositionClear(Vector2.zero,.1f));
        Assert.That(new PhysicsTraversalSource2D(physics,1<<8,true).IsPositionClear(Vector2.zero,.1f),Is.False);
        Assert.That(new PhysicsTraversalSource2D(physics,1<<9,true).IsPositionClear(Vector2.zero,.1f));
        Assert.That(new PhysicsTraversalSource2D(Physics2D.defaultPhysicsScene,1<<8,true).IsPositionClear(Vector2.zero,.1f));
    }
}
