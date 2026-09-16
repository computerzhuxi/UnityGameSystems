using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Computerzhuxi.Navigation2D;

/// <summary>验证真实 Tilemap 接入、端点连接与物理查询开销。</summary>
public sealed class PhysicsBoundaryTests
{
    private Scene scene;
    /// <summary>创建专属二维物理场景。</summary>
    [SetUp] public void Setup()=>scene=SceneManager.CreateScene("NavigationBoundary-"+Guid.NewGuid(),new CreateSceneParameters(LocalPhysicsMode.Physics2D));
    /// <summary>卸载测试资源，不写入 Assets。</summary>
    [UnityTearDown] public IEnumerator Cleanup(){yield return SceneManager.UnloadSceneAsync(scene);}
    /// <summary>创建属于本测试的物体。</summary>
    private GameObject Create(string name){var go=new GameObject(name); SceneManager.MoveGameObjectToScene(go,scene); go.layer=8; return go;}
    /// <summary>精确位置可站立但吸附中心受阻时，应报告连接失败。</summary>
    [UnityTest] public IEnumerator EndpointConnection_IsExplicit()
    {
        var box=Create("Grid center obstacle").AddComponent<BoxCollider2D>(); box.size=new(.1f,.1f); yield return null; Physics2D.SyncTransforms();
        var source=new PhysicsTraversalSource2D(scene.GetPhysicsScene2D(),1<<8); var finder=new GridPathfinder2D(); var path=new List<Vector2>();
        Assert.That(finder.FindPath(new(.4f,0),new(2,0),new(Vector2.zero,1),new(.05f),source,path).Status,Is.EqualTo(PathStatus.StartNotConnected));
        Assert.That(finder.FindPath(new(2,0),new(.4f,0),new(Vector2.zero,1),new(.05f),source,path).Status,Is.EqualTo(PathStatus.DestinationNotConnected));
    }
    /// <summary>TilemapCollider2D 更新后可通过相同 Physics2D 接口查询，无需包维护第二份地图。</summary>
    [UnityTest] public IEnumerator TilemapCollider_IsAnOrdinaryObstacle()
    {
        var grid=Create("Grid").AddComponent<Grid>(); var go=Create("Tilemap"); go.transform.SetParent(grid.transform);
        var map=go.AddComponent<Tilemap>(); var collider=go.AddComponent<TilemapCollider2D>();
        var tile=ScriptableObject.CreateInstance<Tile>(); tile.colliderType=Tile.ColliderType.Grid;
        try
        {
            map.SetTile(Vector3Int.zero,tile); collider.ProcessTilemapChanges(); yield return null; Physics2D.SyncTransforms();
            var source=new PhysicsTraversalSource2D(scene.GetPhysicsScene2D(),1<<8);
            Assert.That(source.IsPositionClear(new(.5f,.5f),.1f),Is.False);
            map.SetTile(Vector3Int.zero,null); collider.ProcessTilemapChanges(); yield return null; Physics2D.SyncTransforms();
            Assert.That(source.IsPositionClear(new(.5f,.5f),.1f));
        }
        finally { UnityEngine.Object.Destroy(tile); }
    }
    /// <summary>记录预热物理寻路的耗时分布和分配；节点预算是硬约束，时间仅作为机器相关证据。</summary>
    [UnityTest] public IEnumerator PhysicsSearch_ReportsBudgetAndAllocation()
    {
        var wall=Create("Wall").AddComponent<BoxCollider2D>(); wall.size=new(.1f,3); yield return null; Physics2D.SyncTransforms();
        var source=new PhysicsTraversalSource2D(scene.GetPhysicsScene2D(),1<<8); var finder=new GridPathfinder2D(); var path=new List<Vector2>(); var settings=new GridSettings2D(Vector2.zero,.25f); var options=new PathOptions2D(.2f,4096);
        for(int i=0;i<5;i++) finder.FindPath(new(-3,0),new(3,0),settings,options,source,path);
        var times=new double[100]; var clock=new System.Diagnostics.Stopwatch(); long allocated=GC.GetAllocatedBytesForCurrentThread(); PathResult2D result=default;
        for(int i=0;i<times.Length;i++){clock.Restart(); result=finder.FindPath(new(-3,0),new(3,0),settings,options,source,path); clock.Stop(); times[i]=clock.Elapsed.TotalMilliseconds;}
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated; Array.Sort(times);
        Assert.That(result.Succeeded); Assert.That(result.ExpandedNodes,Is.LessThanOrEqualTo(4096)); Assert.That(allocated,Is.Zero);
        Debug.Log($"NAVIGATION_PERF samples=100 p50_ms={times[50]:F4} p95_ms={times[95]:F4} max_ms={times[99]:F4} allocated={allocated} expanded={result.ExpandedNodes} queries={result.TraversalQueries}");
    }
}
