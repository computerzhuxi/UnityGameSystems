/*
 * Adapted from RVO2-CS RVOCS/Agent.cs, commit a455da254cffd9ebb8d85f8eedb9d9332e69012a.
 * SPDX-FileCopyrightText: 2008 University of North Carolina at Chapel Hill
 * SPDX-License-Identifier: Apache-2.0
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software distributed
 * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
 * CONDITIONS OF ANY KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations under the License.
 * Changes: Unity Vector2, bounded reusable arrays, no simulator/obstacle objects,
 * locked-neighbor responsibility, deterministic coincident fallback and diagnostics.
 */
using System;
using UnityEngine;

namespace Computerzhuxi.Navigation2D.Internal
{
    /// <summary>改编 RVO2 的代理 ORCA 半平面与线性规划，不暴露第三方类型或持有模拟状态。</summary>
    internal sealed class OrcaSolver2D
    {
        private struct Line { internal Vector2 Point, Direction; }
        private const float Epsilon = .00001f;
        private readonly Line[] lines, projected;
        private int count;
        /// <summary>预留主约束与不可行回退的投影空间，避免热路径分配。</summary>
        internal OrcaSolver2D(int capacity) { lines = new Line[capacity]; projected = new Line[capacity]; }
        /// <summary>开始新代理的约束集合。</summary>
        internal void Clear() => count = 0;
        /// <summary>计算二维行列式，用符号判断半平面方向。</summary>
        internal static float Det(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>为一个邻居建立 ORCA 速度约束；不可自主调整的邻居由本代理承担全部责任。</summary>
        internal void Add(AvoidanceAgent2D a, AvoidanceAgent2D b, float horizon, float deltaTime)
        {
            Vector2 position = b.Position - a.Position, velocity = a.Velocity - b.Velocity;
            float distanceSq = position.sqrMagnitude, radius = a.Radius + b.Radius, radiusSq = radius * radius;
            Vector2 correction; Line line = default;
            if (distanceSq > radiusSq)
            {
                float inverse = 1 / horizon;
                Vector2 w = velocity - inverse * position;
                float wSq = w.sqrMagnitude, dot = Vector2.Dot(w, position);
                if (dot < 0 && dot * dot > radiusSq * wSq)
                {
                    float length = Mathf.Sqrt(wSq); Vector2 unit = w / length;
                    line.Direction = new Vector2(unit.y, -unit.x);
                    correction = (radius * inverse - length) * unit;
                }
                else
                {
                    float leg = Mathf.Sqrt(distanceSq - radiusSq);
                    if (Det(position, w) > 0)
                        line.Direction = new Vector2(position.x * leg - position.y * radius, position.x * radius + position.y * leg) / distanceSq;
                    else
                        line.Direction = -new Vector2(position.x * leg + position.y * radius, -position.x * radius + position.y * leg) / distanceSq;
                    correction = Vector2.Dot(velocity, line.Direction) * line.Direction - velocity;
                }
            }
            else
            {
                // 已重叠时按当前步长分离；完全重合用 ID 给双方相反方向，避免除零和排列依赖。
                float inverse = 1 / deltaTime; Vector2 w = velocity - inverse * position;
                float length = w.magnitude;
                Vector2 unit = length > Epsilon ? w / length : (a.Id < b.Id ? Vector2.left : Vector2.right);
                line.Direction = new Vector2(unit.y, -unit.x);
                correction = (radius * inverse - length) * unit;
            }
            line.Point = a.Velocity + (b.Locked ? 1 : .5f) * correction;
            if (!Finite(line.Point) || !Finite(line.Direction)) throw new ArithmeticException("ORCA 约束超出浮点范围。");
            lines[count++] = line;
        }

        /// <summary>优先求最近的可行速度；不可行时最小化违反量，并显式报告未满足约束。</summary>
        internal Vector2 Solve(Vector2 preferred, float speed, out bool infeasible)
        {
            int failed = Program2(lines, count, speed, preferred, false, out Vector2 result);
            if (failed < count) Program3(failed, speed, ref result);
            infeasible = false;
            for (int i = 0; i < count; i++)
                if (Det(lines[i].Direction, lines[i].Point - result) > Epsilon) { infeasible = true; break; }
            return result;
        }

        /// <summary>在指定直线上裁剪速度圆与先前半平面，求一维可行区间的最优点。</summary>
        private static bool Program1(Line[] constraints, int lineIndex, float radius, Vector2 preferred, bool directionOnly, ref Vector2 result)
        {
            Line current = constraints[lineIndex];
            float dot = Vector2.Dot(current.Point, current.Direction);
            float discriminant = dot * dot + radius * radius - current.Point.sqrMagnitude;
            if (discriminant < 0) return false;
            float root = Mathf.Sqrt(discriminant), left = -dot - root, right = -dot + root;
            for (int i = 0; i < lineIndex; i++)
            {
                float denominator = Det(current.Direction, constraints[i].Direction);
                float numerator = Det(constraints[i].Direction, current.Point - constraints[i].Point);
                if (Mathf.Abs(denominator) <= Epsilon)
                {
                    // 平行反向约束没有交点时，当前可行区间为空。
                    if (numerator < 0) return false;
                    continue;
                }
                float t = numerator / denominator;
                if (denominator >= 0) right = Mathf.Min(right, t); else left = Mathf.Max(left, t);
                if (left > right) return false;
            }
            float optimum = directionOnly ? (Vector2.Dot(preferred, current.Direction) > 0 ? right : left)
                : Mathf.Clamp(Vector2.Dot(current.Direction, preferred - current.Point), left, right);
            result = current.Point + optimum * current.Direction; return true;
        }

        /// <summary>依次加入半平面；新约束排除当前解时退化成该边界上的一维规划。</summary>
        private static int Program2(Line[] constraints, int length, float radius, Vector2 preferred, bool directionOnly, out Vector2 result)
        {
            result = directionOnly ? preferred * radius : Vector2.ClampMagnitude(preferred, radius);
            for (int i = 0; i < length; i++)
                if (Det(constraints[i].Direction, constraints[i].Point - result) > 0)
                {
                    Vector2 previous = result;
                    if (!Program1(constraints, i, radius, preferred, directionOnly, ref result)) { result = previous; return i; }
                }
            return length;
        }

        /// <summary>原约束无共同可行解时，用复用投影数组降低最大违反量，不声称恢复安全保证。</summary>
        private void Program3(int begin, float radius, ref Vector2 result)
        {
            float distance = 0;
            for (int i = begin; i < count; i++)
            {
                if (Det(lines[i].Direction, lines[i].Point - result) <= distance) continue;
                int length = 0;
                for (int j = 0; j < i; j++)
                {
                    Line line = default; float determinant = Det(lines[i].Direction, lines[j].Direction);
                    if (Mathf.Abs(determinant) <= Epsilon)
                    {
                        if (Vector2.Dot(lines[i].Direction, lines[j].Direction) > 0) continue;
                        line.Point = .5f * (lines[i].Point + lines[j].Point);
                    }
                    else line.Point = lines[i].Point + Det(lines[j].Direction, lines[i].Point - lines[j].Point) / determinant * lines[i].Direction;
                    line.Direction = (lines[j].Direction - lines[i].Direction).normalized; projected[length++] = line;
                }
                Vector2 previous = result;
                if (Program2(projected, length, radius, new Vector2(-lines[i].Direction.y, lines[i].Direction.x), true, out result) < length)
                    result = previous; // 投影问题的浮点误差可能导致失败，此时保留上一有限结果。
                distance = Det(lines[i].Direction, lines[i].Point - result);
            }
        }

        /// <summary>拒绝溢出或无定义的约束，避免以有限默认速度掩盖数值失败。</summary>
        private static bool Finite(Vector2 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y)
            && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);
    }
}
