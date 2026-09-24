using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computerzhuxi.Navigation2D
{
    /// <summary>描述跟随状态；暂停独立于状态，查询失败原因通过 LastResult 读取。</summary>
    public enum NavigationAgentState2D { Idle, Pending, Following, Arrived, Failed, DomainUnavailable }

    /// <summary>保存跟随容差、移动目标阈值及成功和失败请求的最小间隔。</summary>
    public readonly struct AgentSettings2D
    {
        public float ArrivalDistance { get; }
        public float DestinationChangeThreshold { get; }
        public float RepathInterval { get; }
        public float FailureRetryInterval { get; }
        /// <summary>可选安全前视距离；零保留逐路径点跟随。</summary>
        public float LookAheadDistance { get; }
        /// <summary>创建明确的跟随配置；正容差避免浮点终点永远无法到达，失败间隔必须大于零。</summary>
        public AgentSettings2D(float arrivalDistance, float destinationChangeThreshold,
            float repathInterval = .1f, float failureRetryInterval = .25f, float lookAheadDistance = 0)
        {
            if (!ValidPositive(arrivalDistance) || !ValidPositive(destinationChangeThreshold)
                || !ValidPositive(failureRetryInterval) || float.IsNaN(repathInterval)
                || float.IsInfinity(repathInterval) || repathInterval < 0
                || float.IsNaN(lookAheadDistance) || float.IsInfinity(lookAheadDistance) || lookAheadDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(arrivalDistance), "导航跟随配置必须是有限的有效数值。");
            ArrivalDistance = arrivalDistance;
            DestinationChangeThreshold = destinationChangeThreshold;
            RepathInterval = repathInterval;
            FailureRetryInterval = failureRetryInterval; LookAheadDistance = lookAheadDistance;
        }
        /// <summary>检查要求严格大于零的有限参数。</summary>
        private static bool ValidPositive(float value) => value > 0 && !float.IsInfinity(value);
    }

    /// <summary>唯一管理二维导航意图、路径和进度；由调用方提供位置与障碍域并执行移动，不写入任何场景对象。</summary>
    public sealed class NavigationAgent2D
    {
        private readonly GridPathfinder2D finder = new();
        private readonly GridSettings2D grid;
        private readonly PathOptions2D options;
        private readonly AgentSettings2D settings;
        private readonly List<Vector2> path = new();
        private ITraversalSource2D previousSource;
        private int domainRevision;
        private bool previousDomainValid, needsRepath, ticking;
        private Vector2 pathDestination;
        private float retryRemaining;

        public IReadOnlyList<Vector2> CurrentPath { get; }
        public int CurrentPathIndex { get; private set; }
        public bool HasDestination { get; private set; }
        public bool HasPath => CurrentPathIndex < path.Count;
        public bool IsPaused { get; private set; }
        public Vector2 Destination { get; private set; }
        public Vector2 DesiredDirection { get; private set; }
        /// <summary>当前安全路径点的距离；执行器可用它限制单步位移，防止跨过拐点。</summary>
        public float RemainingWaypointDistance { get; private set; }
        public NavigationAgentState2D State { get; private set; }
        /// <summary>最近一次查询结果；尚未查询、停止、域失效或源异常时为空，不能误读为成功。</summary>
        public PathResult2D? LastResult { get; private set; }
        public int QueryCount { get; private set; }

        /// <summary>创建单角色跟随状态；不共享寻路器工作区，不允许并发或源回调重入。</summary>
        public NavigationAgent2D(GridSettings2D grid, PathOptions2D options, AgentSettings2D settings)
        {
            if (!Finite(grid.Origin) || !Finite(grid.CellSize) || grid.CellSize <= 0
                || !Finite(options.Radius) || options.Radius < 0 || options.MaxExpandedNodes < 1
                || options.MaxExpandedNodes > 1000000
                || (options.Directions != GridDirections.Four && options.Directions != GridDirections.Eight)
                || settings.ArrivalDistance <= 0 || settings.FailureRetryInterval <= 0)
                throw new ArgumentException("导航网格、查询或跟随配置无效。");
            this.grid = grid; this.options = options; this.settings = settings;
            // 返回固定只读包装，调用者既不能修改底层列表，也不会在每帧产生包装分配。
            CurrentPath = path.AsReadOnly();
        }

        /// <summary>替换导航任务并立即失效旧路径；下一次有效 Tick 查询。相同目的地更新应使用 UpdateDestination。</summary>
        public void SetDestination(Vector2 destination)
        {
            EnsureMutable(); ValidatePosition(destination);
            Destination = destination; HasDestination = true; needsRepath = true;
            retryRemaining = 0; LastResult = null; ClearPath(); State = NavigationAgentState2D.Pending;
        }

        /// <summary>更新移动目标；相对最近查询终点累计偏移，避免每帧小位移永远达不到重算阈值。</summary>
        public void UpdateDestination(Vector2 destination)
        {
            EnsureMutable(); ValidatePosition(destination);
            if (!HasDestination) { SetDestination(destination); return; }
            Destination = destination;
            if ((destination - pathDestination).sqrMagnitude >= settings.DestinationChangeThreshold * settings.DestinationChangeThreshold)
                needsRepath = true;
        }

        /// <summary>显式失效当前路径并允许下一有效帧重算，用于传送或调用方已知的地图变更。</summary>
        public void InvalidatePath()
        {
            EnsureMutable(); ClearPath(); LastResult = null; retryRemaining = 0;
            needsRepath = HasDestination; State = HasDestination ? NavigationAgentState2D.Pending : NavigationAgentState2D.Idle;
        }

        /// <summary>暂停跟随与计时并清除移动建议，保留任务；恢复后先验证当前位置到路径点的安全性。</summary>
        public void Pause() { EnsureMutable(); IsPaused = true; ClearSuggestion(); }
        /// <summary>恢复任务；位移或地图发生不连续变化时调用方应同时显式失效路径。</summary>
        public void Resume() { EnsureMutable(); IsPaused = false; }
        /// <summary>移除任务和所有路径状态；保留暂停设置及累计诊断查询次数。</summary>
        public void Stop()
        {
            EnsureMutable(); HasDestination = false; needsRepath = false; retryRemaining = 0;
            LastResult = null; ClearPath(); State = NavigationAgentState2D.Idle;
        }

        /// <summary>根据真实位置产生安全移动建议；零时间等价于暂停本帧。障碍源或版本变化会立即丢弃旧路径。</summary>
        /// <param name="source">本帧稳定的障碍源；域不可用时允许为空。</param>
        /// <param name="revision">由接入方维护的空间域版本，不要求逐帧递增。</param>
        /// <param name="domainValid">层级或地图不可用时传 false，不把不可用误报为无障碍。</param>
        public void Tick(Vector2 position, float deltaTime, ITraversalSource2D source, int revision = 0, bool domainValid = true)
        {
            EnsureMutable(); ValidatePosition(position);
            if (!Finite(deltaTime) || deltaTime < 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (domainValid && source == null) throw new ArgumentNullException(nameof(source));
            ticking = true;
            try { TickCore(position, deltaTime, source, revision, domainValid); }
            catch
            {
                // 用户障碍回调异常必须向外传播，但不能留下上一帧仍可消费的移动建议。
                ClearPath(); LastResult = null; needsRepath = HasDestination;
                retryRemaining = settings.FailureRetryInterval;
                State = HasDestination ? NavigationAgentState2D.Failed : NavigationAgentState2D.Idle;
                throw;
            }
            finally { ticking = false; }
        }

        /// <summary>推进状态，先验证域与净空，再按节流规则查询，最后仅输出完整安全线段的方向。</summary>
        private void TickCore(Vector2 position, float deltaTime, ITraversalSource2D source, int revision, bool domainValid)
        {
            ClearSuggestion();
            if (!ReferenceEquals(source, previousSource) || revision != domainRevision || domainValid != previousDomainValid)
            {
                ClearPath(); LastResult = null; retryRemaining = 0; needsRepath = HasDestination;
                previousSource = source; domainRevision = revision; previousDomainValid = domainValid;
                State = HasDestination ? NavigationAgentState2D.Pending : NavigationAgentState2D.Idle;
            }
            if (!HasDestination) { State = NavigationAgentState2D.Idle; return; }
            if (!domainValid) { State = NavigationAgentState2D.DomainUnavailable; return; }
            if (IsPaused || deltaTime == 0) return;
            retryRemaining = Mathf.Max(0, retryRemaining - deltaTime);
            // 欧氏距离不足以证明到达：薄墙或失效目标占位必须继续交给路径查询判定。
            if (Vector2.Distance(position, Destination) <= settings.ArrivalDistance
                && source.IsSegmentClear(position, Destination, options.Radius))
            { ClearPath(); needsRepath = false; State = NavigationAgentState2D.Arrived; return; }
            if (HasPath && !source.IsSegmentClear(position, path[CurrentPathIndex], options.Radius))
            {
                // 动态封路立即停止；等待剩余间隔后重算，不能沿阻塞旧路径继续走。
                ClearPath(); needsRepath = true; State = NavigationAgentState2D.Pending;
            }
            if (!HasPath) needsRepath = true;
            if (needsRepath && retryRemaining <= 0) Rebuild(position, source);
            if (!HasPath) return;
            while (CurrentPathIndex < path.Count
                && Vector2.Distance(position, path[CurrentPathIndex]) <= settings.ArrivalDistance)
            {
                // 跳过容差内拐点之前验证新连线，避免切墙角。
                if (CurrentPathIndex + 1 < path.Count
                    && !source.IsSegmentClear(position, path[CurrentPathIndex + 1], options.Radius)) break;
                CurrentPathIndex++;
            }
            if (!HasPath) { State = NavigationAgentState2D.Pending; needsRepath = true; return; }
            if (!source.IsSegmentClear(position, path[CurrentPathIndex], options.Radius))
            { ClearPath(); needsRepath = true; State = NavigationAgentState2D.Pending; return; }
            if (settings.LookAheadDistance > 0)
            {
                int farthest = CurrentPathIndex;
                while (farthest + 1 < path.Count
                    && Vector2.Distance(position, path[farthest + 1]) <= settings.LookAheadDistance) farthest++;
                // 从最远候选反向验证，开阔区仅需一次扫掠；不能跨过未验证的墙角。
                // 进度仍由本代理唯一拥有，外部避让不会私自保存或修改路径索引。
                // Physics2D 长扫掠与短步端点重叠在贴角处存在接触容差差异；捷径额外留出余量。
                // 不能满足额外净空时保留原网格路径，不放宽查询本身的半径。
                float lookAheadRadius = options.Radius + Mathf.Max(.02f, options.Radius * .05f);
                int checks = 0;
                for (int candidate = farthest; candidate > CurrentPathIndex && checks < 8; candidate--, checks++)
                    if (source.IsSegmentClear(position, path[candidate], lookAheadRadius)) { CurrentPathIndex = candidate; break; }
            }
            Vector2 offset = path[CurrentPathIndex] - position;
            DesiredDirection = offset.normalized; RemainingWaypointDistance = offset.magnitude;
            State = NavigationAgentState2D.Following;
        }

        /// <summary>查询最新目的地，并只在验证整段净空后跳过可能位于身后的起点格中心。</summary>
        private void Rebuild(Vector2 position, ITraversalSource2D source)
        {
            ClearPath(); pathDestination = Destination; needsRepath = false; QueryCount++;
            LastResult = finder.FindPath(position, Destination, grid, options, source, path);
            retryRemaining = LastResult.Value.Succeeded ? settings.RepathInterval : settings.FailureRetryInterval;
            State = LastResult.Value.Succeeded ? NavigationAgentState2D.Following : NavigationAgentState2D.Failed;
            if (path.Count > 1 && source.IsSegmentClear(position, path[1], options.Radius)) CurrentPathIndex = 1;
        }
        /// <summary>清除路径及瞬时移动建议，不创建第二份路径快照。</summary>
        private void ClearPath() { path.Clear(); CurrentPathIndex = 0; ClearSuggestion(); }
        /// <summary>清除当前帧可消费的移动量。</summary>
        private void ClearSuggestion() { DesiredDirection = Vector2.zero; RemainingWaypointDistance = 0; }
        /// <summary>拒绝障碍源回调重入或修改正在计算的同一代理。</summary>
        private void EnsureMutable() { if (ticking) throw new InvalidOperationException("同一导航代理不允许重入。"); }
        /// <summary>拒绝非有限坐标，避免污染后续可恢复状态。</summary>
        private static void ValidatePosition(Vector2 value) { if (!Finite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
        /// <summary>检查二维坐标是否有限。</summary>
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        /// <summary>检查浮点数是否有限。</summary>
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
