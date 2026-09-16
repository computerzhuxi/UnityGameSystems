using UnityEngine;

namespace Computerzhuxi.Navigation2D
{
    /// <summary>在明确的二维物理场景内以圆形占位检查障碍，不读取全局 Trigger 开关。</summary>
    public sealed class PhysicsTraversalSource2D : ITraversalSource2D
    {
        private readonly PhysicsScene2D scene;
        private readonly ContactFilter2D filter;
        private readonly Collider2D[] overlaps = new Collider2D[1];
        private readonly RaycastHit2D[] hits = new RaycastHit2D[1];
        /// <summary>选择障碍层与 Trigger 规则；构造后固定，层级变化时由适配层替换实例。</summary>
        public PhysicsTraversalSource2D(PhysicsScene2D scene, LayerMask obstacleMask, bool includeTriggers = false)
        {
            this.scene = scene;
            filter = new ContactFilter2D { useLayerMask = true, layerMask = obstacleMask, useTriggers = includeTriggers };
        }
        /// <summary>检测当前圆形占位是否与任一被选中的障碍重叠。</summary>
        public bool IsPositionClear(Vector2 position, float radius)
        { return scene.IsValid() && scene.OverlapCircle(position, radius, filter, overlaps) == 0; }
        /// <summary>先检查两端，再进行圆形扫掠；只需一个命中即可判定受阻，不截断过滤结果。</summary>
        public bool IsSegmentClear(Vector2 start, Vector2 end, float radius)
        {
            if (!IsPositionClear(start,radius) || !IsPositionClear(end,radius)) return false;
            Vector2 delta = end - start;
            float distance = delta.magnitude;
            if (distance == 0) return true;
            return scene.CircleCast(start,radius,delta / distance,distance,filter,hits) == 0;
        }
    }
}
