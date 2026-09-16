using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computerzhuxi.Navigation2D
{
    /// <summary>执行同步 A* 查询；实例复用搜索存储，不拥有角色或调用方的路径状态。</summary>
    public sealed class GridPathfinder2D
    {
        private static readonly Vector2Int[] Offsets = { new(1,0), new(-1,0), new(0,1), new(0,-1), new(1,1), new(1,-1), new(-1,1), new(-1,-1) };
        private readonly Dictionary<Vector2Int, int> indices = new();
        private readonly List<Node> nodes = new();
        private readonly List<int> heap = new();
        private readonly List<Vector2> reverse = new();
        private int used, expanded, queries;
        private bool searching;
        private GridSettings2D grid;
        private PathOptions2D options;
        private ITraversalSource2D source;
        private Vector2Int goal;

        /// <summary>搜索完整路径，覆盖输出列表；实例不可并发或重入使用，障碍源异常会向调用方传播。</summary>
        /// <param name="path">调用方拥有并复用的输出列表；失败与异常时保持为空。</param>
        /// <returns>成功或明确失败原因，以及已执行的工作量。</returns>
        public PathResult2D FindPath(Vector2 start, Vector2 destination, GridSettings2D grid,
            PathOptions2D options, ITraversalSource2D source, List<Vector2> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (searching) throw new InvalidOperationException("同一寻路器不能重入或并发查询。");
            path.Clear();
            expanded = queries = 0;
            if (source == null || !Finite(start) || !Finite(destination) || !Finite(grid.Origin)
                || !Finite(grid.CellSize) || grid.CellSize <= 0 || !Finite(options.Radius) || options.Radius < 0
                || options.MaxExpandedNodes < 1 || options.MaxExpandedNodes > 1000000
                || (options.Directions != GridDirections.Four && options.Directions != GridDirections.Eight)
                || !ValidCell(start, grid) || !ValidCell(destination, grid)) return Result(PathStatus.InvalidInput);
            searching = true;
            this.grid = grid; this.options = options; this.source = source;
            try
            {
                if (!PositionClear(start)) return Result(PathStatus.StartBlocked);
                if (!PositionClear(destination)) return Result(PathStatus.DestinationBlocked);
                Vector2Int first = grid.WorldToCell(start);
                goal = grid.WorldToCell(destination);
                // 同格也可能被薄墙切开，不能仅凭两端可站立就成功。
                if (first == goal)
                {
                    if (!SegmentClear(start, destination)) return Result(PathStatus.Unreachable);
                    path.Add(destination);
                    return Result(PathStatus.Success);
                }
                if (!PositionClear(grid.CellToWorld(first)) || !SegmentClear(start, grid.CellToWorld(first)))
                    return Result(PathStatus.StartNotConnected);
                if (!PositionClear(grid.CellToWorld(goal)) || !SegmentClear(grid.CellToWorld(goal), destination))
                    return Result(PathStatus.DestinationNotConnected);
                indices.Clear(); heap.Clear(); reverse.Clear(); used = 0;
                int firstIndex = GetNode(first);
                nodes[firstIndex].Cost = 0;
                Push(firstIndex);
                while (heap.Count > 0)
                {
                    if (expanded == options.MaxExpandedNodes) return Result(PathStatus.BudgetExceeded);
                    int currentIndex = Pop();
                    Node current = nodes[currentIndex];
                    current.Closed = true;
                    expanded++;
                    if (current.Cell == goal)
                    {
                        BuildPath(currentIndex, start, destination, path);
                        return Result(PathStatus.Success);
                    }
                    for (int i = 0; i < (int)options.Directions; i++)
                    {
                        Vector2Int offset = Offsets[i], cell = current.Cell + offset;
                        // 坐标范围限制同时避免启发代价溢出；范围外节点视作图边界。
                        if (Math.Abs((long)cell.x) > 1000000 || Math.Abs((long)cell.y) > 1000000) continue;
                        if (indices.TryGetValue(cell, out int existing) && nodes[existing].Closed) continue;
                        Vector2 point = grid.CellToWorld(cell);
                        if (!PositionClear(point)) continue;
                        bool diagonal = offset.x != 0 && offset.y != 0;
                        if (diagonal && (!PositionClear(grid.CellToWorld(current.Cell + new Vector2Int(offset.x, 0)))
                            || !PositionClear(grid.CellToWorld(current.Cell + new Vector2Int(0, offset.y))))) continue;
                        if (!SegmentClear(grid.CellToWorld(current.Cell), point)) continue;
                        int index = GetNode(cell);
                        Node next = nodes[index];
                        int cost = current.Cost + (diagonal ? 14 : 10);
                        if (cost >= next.Cost) continue;
                        next.Cost = cost; next.Parent = currentIndex;
                        if (next.HeapIndex < 0) Push(index); else Rise(next.HeapIndex);
                    }
                }
                return Result(PathStatus.Unreachable);
            }
            catch { path.Clear(); throw; }
            finally { this.source = null; searching = false; }
        }

        /// <summary>累计查询次数并询问当前占位净空。</summary>
        private bool PositionClear(Vector2 point) { queries++; return source.IsPositionClear(point, options.Radius); }
        /// <summary>累计查询次数并检查完整边的净空。</summary>
        private bool SegmentClear(Vector2 from, Vector2 to) { queries++; return source.IsSegmentClear(from, to, options.Radius); }
        /// <summary>保存当前搜索统计快照。</summary>
        private PathResult2D Result(PathStatus status) => new(status, expanded, queries);
        /// <summary>拒绝非有限数值。</summary>
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        /// <summary>验证二维坐标的两个分量。</summary>
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        /// <summary>在整数转换之前验证网格坐标范围。</summary>
        private static bool ValidCell(Vector2 point, GridSettings2D grid)
        {
            Vector2 cell = (point - grid.Origin) / grid.CellSize;
            return Finite(cell) && Mathf.Abs(cell.x) <= 1000000 && Mathf.Abs(cell.y) <= 1000000;
        }
        /// <summary>取得本次节点记录，复用历史分配而不保留历史通行事实。</summary>
        private int GetNode(Vector2Int cell)
        {
            if (indices.TryGetValue(cell, out int index)) return index;
            index = used++;
            if (index == nodes.Count) nodes.Add(new Node());
            Node node = nodes[index];
            node.Cell = cell; node.Cost = int.MaxValue; node.Parent = -1; node.HeapIndex = -1; node.Closed = false;
            int dx = Math.Abs(cell.x - goal.x), dy = Math.Abs(cell.y - goal.y);
            node.Heuristic = options.Directions == GridDirections.Four ? 10 * (dx + dy) : 14 * Math.Min(dx,dy) + 10 * Math.Abs(dx-dy);
            indices.Add(cell, index);
            return index;
        }
        /// <summary>比较总代价、启发代价和首次发现顺序，保证稳定的平局处理。</summary>
        private bool Before(int left, int right)
        {
            Node a = nodes[left], b = nodes[right];
            int total = (a.Cost + a.Heuristic).CompareTo(b.Cost + b.Heuristic);
            return total < 0 || total == 0 && (a.Heuristic < b.Heuristic || a.Heuristic == b.Heuristic && left < right);
        }
        /// <summary>把节点加入可原位降低优先级的最小堆。</summary>
        private void Push(int index) { nodes[index].HeapIndex = heap.Count; heap.Add(index); Rise(heap.Count - 1); }
        /// <summary>交换堆元素并同步其位置索引。</summary>
        private void Swap(int a, int b)
        { int value = heap[a]; heap[a] = heap[b]; heap[b] = value; nodes[heap[a]].HeapIndex = a; nodes[heap[b]].HeapIndex = b; }
        /// <summary>代价降低后向堆顶调整节点。</summary>
        private void Rise(int index)
        { while (index > 0) { int parent = (index - 1) / 2; if (!Before(heap[index], heap[parent])) break; Swap(index,parent); index = parent; } }
        /// <summary>弹出最佳节点并恢复最小堆。</summary>
        private int Pop()
        {
            int result = heap[0], last = heap.Count - 1;
            Swap(0,last); heap.RemoveAt(last); nodes[result].HeapIndex = -1;
            int index = 0;
            while (index * 2 + 1 < heap.Count)
            {
                int child = index * 2 + 1;
                if (child + 1 < heap.Count && Before(heap[child+1],heap[child])) child++;
                if (!Before(heap[child],heap[index])) break;
                Swap(index,child); index = child;
            }
            return result;
        }
        /// <summary>保留已验证的网格中心连接段，追加精确终点而不替换末段。</summary>
        private void BuildPath(int index, Vector2 start, Vector2 destination, List<Vector2> path)
        {
            reverse.Clear();
            while (index >= 0) { reverse.Add(grid.CellToWorld(nodes[index].Cell)); index = nodes[index].Parent; }
            for (int i = reverse.Count - 1; i >= 0; i--) if (reverse[i] != start || i != reverse.Count - 1) path.Add(reverse[i]);
            if (path.Count == 0 || path[path.Count-1] != destination) path.Add(destination);
        }
        /// <summary>复用单个网格节点的搜索记录。</summary>
        private sealed class Node
        { public Vector2Int Cell; public int Cost, Heuristic, Parent, HeapIndex; public bool Closed; }
    }
}
