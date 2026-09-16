using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Computerzhuxi.Navigation2D;

public sealed class NavigationTests
{
    private readonly List<Vector2> path = new();
    private readonly GridPathfinder2D finder = new();
    private readonly GridSettings2D grid = new(Vector2.zero, 1);
    /// <summary>模拟有限网格，隔离物理引擎以验证算法结果与方向契约。</summary>
    private sealed class Map : ITraversalSource2D
    {
        public readonly HashSet<Vector2Int> Blocked = new();
        public bool BlockSegments;
        /// <summary>地图外视为受阻，地图内使用显式网格障碍。</summary>
        public bool IsPositionClear(Vector2 point, float radius) => Mathf.Abs(point.x) <= 12 && Mathf.Abs(point.y) <= 12 && !Blocked.Contains(Vector2Int.RoundToInt(point));
        /// <summary>可单独阻止连线，用于验证节点之间不可通行的情况。</summary>
        public bool IsSegmentClear(Vector2 a, Vector2 b, float radius) => !BlockSegments && IsPositionClear(a,radius) && IsPositionClear(b,radius);
    }
    /// <summary>不同方向下应返回稳定、完整且保留精确终点的路径。</summary>
    [TestCase(GridDirections.Four)] [TestCase(GridDirections.Eight)]
    public void CompletePath_IsDeterministic(GridDirections directions)
    {
        var map = new Map(); map.Blocked.Add(new Vector2Int(1,0));
        var options = new PathOptions2D(.2f,1000,directions);
        Assert.That(finder.FindPath(Vector2.zero,new Vector2(3.1f,0),grid,options,map,path).Succeeded);
        var expected = path.ToArray();
        for(int i=0;i<20;i++) { Assert.That(finder.FindPath(Vector2.zero,new Vector2(3.1f,0),grid,options,map,path).Succeeded); CollectionAssert.AreEqual(expected,path); }
        Assert.That(path[path.Count-1],Is.EqualTo(new Vector2(3.1f,0)));
        Vector2 previous = Vector2.zero;
        foreach(var point in path) { Assert.That(map.IsPositionClear(point,.2f)); if(directions==GridDirections.Four) Assert.That(Mathf.Abs(point.x-previous.x)<.001f || Mathf.Abs(point.y-previous.y)<.001f); previous=point; }
    }
    /// <summary>有限搜索空间耗尽与展开预算耗尽必须能区分。</summary>
    [Test]
    public void Failure_SeparatesBudgetAndUnreachable()
    {
        var map = new Map();
        Assert.That(finder.FindPath(Vector2.zero,new Vector2(10,10),grid,new(.1f,1),map,path).Status,Is.EqualTo(PathStatus.BudgetExceeded));
        for(int y=-12;y<=12;y++) map.Blocked.Add(new Vector2Int(1,y));
        var result=finder.FindPath(Vector2.zero,new Vector2(3,0),grid,new(.1f,1000),map,path);
        Assert.That(result.Status,Is.EqualTo(PathStatus.Unreachable)); Assert.That(path,Is.Empty);
    }
    /// <summary>起点与终点受阻应分别报告，且清除上次成功结果。</summary>
    [Test]
    public void BlockedEndpoints_ClearOldPath()
    {
        var map = new Map(); path.Add(Vector2.one);
        map.Blocked.Add(Vector2Int.zero);
        Assert.That(finder.FindPath(Vector2.zero,Vector2.one,grid,new(.1f),map,path).Status,Is.EqualTo(PathStatus.StartBlocked)); Assert.That(path,Is.Empty);
        map.Blocked.Clear(); map.Blocked.Add(Vector2Int.one);
        Assert.That(finder.FindPath(Vector2.zero,Vector2.one,grid,new(.1f),map,path).Status,Is.EqualTo(PathStatus.DestinationBlocked));
    }
    /// <summary>同格请求只有在整段可通行时才成功。</summary>
    [Test]
    public void SameCell_ChecksSegment()
    {
        var map = new Map();
        Assert.That(finder.FindPath(Vector2.zero,new(.2f,0),grid,new(.1f),map,path).Succeeded);
        map.BlockSegments=true;
        Assert.That(finder.FindPath(Vector2.zero,new(.2f,0),grid,new(.1f),map,path).Status,Is.EqualTo(PathStatus.Unreachable));
    }
    /// <summary>四个正交出口封闭时不能从对角逃出。</summary>
    [Test]
    public void Diagonal_DoesNotCutBlockedCorner()
    {
        var map=new Map(); foreach(var cell in new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}) map.Blocked.Add(cell);
        Assert.That(finder.FindPath(Vector2.zero,new(2,2),grid,new(.1f),map,path).Status,Is.EqualTo(PathStatus.Unreachable));
    }
    /// <summary>非有限坐标、非法半径、方向和预算不触发搜索。</summary>
    [Test]
    public void InvalidInput_IsExplicit()
    {
        var map=new Map();
        foreach(var options in new[]{new PathOptions2D(-1),new PathOptions2D(.1f,0),new PathOptions2D(.1f,5,(GridDirections)5),new PathOptions2D(float.NaN)})
            Assert.That(finder.FindPath(Vector2.zero,Vector2.one,grid,options,map,path).Status,Is.EqualTo(PathStatus.InvalidInput));
        Assert.That(finder.FindPath(new(float.NaN,0),Vector2.one,grid,new(.1f),map,path).Status,Is.EqualTo(PathStatus.InvalidInput));
        Assert.That(finder.FindPath(Vector2.zero,Vector2.one,new(Vector2.zero,0),new(.1f),map,path).Status,Is.EqualTo(PathStatus.InvalidInput));
    }
    /// <summary>恰好允许展开终点的预算仍应成功。</summary>
    [Test]
    public void BudgetBoundary_CountsGoalExpansion()
    {
        var result=finder.FindPath(Vector2.zero,Vector2.right,grid,new(.1f,2),new Map(),path);
        Assert.That(result.Succeeded); Assert.That(result.ExpandedNodes,Is.EqualTo(2));
    }
    /// <summary>负坐标和半格转换遵循明确的最近中心规则。</summary>
    [Test]
    public void Coordinates_RoundTripAndTies()
    {
        Assert.That(grid.WorldToCell(new(-1.5f,.5f)),Is.EqualTo(new Vector2Int(-2,0)));
        Assert.That(grid.WorldToCell(grid.CellToWorld(new(-8,7))),Is.EqualTo(new Vector2Int(-8,7)));
    }
    /// <summary>预热后的算法反复查询不应分配新节点或路径容器。</summary>
    [Test]
    public void WarmSearch_HasNoManagedAllocations()
    {
        var map=new Map(); var options=new PathOptions2D(.1f,4096);
        for(int i=0;i<5;i++) finder.FindPath(Vector2.zero,new(10,10),grid,options,map,path);
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<100;i++) finder.FindPath(Vector2.zero,new(10,10),grid,options,map,path);
        long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.That(allocated,Is.Zero);
    }
}
