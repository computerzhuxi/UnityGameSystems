using UnityEngine;

namespace Computerzhuxi.Navigation2D
{
    /// <summary>调用方在同一时刻采集的圆形代理快照；Group 不同的代理互不影响。</summary>
    public readonly struct AvoidanceAgent2D
    {
        public int Id { get; }
        public int Group { get; }
        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public Vector2 PreferredVelocity { get; }
        public float Radius { get; }
        public float MaxSpeed { get; }
        public bool Locked { get; }
        /// <summary>创建快照；实际速度来自上一步真实运动，锁定代理由外部控制且不承担互惠修正。</summary>
        public AvoidanceAgent2D(int id, int group, Vector2 position, Vector2 velocity,
            Vector2 preferredVelocity, float radius, float maxSpeed, bool locked = false)
        {
            Id = id; Group = group; Position = position; Velocity = velocity;
            PreferredVelocity = preferredVelocity; Radius = radius; MaxSpeed = maxSpeed; Locked = locked;
        }
    }

    /// <summary>同序输出的建议速度；截断或不可行时不提供无碰撞承诺，调用方仍负责环境检查与移动。</summary>
    public readonly struct AvoidanceResult2D
    {
        public int Id { get; }
        public Vector2 Velocity { get; }
        public bool TruncatedNeighbors { get; }
        public bool Infeasible { get; }
        /// <summary>记录建议速度和本步约束诊断。</summary>
        public AvoidanceResult2D(int id, Vector2 velocity, bool truncatedNeighbors, bool infeasible)
        { Id = id; Velocity = velocity; TruncatedNeighbors = truncatedNeighbors; Infeasible = infeasible; }
    }
}
