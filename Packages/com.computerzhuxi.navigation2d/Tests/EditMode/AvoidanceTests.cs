using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Computerzhuxi.Navigation2D;

/// <summary>通过公开快照接口验证批量避让、长期运动、异常原子性与预热分配。</summary>
public sealed class AvoidanceTests
{
    private sealed class ReentrantInput : IReadOnlyList<AvoidanceAgent2D>
    {
        public Action Callback;
        public int Count => 1;
        public AvoidanceAgent2D this[int index] { get { Callback(); return A(1, Vector2.zero, Vector2.right); } }
        /// <summary>验证求解器只按索引访问输入，不需要枚举器分配。</summary>
        public IEnumerator<AvoidanceAgent2D> GetEnumerator() => throw new NotSupportedException();
        /// <summary>验证求解器不依赖非泛型枚举路径。</summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>非法工作区配置在创建时明确失败。</summary>
    [Test] public void InvalidConfiguration_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AvoidanceWorld2D(0, 0, 5, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AvoidanceWorld2D(2, -1, 5, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AvoidanceWorld2D(2, 3, 5, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AvoidanceWorld2D(2, 1, float.NaN, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AvoidanceWorld2D(2, 1, 5, float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AvoidanceWorld2D(2, 1, 5, 2, -1));
    }

    /// <summary>输入回调触发的同实例重入被拒绝且保留输出；下一次正常求解能够恢复。</summary>
    [Test] public void Reentry_IsAtomicAndGuardRecovers()
    {
        var world = new AvoidanceWorld2D(2, 1, 5, 2);
        var output = new List<AvoidanceResult2D> { new(99, Vector2.one, false, false) };
        var input = new ReentrantInput { Callback = () => world.Solve(Array.Empty<AvoidanceAgent2D>(), .02f, output) };
        Assert.Throws<InvalidOperationException>(() => world.Solve(input, .02f, output));
        Assert.That(output[0].Id, Is.EqualTo(99));
        input.Callback = () => { }; world.Solve(input, .02f, output);
        Assert.That(output[0].Id, Is.EqualTo(1));
    }

    /// <summary>创建默认半径的测试代理。</summary>
    private static AvoidanceAgent2D A(int id, Vector2 p, Vector2 preferred, Vector2 velocity = default,
        int group = 0, float radius = .3f, float speed = 1, bool locked = false) =>
        new(id, group, p, velocity, preferred, radius, speed, locked);

    /// <summary>有限但超出数值支持域的速度必须明确失败，不得绕过限速并污染输出。</summary>
    [Test] public void ExtremeFiniteVelocity_FailsAtomicallyInsteadOfExceedingLimit()
    {
        var output = new List<AvoidanceResult2D> { new(99, Vector2.zero, false, false) };
        var world = new AvoidanceWorld2D(2, 1, 5, 2);
        Assert.Throws<ArithmeticException>(() => world.Solve(new[] { A(1, Vector2.zero, new(2e20f, 0), speed: 1e20f) }, .02f, output));
        Assert.That(output[0].Id, Is.EqualTo(99));
        world.Solve(new[] { A(1, Vector2.zero, Vector2.right) }, .02f, output);
        Assert.That(output[0].Velocity, Is.EqualTo(Vector2.right));
    }

    /// <summary>单体保留目标方向并严格限制速度。</summary>
    [Test] public void SingleAgent_ClampsSpeed()
    {
        var output = new List<AvoidanceResult2D>();
        new AvoidanceWorld2D(4, 3, 5, 2).Solve(new[] { A(1, Vector2.zero, Vector2.right * 4) }, .02f, output);
        Assert.That(output[0].Velocity, Is.EqualTo(Vector2.right));
        Assert.That(output[0].Infeasible, Is.False);
    }

    /// <summary>以 ARPG 角色实际半径与速度验证七秒内绕过锁定同伴并到达目标。</summary>
    [Test] public void ArpgScaleStationaryBlocker_PassesWithinSevenSeconds()
    {
        var world = new AvoidanceWorld2D(2, 1, 6, 1, .1f);
        var input = new AvoidanceAgent2D[2]; var output = new List<AvoidanceResult2D>(2);
        Vector2 position = new(-2, 0), velocity = Vector2.zero, goal = new(3, 0);
        for (int i = 0; i < 350; i++)
        {
            input[0] = A(1, position, Vector2.ClampMagnitude((goal - position) * 3, 3), velocity, radius: .603f, speed: 3);
            input[1] = A(2, Vector2.zero, Vector2.zero, radius: .603f, speed: 3, locked: true);
            world.Solve(input, .02f, output);
            velocity = output[0].Velocity; position += velocity * .02f;
            Assert.That(position.magnitude, Is.GreaterThanOrEqualTo(1.206f - .002f), "step " + i);
            Assert.That(output[0].Infeasible, Is.False);
        }
        Assert.That(Vector2.Distance(position, goal), Is.LessThan(.08f));
    }

    /// <summary>对称迎面与静止障碍都必须真正通过并到达，且不同半径始终保持间距。</summary>
    [TestCase(false, .3f)] [TestCase(true, .3f)] [TestCase(true, .65f)]
    public void LongSimulation_PassesAndArrives(bool locked, float blockerRadius)
    {
        var world = new AvoidanceWorld2D(2, 1, 8, 2);
        var input = new AvoidanceAgent2D[2]; var output = new List<AvoidanceResult2D>(2);
        Vector2 p = new(-3, 0), q = locked ? Vector2.zero : new(3, 0);
        Vector2 v = Vector2.zero, w = Vector2.zero, goal = new(3, 0), otherGoal = new(-3, 0);
        const float dt = .02f;
        for (int i = 0; i < 1800; i++)
        {
            input[0] = A(1, p, Vector2.ClampMagnitude((goal - p) * 2, 1), v);
            input[1] = A(2, q, locked ? Vector2.zero : Vector2.ClampMagnitude((otherGoal - q) * 2, 1), w,
                radius: blockerRadius, locked: locked);
            world.Solve(input, dt, output);
            v = output[0].Velocity; w = output[1].Velocity; p += v * dt; q += w * dt;
            Assert.That(Vector2.Distance(p, q), Is.GreaterThanOrEqualTo(.3f + blockerRadius - .002f), "step " + i);
            Assert.That(v.magnitude, Is.LessThanOrEqualTo(1.0001f));
            Assert.That(output[0].Infeasible, Is.False);
        }
        Assert.That(Vector2.Distance(p, goal), Is.LessThan(.06f));
        if (locked) Assert.That(q, Is.EqualTo(Vector2.zero));
        else Assert.That(Vector2.Distance(q, otherGoal), Is.LessThan(.06f));
    }

    /// <summary>不同组在相同位置仍不产生避让，锁定速度不受最大速度裁剪。</summary>
    [Test] public void GroupsAndLocked_DoNotAlterUnrelatedMotion()
    {
        var output = new List<AvoidanceResult2D>();
        new AvoidanceWorld2D(2, 1, 5, 2).Solve(new[] {
            A(1, Vector2.zero, Vector2.right), A(2, Vector2.zero, Vector2.left, Vector2.up * 2, group: 1, locked: true)
        }, .02f, output);
        Assert.That(output[0].Velocity, Is.EqualTo(Vector2.right));
        Assert.That(output[1].Velocity, Is.EqualTo(Vector2.up * 2));
    }

    /// <summary>相同距离的邻居通过稳定 ID 决定次序，不依赖输入顺序。</summary>
    [Test] public void Permutations_ProduceIdenticalVelocityAndReportTruncation()
    {
        var input = new[] { A(3, Vector2.zero, Vector2.right), A(2, Vector2.right, Vector2.left), A(1, Vector2.up, Vector2.down) };
        var a = new List<AvoidanceResult2D>(); var b = new List<AvoidanceResult2D>();
        var world = new AvoidanceWorld2D(3, 1, 5, 2);
        world.Solve(input, .02f, a); Array.Reverse(input); world.Solve(input, .02f, b);
        for (int i = 0; i < 3; i++)
        {
            Assert.That(a[i].Id, Is.EqualTo(b[2 - i].Id));
            Assert.That(a[i].Velocity, Is.EqualTo(b[2 - i].Velocity));
            Assert.That(a[i].TruncatedNeighbors, Is.True);
        }
    }

    /// <summary>重叠且没有修正速度预算时，返回有限值并明确标记不可行。</summary>
    [Test] public void CoincidentAgents_AreFiniteAndInfeasible()
    {
        var output = new List<AvoidanceResult2D>();
        new AvoidanceWorld2D(2, 1, 5, 2).Solve(new[] { A(1, Vector2.zero, Vector2.zero, speed: 0), A(2, Vector2.zero, Vector2.zero, speed: 0) }, .02f, output);
        Assert.That(output[0].Velocity, Is.EqualTo(Vector2.zero)); Assert.That(output[0].Infeasible, Is.True);
        Assert.That(output[1].Infeasible, Is.True);
    }

    /// <summary>非法数值、重复 ID 与代理预算异常均不得污染已有输出。</summary>
    [Test] public void InvalidInputs_AreAtomicAndWorldRecovers()
    {
        var world = new AvoidanceWorld2D(2, 1, 5, 2); var output = new List<AvoidanceResult2D> { new(99, Vector2.one, false, false) };
        var normal = A(1, Vector2.zero, Vector2.right);
        var invalid = new[] { A(2, new(float.NaN, 0), Vector2.zero), A(2, Vector2.zero, new(float.PositiveInfinity, 0)),
            A(2, Vector2.zero, Vector2.zero, radius: -1), A(2, Vector2.zero, Vector2.zero, speed: -1), normal };
        foreach (var value in invalid)
        {
            Assert.Throws<ArgumentException>(() => world.Solve(new[] { normal, value }, .02f, output));
            Assert.That(output.Count, Is.EqualTo(1)); Assert.That(output[0].Id, Is.EqualTo(99));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => world.Solve(new[] { normal }, 0, output));
        Assert.Throws<ArgumentException>(() => world.Solve(new[] { normal, normal, normal }, .02f, output));
        world.Solve(new[] { normal }, .02f, output); Assert.That(output[0].Id, Is.EqualTo(1));
    }

    /// <summary>零邻居预算保持限速同时声明截断，空批次清空输出。</summary>
    [Test] public void ZeroNeighborBudgetAndEmptyBatch_AreExplicit()
    {
        var world = new AvoidanceWorld2D(2, 0, 5, 2); var output = new List<AvoidanceResult2D>();
        world.Solve(new[] { A(1, Vector2.zero, Vector2.right), A(2, Vector2.zero, Vector2.left) }, .02f, output);
        Assert.That(output[0].TruncatedNeighbors, Is.True); Assert.That(output[0].Velocity, Is.EqualTo(Vector2.right));
        world.Solve(Array.Empty<AvoidanceAgent2D>(), .02f, output); Assert.That(output, Is.Empty);
    }

    /// <summary>固定容量预热后，包括不可行回退在内的求解不得产生托管分配。</summary>
    [Test] public void WarmSolve_DoesNotAllocate()
    {
        var world = new AvoidanceWorld2D(8, 7, 5, 2); var input = new AvoidanceAgent2D[8];
        for (int i = 0; i < input.Length; i++) input[i] = A(i, Vector2.zero, Vector2.right);
        var output = new List<AvoidanceResult2D>(8);
        for (int i = 0; i < 30; i++) world.Solve(input, .02f, output);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) world.Solve(input, .02f, output);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(bytes, Is.Zero);
    }

    /// <summary>记录128代理有界邻域批量的实测开销，同时检查快照不被推进且输出顺序保持。</summary>
    [Test] public void Batch128_RecordsPerformanceAndPreservesSnapshots()
    {
        var world = new AvoidanceWorld2D(128, 12, 3, 2); var input = new AvoidanceAgent2D[128];
        for (int i = 0; i < input.Length; i++) input[i] = A(i, new(i % 16, i / 16), (i % 2 == 0 ? Vector2.right : Vector2.left));
        var output = new List<AvoidanceResult2D>(128);
        for (int i = 0; i < 20; i++) world.Solve(input, .02f, output);
        var timer = Stopwatch.StartNew(); long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 200; i++) world.Solve(input, .02f, output);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before; timer.Stop();
        TestContext.WriteLine("128 agents x 200 solves: " + timer.Elapsed.TotalMilliseconds + " ms; allocated=" + bytes);
        Assert.That(bytes, Is.Zero);
        for (int i = 0; i < input.Length; i++)
        {
            Assert.That(output[i].Id, Is.EqualTo(i)); Assert.That(input[i].Position, Is.EqualTo(new Vector2(i % 16, i / 16)));
        }
    }
}
