using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Computerzhuxi.Navigation2D.Internal;
namespace Computerzhuxi.Navigation2D
{
    /// <summary>复用有界工作区计算圆形代理建议速度，不推进位置、不注册对象；同一实例禁止并发或重入。</summary>
    public sealed class AvoidanceWorld2D
    {
        private readonly AvoidanceAgent2D[] agents;
        private readonly AvoidanceResult2D[] results;
        private readonly int[] order, original, neighbors;
        private readonly double[] distances;
        private readonly OrcaSolver2D solver;
        private readonly float neighborDistance, timeHorizon, passingBias;
        private int solving;

        /// <summary>创建固定容量工作区；邻域距离按圆心计算，零邻居预算仅限速并报告截断。</summary>
        /// <param name="passingBias">有前方碰撞风险时，向期望方向右侧添加的速度比例；零表示禁用对称破局。</param>
        public AvoidanceWorld2D(int maxAgents, int maxNeighbors, float neighborDistance, float timeHorizon, float passingBias = .05f)
        {
            if (maxAgents <= 0 || maxNeighbors < 0 || maxNeighbors > maxAgents || !Positive(neighborDistance)
                || !Positive(timeHorizon) || neighborDistance > 1000000 || timeHorizon < .0001f || timeHorizon > 10000 || !Finite(passingBias) || passingBias < 0 || passingBias > 1)
                throw new ArgumentOutOfRangeException(nameof(maxAgents), "容量、邻域、时间范围或偏置无效。");
            agents = new AvoidanceAgent2D[maxAgents]; results = new AvoidanceResult2D[maxAgents];
            order = new int[maxAgents]; original = new int[maxAgents]; neighbors = new int[maxNeighbors];
            distances = new double[maxNeighbors]; solver = new OrcaSolver2D(maxNeighbors);
            this.neighborDistance = neighborDistance; this.timeHorizon = timeHorizon; this.passingBias = passingBias;
        }

        /// <summary>一次性验证与求解后替换输出；输出预留批次容量时热路径不分配，输入在调用期间必须保持不变。</summary>
        /// <remarks>锁定代理原样输出实际速度。邻居截断、初始重叠或环境裁剪会破坏无碰撞前提；极端数值超出浮点运算范围时抛出 ArithmeticException。</remarks>
        public void Solve(IReadOnlyList<AvoidanceAgent2D> inputs, float deltaTime, List<AvoidanceResult2D> outputs)
        {
            if (Interlocked.CompareExchange(ref solving, 1, 0) != 0) throw new InvalidOperationException("同一避让工作区不可重入。");
            try
            {
                if (inputs == null) throw new ArgumentNullException(nameof(inputs));
                if (outputs == null) throw new ArgumentNullException(nameof(outputs));
                if (!Positive(deltaTime)) throw new ArgumentOutOfRangeException(nameof(deltaTime));
                // ORCA 使用单精度几何；明确限定数值域，避免有限输入在平方/行列式中溢出。
                if (deltaTime < .0001f || deltaTime > 10000) throw new ArithmeticException("步长超出支持范围 [0.0001, 10000]。");
                int count = inputs.Count;
                if (count > agents.Length || count < 0) throw new ArgumentException("批次超过代理容量。", nameof(inputs));
                for (int i = 0; i < count; i++)
                {
                    var a = inputs[i];
                    if (!Finite(a.Position) || !Finite(a.Velocity) || !Finite(a.PreferredVelocity)
                        || !Positive(a.Radius) || !Finite(a.MaxSpeed) || a.MaxSpeed < 0)
                        throw new ArgumentException("代理快照包含非法数值。", nameof(inputs));
                    if (!Supported(a.Position) || !Supported(a.Velocity) || !Supported(a.PreferredVelocity)
                        || a.Radius > 1000000 || a.MaxSpeed > 1000000)
                        throw new ArithmeticException("避让坐标、速度分量、半径和最大速度的绝对值不得超过 1000000。");
                    agents[i] = a; order[i] = i; original[i] = i;
                }
                // ID 排序既验证重复身份，也让后续同位置比较不受输入排列影响。
                SortIds(count);
                for (int i = 1; i < count; i++)
                    if (agents[original[i - 1]].Id == agents[original[i]].Id)
                        throw new ArgumentException("批次含重复代理 ID。", nameof(inputs));
                SortPositions(count);
                for (int s = 0; s < count; s++)
                {
                    int index = order[s]; var a = agents[index];
                    int found = FindNeighbors(s, count, out bool truncated);
                    Vector2 velocity = a.Velocity; bool infeasible = false;
                    if (!a.Locked)
                    {
                        solver.Clear(); bool bias = false;
                        for (int j = 0; j < found; j++)
                        {
                            var b = agents[neighbors[j]];
                            solver.Add(a, b, timeHorizon, deltaTime);
                            Vector2 relative = b.Position - a.Position;
                            float along = Vector2.Dot(relative, a.PreferredVelocity.normalized);
                            float cross = Mathf.Abs(OrcaSolver2D.Det(a.PreferredVelocity.normalized, relative));
                            // 只对前方通行走廊内的邻居破除完全对称；最终速度仍满足 ORCA 约束。
                            if (along > 0 && cross < a.Radius + b.Radius + .1f) bias = true;
                        }
                        Vector2 preferred = a.PreferredVelocity;
                        if (bias) preferred += new Vector2(preferred.y, -preferred.x) * passingBias;
                        velocity = solver.Solve(preferred, a.MaxSpeed, out infeasible);
                    }
                    if (!Finite(velocity)) throw new ArithmeticException("避让计算超出浮点范围。");
                    results[index] = new AvoidanceResult2D(a.Id, velocity, truncated, infeasible);
                }
                // 所有输入及计算成功后再发布，异常不会留下半批结果。
                if (outputs.Capacity < count) outputs.Capacity = count;
                outputs.Clear();
                for (int i = 0; i < count; i++) outputs.Add(results[i]);
            }
            finally { Volatile.Write(ref solving, 0); }
        }

        /// <summary>按稳定 ID 排序索引以检测重复，不改变调用方快照。</summary>
        private void SortIds(int count)
        {
            for (int i = 1; i < count; i++)
            {
                int item = original[i], j = i - 1;
                while (j >= 0 && agents[original[j]].Id > agents[item].Id) { original[j + 1] = original[j]; j--; }
                original[j + 1] = item;
            }
        }

        /// <summary>共享按 X 坐标排序的索引，允许每个代理只扫描邻域横带。</summary>
        private void SortPositions(int count)
        {
            for (int i = 1; i < count; i++)
            {
                int item = order[i], j = i - 1;
                while (j >= 0 && PositionAfter(order[j], item)) { order[j + 1] = order[j]; j--; }
                order[j + 1] = item;
            }
        }

        /// <summary>使用 ID 为同 X 坐标建立稳定顺序。</summary>
        private bool PositionAfter(int a, int b) => agents[a].Position.x > agents[b].Position.x
            || (agents[a].Position.x == agents[b].Position.x && agents[a].Id > agents[b].Id);

        /// <summary>扫描左右横带，保留距离最近的有界邻居并统计所有被截断对象。</summary>
        private int FindNeighbors(int sortedIndex, int count, out bool truncated)
        {
            int self = order[sortedIndex], kept = 0, total = 0;
            for (int direction = -1; direction <= 1; direction += 2)
                for (int s = sortedIndex + direction; s >= 0 && s < count; s += direction)
                {
                    int candidate = order[s]; var a = agents[self]; var b = agents[candidate];
                    double dx = (double)b.Position.x - a.Position.x;
                    if (Math.Abs(dx) > neighborDistance) break;
                    double dy = (double)b.Position.y - a.Position.y, distance = dx * dx + dy * dy;
                    if (a.Group != b.Group || distance > (double)neighborDistance * neighborDistance) continue;
                    total++;
                    int insert = kept;
                    while (insert > 0 && (distance < distances[insert - 1]
                        || (distance == distances[insert - 1] && b.Id < agents[neighbors[insert - 1]].Id))) insert--;
                    if (insert >= neighbors.Length) continue;
                    if (kept < neighbors.Length) kept++;
                    for (int j = kept - 1; j > insert; j--) { neighbors[j] = neighbors[j - 1]; distances[j] = distances[j - 1]; }
                    neighbors[insert] = candidate; distances[insert] = distance;
                }
            truncated = total > kept; return kept;
        }

        /// <summary>限制几何运算量级，拒绝会绕过浮点限速的巨大有限向量。</summary>
        private static bool Supported(Vector2 value) => Mathf.Abs(value.x) <= 1000000 && Mathf.Abs(value.y) <= 1000000;

        /// <summary>检查标量不是 NaN 或无穷。</summary>
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        /// <summary>检查二维向量的两个分量均有限。</summary>
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        /// <summary>检查要求严格为正的有限数值。</summary>
        private static bool Positive(float value) => Finite(value) && value > 0;
    }
}
