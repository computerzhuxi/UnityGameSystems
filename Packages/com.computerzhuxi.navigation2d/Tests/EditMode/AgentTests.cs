using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Computerzhuxi.Navigation2D;

/// <summary>通过公开 API 验证跟随代理状态、节流、异常恢复和无分配热路径。</summary>
public sealed class AgentTests
{
    private sealed class Source : ITraversalSource2D
    {
        public bool Blocked, Throw;
        public Action Callback;
        /// <summary>提供可切换的占位状态与异常回调。</summary>
        public bool IsPositionClear(Vector2 position, float radius)
        { Callback?.Invoke(); if (Throw) throw new InvalidOperationException("test"); return !Blocked; }
        /// <summary>提供一致的整段通行状态。</summary>
        public bool IsSegmentClear(Vector2 start, Vector2 end, float radius) => IsPositionClear(start, radius);
    }
    /// <summary>创建固定配置，避免测试依赖包内部成员。</summary>
    private static NavigationAgent2D Create(float interval = .2f, int budget = 4096) =>
        new(new(Vector2.zero, .5f), new(.1f, budget), new(.05f, .1f, interval, .3f));

    /// <summary>可选前视使局部避让不再执着于其他圆形身体内部的短路径点。</summary>
    [Test] public void LookAhead_WithAvoidance_PassesOccupiedIntermediateWaypoint()
    {
        var agent = new NavigationAgent2D(new(Vector2.zero, .5f), new(.32f), new(.15f, .5f, .25f, .25f, 3));
        var source = new Source(); var world = new AvoidanceWorld2D(2, 1, 6, 1, .1f);
        var input = new AvoidanceAgent2D[2]; var output = new List<AvoidanceResult2D>(2);
        Vector2 position = Vector2.zero, velocity = Vector2.zero;
        agent.SetDestination(new(5, 0));
        for (int i = 0; i < 350; i++)
        {
            agent.Tick(position, .02f, source);
            input[0] = new(1, 0, position, velocity, agent.DesiredDirection * 3, .603f, 3);
            input[1] = new(2, 0, new(2, 0), Vector2.zero, Vector2.zero, .603f, 0, true);
            world.Solve(input, .02f, output); velocity = output[0].Velocity;
            position += velocity * .02f;
            Assert.That(Vector2.Distance(position, new Vector2(2, 0)), Is.GreaterThan(1.204f));
        }
        Assert.That(position.x, Is.GreaterThan(3.25f));
    }

    /// <summary>连续移动目标重算时，首个建议必须继续前进而非回到身后网格中心。</summary>
    [TestCase(1f)] [TestCase(-1f)]
    public void MovingTarget_DoesNotReverse(float sign)
    {
        var agent = Create(0); var source = new Source();
        for (int i = 0; i < 30; i++)
        {
            Vector2 position = new(sign * (.2f + .13f * i), 0);
            agent.UpdateDestination(position + Vector2.right * sign * 3);
            agent.Tick(position, .02f, source);
            Assert.That(agent.DesiredDirection.x * sign, Is.GreaterThan(0));
            Assert.That(agent.HasPath);
        }
    }
    /// <summary>每帧更新任务不会绕过重算间隔，阈值以最近查询终点为基准累计。</summary>
    [Test] public void MovingTarget_ThrottlesQueriesAndAccumulatesChanges()
    {
        var agent = Create(); var source = new Source(); agent.SetDestination(new(3, 0));
        agent.Tick(Vector2.zero, .01f, source);
        for (int i = 1; i <= 9; i++) { agent.UpdateDestination(new(3 + .03f * i, 0)); agent.Tick(Vector2.zero, .01f, source); }
        Assert.That(agent.QueryCount, Is.EqualTo(1));
        agent.Tick(Vector2.zero, .2f, source); Assert.That(agent.QueryCount, Is.EqualTo(2));
        Assert.That(agent.CurrentPath[agent.CurrentPath.Count - 1], Is.EqualTo(agent.Destination));
    }
    /// <summary>动态封路立即停止，失败按间隔重试，移除障碍后自动恢复。</summary>
    [Test] public void DynamicBlock_RetriesWithoutBusyLoop()
    {
        var agent = Create(); var source = new Source(); agent.SetDestination(Vector2.right * 3);
        agent.Tick(Vector2.zero, .01f, source); source.Blocked = true;
        agent.Tick(Vector2.zero, .01f, source); Assert.That(agent.DesiredDirection, Is.EqualTo(Vector2.zero)); Assert.That(agent.HasPath, Is.False);
        agent.Tick(Vector2.zero, .3f, source); Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.Failed));
        Assert.That(agent.LastResult.Value.Status, Is.EqualTo(PathStatus.StartBlocked)); int count = agent.QueryCount;
        for (int i = 0; i < 10; i++) agent.Tick(Vector2.zero, .01f, source);
        Assert.That(agent.QueryCount, Is.EqualTo(count)); source.Blocked = false;
        agent.Tick(Vector2.zero, .3f, source); Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.Following));
    }
    /// <summary>暂停及零时间冻结查询与计时，恢复和停止语义明确。</summary>
    [Test] public void PauseZeroTimeResumeAndStop()
    {
        var agent = Create(); var source = new Source(); agent.SetDestination(Vector2.right * 3);
        agent.Tick(Vector2.zero, 0, source); Assert.That(agent.QueryCount, Is.Zero);
        agent.Tick(Vector2.zero, .02f, source); agent.Pause(); agent.UpdateDestination(Vector2.right * 4);
        agent.Tick(Vector2.zero, 5, source); Assert.That(agent.QueryCount, Is.EqualTo(1)); Assert.That(agent.DesiredDirection, Is.EqualTo(Vector2.zero));
        agent.Resume(); agent.Tick(Vector2.zero, .01f, source); Assert.That(agent.QueryCount, Is.EqualTo(1));
        agent.Stop(); Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.Idle)); Assert.That(agent.CurrentPath, Is.Empty); Assert.That(agent.LastResult, Is.Null);
    }
    /// <summary>不可用域立即失效，恢复或替换源时不等待旧域失败计时。</summary>
    [Test] public void DomainInvalidation_DiscardsAllOldProgress()
    {
        var agent = Create(); var source = new Source(); agent.SetDestination(Vector2.right * 3); agent.Tick(Vector2.zero, .01f, source);
        agent.Tick(Vector2.zero, .01f, null, 1, false);
        Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.DomainUnavailable)); Assert.That(agent.HasPath, Is.False); Assert.That(agent.LastResult, Is.Null);
        agent.Tick(Vector2.zero, .01f, source, 2); Assert.That(agent.QueryCount, Is.EqualTo(2));
        agent.Tick(Vector2.zero, .01f, new Source(), 2); Assert.That(agent.QueryCount, Is.EqualTo(3));
    }
    /// <summary>暂停期间切换有效域也应显示待查询，不能保留旧域的 Following 状态。</summary>
    [Test] public void DomainChangeWhilePaused_IsPending()
    {
        var agent = Create(); var source = new Source(); agent.SetDestination(Vector2.right * 3);
        agent.Tick(Vector2.zero, .02f, source); agent.Pause(); agent.Tick(Vector2.zero, 0, source, 1);
        Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.Pending)); Assert.That(agent.HasPath, Is.False);
        Assert.That(agent.QueryCount, Is.EqualTo(1)); agent.Resume(); agent.Tick(Vector2.zero, .02f, source, 1);
        Assert.That(agent.QueryCount, Is.EqualTo(2));
    }
    /// <summary>相同输入、时钟和障碍事实生成相同路径、进度及请求次数。</summary>
    [Test] public void IdenticalInputs_AreDeterministic()
    {
        var first = Create(); var second = Create(); var source = new Source(); Vector2 position = new(.2f, 0);
        for (int i = 0; i < 30; i++)
        {
            Vector2 destination = position + new Vector2(3, i % 3 * .2f);
            first.UpdateDestination(destination); second.UpdateDestination(destination);
            first.Tick(position, .02f, source); second.Tick(position, .02f, source);
            Assert.That(first.CurrentPath, Is.EqualTo(second.CurrentPath)); Assert.That(first.CurrentPathIndex, Is.EqualTo(second.CurrentPathIndex));
            Assert.That(first.DesiredDirection, Is.EqualTo(second.DesiredDirection)); Assert.That(first.QueryCount, Is.EqualTo(second.QueryCount));
            position += first.DesiredDirection * .02f;
        }
    }
    /// <summary>同格终点可到达，但距离很近且被障碍阻挡不能误报到达。</summary>
    [Test] public void Arrival_RequiresClearSegment()
    {
        var agent = Create(); var source = new Source { Blocked = true }; agent.SetDestination(new(.02f, 0));
        agent.Tick(Vector2.zero, .01f, source); Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.Failed));
        source.Blocked = false; agent.Tick(Vector2.zero, .01f, source); Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.Arrived));
        agent.UpdateDestination(new(.2f, 0)); agent.Tick(Vector2.zero, .4f, source); Assert.That(agent.DesiredDirection.x, Is.GreaterThan(0));
    }
    /// <summary>小于目标阈值的尾端位移也不会在旧路径末端永久停住。</summary>
    [Test] public void SmallTargetDrift_RepathsAfterOldPathEnds()
    {
        var agent = Create(0); var source = new Source(); agent.SetDestination(new(3, 0)); agent.Tick(Vector2.zero, .01f, source);
        agent.UpdateDestination(new(3.08f, 0));
        Vector2 position = Vector2.zero;
        for (int i = 0; i < 100 && agent.State != NavigationAgentState2D.Arrived; i++)
        {
            agent.Tick(position, .02f, source);
            position += agent.DesiredDirection * Mathf.Min(.05f, agent.RemainingWaypointDistance);
        }
        Assert.That(position.x, Is.GreaterThan(3.02f));
        Assert.That(agent.State, Is.EqualTo(NavigationAgentState2D.Arrived));
    }
    /// <summary>只读路径不允许外部修改；非法输入明确拒绝，搜索预算仍保留原失败原因。</summary>
    [Test] public void Contracts_ReadOnlyInvalidInputAndBudget()
    {
        var agent = Create(0, 1); var source = new Source(); agent.SetDestination(Vector2.right * 10); agent.Tick(Vector2.zero, .01f, source);
        Assert.That(agent.LastResult.Value.Status, Is.EqualTo(PathStatus.BudgetExceeded));
        Assert.Throws<NotSupportedException>(() => ((IList<Vector2>)agent.CurrentPath).Add(Vector2.zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => agent.UpdateDestination(new(float.NaN, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => agent.Tick(Vector2.zero, -1, source));
        Assert.Throws<ArgumentException>(() => new NavigationAgent2D(new(Vector2.zero, 1), new(0), default));
    }
    /// <summary>障碍源异常或回调修改代理会停止建议；移除错误回调后实例可以继续使用。</summary>
    [TestCase(false)] [TestCase(true)] public void ExceptionsAndReentry_Recover(bool reentry)
    {
        var agent = Create(); var source = new Source(); agent.SetDestination(Vector2.right * 3);
        if (reentry) source.Callback = agent.Stop; else source.Throw = true;
        Assert.Throws<InvalidOperationException>(() => agent.Tick(Vector2.zero, .01f, source));
        Assert.That(agent.HasPath, Is.False); Assert.That(agent.DesiredDirection, Is.EqualTo(Vector2.zero)); Assert.That(agent.LastResult, Is.Null);
        source.Callback = null; source.Throw = false; agent.Tick(Vector2.zero, .4f, source); Assert.That(agent.HasPath);
    }
    /// <summary>预热后的稳定跟随不重复查询且不分配托管内存。</summary>
    [Test] public void WarmFollowing_AllocatesNothing()
    {
        var agent = Create(); var source = new Source(); agent.SetDestination(Vector2.right * 3);
        for (int i = 0; i < 20; i++) agent.Tick(Vector2.zero, .02f, source);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) agent.Tick(Vector2.zero, .02f, source);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero); Assert.That(agent.QueryCount, Is.EqualTo(1));
    }
}
