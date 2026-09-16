using System;
using UnityEngine;

namespace Computerzhuxi.Navigation2D
{
    /// <summary>描述一次完整路径查询的结论；失败时不提供部分路径。</summary>
    public enum PathStatus { Success, InvalidInput, StartBlocked, DestinationBlocked, StartNotConnected, DestinationNotConnected, Unreachable, BudgetExceeded }
    /// <summary>选择网格相邻节点的方向数量。</summary>
    public enum GridDirections { Four = 4, Eight = 8 }

    /// <summary>提供当前位置和整段移动的净空事实；调用期间应保持障碍状态稳定。</summary>
    public interface ITraversalSource2D
    {
        /// <summary>判断指定圆形占位是否完全避开障碍。</summary>
        bool IsPositionClear(Vector2 position, float radius);
        /// <summary>判断圆形占位沿完整线段移动时是否避开障碍，包含两端。</summary>
        bool IsSegmentClear(Vector2 start, Vector2 end, float radius);
    }

    /// <summary>定义世界空间中的最近中心网格；半格采用 Unity 的偶数取整规则。</summary>
    public readonly struct GridSettings2D
    {
        public Vector2 Origin { get; }
        public float CellSize { get; }
        /// <summary>建立显式原点和格距，不静默修正非法配置。</summary>
        public GridSettings2D(Vector2 origin, float cellSize) { Origin = origin; CellSize = cellSize; }
        /// <summary>把世界位置转换为最近的网格中心坐标。</summary>
        public Vector2Int WorldToCell(Vector2 position) => Vector2Int.RoundToInt((position - Origin) / CellSize);
        /// <summary>把网格坐标转换为世界空间中心。</summary>
        public Vector2 CellToWorld(Vector2Int cell) => Origin + (Vector2)cell * CellSize;
    }

    /// <summary>保存每次请求的净空、展开预算和方向规则。</summary>
    public readonly struct PathOptions2D
    {
        public float Radius { get; }
        public int MaxExpandedNodes { get; }
        public GridDirections Directions { get; }
        /// <summary>创建请求选项；八方向始终禁止从任一受阻正交邻格旁切角。</summary>
        public PathOptions2D(float radius, int maxExpandedNodes = 4096, GridDirections directions = GridDirections.Eight)
        { Radius = radius; MaxExpandedNodes = maxExpandedNodes; Directions = directions; }
    }

    /// <summary>返回失败原因和本次工作量；路径由调用方列表唯一保存。</summary>
    public readonly struct PathResult2D
    {
        public PathStatus Status { get; }
        public bool Succeeded => Status == PathStatus.Success;
        public int ExpandedNodes { get; }
        public int TraversalQueries { get; }
        /// <summary>构造不可变的查询结果。</summary>
        internal PathResult2D(PathStatus status, int expanded, int queries)
        { Status = status; ExpandedNodes = expanded; TraversalQueries = queries; }
    }
}
