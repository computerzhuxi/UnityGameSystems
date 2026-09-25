using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computerzhuxi.Perception2D
{
    /// <summary>在指定二维物理场景中执行一次视觉采样，不修改核心记忆。</summary>
    public class PhysicsSightScanner2D
    {
        private readonly List<Collider2D> colliders = new(16);
        private readonly List<RaycastHit2D> hits = new(8);
        private readonly HashSet<PerceptionTarget2D> seen = new();
        private readonly HashSet<PerceptionTargetHandle> current = new();

        /// <summary>完整扫描一次并填充调用方缓冲；派生扫描器可以替换默认物理实现。</summary>
        public virtual void Scan(PhysicsScene2D scene, PerceptionTargetRegistry registry, PerceptionSettings2D settings,
            Vector2 origin, Vector2 facing, Transform owner, IReadOnlyList<TargetPerceptionInfo> observations,
            List<SightObservation2D> results)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (results == null) throw new ArgumentNullException(nameof(results));
            Guard.Vector(origin);
            Guard.Vector(facing);
            if (!scene.IsValid()) throw new ArgumentException("二维物理场景无效。", nameof(scene));

            results.Clear();
            seen.Clear();
            current.Clear();
            foreach (TargetPerceptionInfo item in observations)
                if (item.IsVisible) current.Add(item.Handle);

            var targetFilter = new ContactFilter2D();
            targetFilter.SetLayerMask(settings.TargetLayers);
            targetFilter.useTriggers = true;
            scene.OverlapCircle(origin, settings.LoseSightDistance, targetFilter, colliders);
            foreach (Collider2D collider in colliders)
            {
                if (collider == null) continue;
                PerceptionTarget2D target = collider.GetComponentInParent<PerceptionTarget2D>();
                if (target == null || !target.IsRegistered || !target.SightDetectable ||
                    !ReferenceEquals(target.Handle.Registry, registry) || target.transform.IsChildOf(owner) ||
                    target.TargetRoot == owner || !seen.Add(target)) continue;

                Vector2 point = target.SightPosition;
                Vector2 offset = point - origin;
                float radius = current.Contains(target.Handle) ? settings.LoseSightDistance : settings.SightDistance;
                if (offset.sqrMagnitude > radius * radius) continue;
                if (offset.sqrMagnitude > 0.000001f && Vector2.Angle(facing, offset) > settings.ViewAngle * 0.5f) continue;
                if (IsBlocked(scene, settings.ObstacleLayers, origin, point, owner, target)) continue;
                results.Add(new SightObservation2D(target.Handle, point, origin));
            }
        }

        /// <summary>仅在同一物理场景内检查障碍，并忽略观察者及目标自己的碰撞体。</summary>
        private bool IsBlocked(PhysicsScene2D scene, LayerMask obstacleLayers, Vector2 origin, Vector2 point,
            Transform owner, PerceptionTarget2D target)
        {
            if (obstacleLayers.value == 0) return false;
            var obstacleFilter = new ContactFilter2D();
            obstacleFilter.SetLayerMask(obstacleLayers);
            obstacleFilter.useTriggers = false;
            scene.Linecast(origin, point, obstacleFilter, hits);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(owner) ||
                    hit.collider.GetComponentInParent<PerceptionTarget2D>() == target) continue;
                return true;
            }
            return false;
        }
    }
}
