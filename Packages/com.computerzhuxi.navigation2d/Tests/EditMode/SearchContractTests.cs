using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Computerzhuxi.Navigation2D;

/// <summary>验证异常恢复、端点连接和与独立最短路基准的一致性。</summary>
public sealed class SearchContractTests
{
    private sealed class Source : ITraversalSource2D
    {
        public readonly HashSet<Vector2Int> Blocked=new();
        public bool Throws;
        /// <summary>提供测试专用的有限地图。</summary>
        public bool IsPositionClear(Vector2 p,float r) { if(Throws) throw new InvalidOperationException("test source"); return Mathf.Abs(p.x)<=4 && Mathf.Abs(p.y)<=4 && !Blocked.Contains(Vector2Int.RoundToInt(p)); }
        /// <summary>只检查格点相邻边，两端均允许才可通行。</summary>
        public bool IsSegmentClear(Vector2 a,Vector2 b,float r)=>IsPositionClear(a,r)&&IsPositionClear(b,r);
    }
    /// <summary>源异常不得留下输出，后续请求仍可使用相同寻路器。</summary>
    [Test] public void SourceException_DoesNotPoisonFinder()
    {
        var finder=new GridPathfinder2D(); var path=new List<Vector2>{Vector2.one}; var source=new Source{Throws=true};
        Assert.Throws<InvalidOperationException>(()=>finder.FindPath(Vector2.zero,Vector2.one,new(Vector2.zero,1),new(.1f),source,path));
        Assert.That(path,Is.Empty); source.Throws=false;
        Assert.That(finder.FindPath(Vector2.zero,Vector2.one,new(Vector2.zero,1),new(.1f),source,path).Succeeded);
    }
    /// <summary>随机有限地图的四方向路径长度应与独立广度优先基准一致。</summary>
    [Test] public void FourDirections_MatchesBreadthFirstOracle()
    {
        var random=new System.Random(711); var finder=new GridPathfinder2D(); var path=new List<Vector2>();
        var offsets=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        for(int run=0;run<40;run++)
        {
            var map=new Source(); var start=new Vector2Int(-4,-4); var goal=new Vector2Int(4,4);
            for(int x=-4;x<=4;x++) for(int y=-4;y<=4;y++) if(random.NextDouble()<.25 && new Vector2Int(x,y)!=start && new Vector2Int(x,y)!=goal) map.Blocked.Add(new(x,y));
            var queue=new Queue<Vector2Int>(); var distance=new Dictionary<Vector2Int,int>{{start,0}}; queue.Enqueue(start);
            while(queue.Count>0) { var cell=queue.Dequeue(); foreach(var offset in offsets) { var next=cell+offset; if(distance.ContainsKey(next)||!map.IsPositionClear(next,0))continue; distance[next]=distance[cell]+1; queue.Enqueue(next); } }
            var result=finder.FindPath(start,goal,new(Vector2.zero,1),new(0,1000,GridDirections.Four),map,path);
            if(distance.TryGetValue(goal,out int expected)) { Assert.That(result.Succeeded,"seed iteration "+run); Assert.That(path.Count,Is.EqualTo(expected)); }
            else Assert.That(result.Status,Is.EqualTo(PathStatus.Unreachable));
        }
    }
    /// <summary>空引用输出抛出参数异常，空列表则是合法可复用输出。</summary>
    [Test] public void NullOutput_ThrowsWithoutPoisoningFinder()
    {
        var finder = new GridPathfinder2D(); var source = new Source();
        Assert.Throws<ArgumentNullException>(() => finder.FindPath(Vector2.zero, Vector2.one, new(Vector2.zero, 1), new(.1f), source, null));
        var output = new List<Vector2>();
        Assert.That(finder.FindPath(Vector2.zero, Vector2.one, new(Vector2.zero, 1), new(.1f), source, output).Succeeded);
        Assert.That(output, Is.Not.Empty);
    }
    /// <summary>源回调重入同一实例必须拒绝；捕获或传播异常后实例均可继续查询。</summary>
    [TestCase(true)] [TestCase(false)]
    public void Reentry_IsRejectedAndFinderRecovers(bool catchInsideSource)
    {
        var finder = new GridPathfinder2D(); var output = new List<Vector2>();
        var source = new ReentrantSource { Finder = finder, Catch = catchInsideSource };
        if (catchInsideSource)
            Assert.That(finder.FindPath(Vector2.zero, Vector2.one, new(Vector2.zero, 1), new(.1f), source, output).Succeeded);
        else
        {
            Assert.Throws<InvalidOperationException>(() => finder.FindPath(Vector2.zero, Vector2.one, new(Vector2.zero, 1), new(.1f), source, output));
            Assert.That(output, Is.Empty);
        }
        Assert.That(source.Attempted, Is.True);
        Assert.That(finder.FindPath(Vector2.zero, Vector2.one, new(Vector2.zero, 1), new(.1f), new Source(), output).Succeeded);
    }
    private sealed class ReentrantSource : ITraversalSource2D
    {
        public GridPathfinder2D Finder;
        public bool Catch, Attempted;
        /// <summary>首次查询时模拟可通行性提供者误用同一寻路器。</summary>
        public bool IsPositionClear(Vector2 position, float radius)
        {
            if (!Attempted)
            {
                Attempted = true;
                if (Catch) Assert.Throws<InvalidOperationException>(() => Reenter());
                else Reenter();
            }
            return true;
        }
        /// <summary>通过公开查询入口触发实例重入。</summary>
        private void Reenter() => Finder.FindPath(Vector2.zero, Vector2.one, new(Vector2.zero, 1), new(.1f), this, new List<Vector2>());
        /// <summary>为外层查询提供无障碍线段。</summary>
        public bool IsSegmentClear(Vector2 start, Vector2 end, float radius) => true;
    }
}
