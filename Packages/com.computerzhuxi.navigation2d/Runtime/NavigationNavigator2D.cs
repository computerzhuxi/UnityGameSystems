using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Computerzhuxi.Navigation2D
{
    /// <summary>自动根据真实位置推进唯一导航代理；只输出只读路径与移动建议，不写位置或刚体。</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    [AddComponentMenu("Navigation 2D/Navigation Navigator 2D")]
    public sealed class NavigationNavigator2D : MonoBehaviour
    {
        [Header("目标（运行时修改立即生效）")]
        [SerializeField, Tooltip("可选跟踪目标；也可调用 SetDestination 设置固定目的地。Stop 会清除此引用。")] private Transform target;
        [Header("身体与障碍（修改后重新规划）")]
        [SerializeField, Tooltip("用于推导世界空间保守圆形净空，不负责移动；建议居中圆形碰撞体。自身层不能属于障碍层。")] private Collider2D bodyCollider;
        [SerializeField, Min(0), Tooltip("没有指定身体时使用的世界空间半径。")] private float radius = .2f;
        [SerializeField, Min(0)] private float clearance = .02f;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private bool includeTriggers;
        [Header("网格与预算（修改后重新规划）")]
        [SerializeField] private Vector2 gridOrigin;
        [SerializeField, Min(.01f)] private float cellSize = .5f;
        [SerializeField, Range(1, 1000000)] private int maxExpandedNodes = 4096;
        [SerializeField] private GridDirections directions = GridDirections.Eight;
        [Header("跟随与重算（修改后重新规划）")]
        [SerializeField, Min(.001f)] private float arrivalDistance = .08f;
        [SerializeField, Min(.001f)] private float destinationChangeThreshold = .2f;
        [SerializeField, Min(0)] private float repathInterval = .1f;
        [SerializeField, Min(.001f)] private float failureRetryInterval = .25f;
        [SerializeField, Min(0)] private float lookAheadDistance = 2;
        [SerializeField] private bool drawPath = true;
        private NavigationAgent2D agent;
        private PhysicsTraversalSource2D source;
        private PhysicsScene2D physicsScene;
        private Transform observedTarget;
        private bool trackingTarget, paused;
        private Settings applied;
        private static readonly IReadOnlyList<Vector2> EmptyPath = Array.AsReadOnly(Array.Empty<Vector2>());

        /// <summary>保存上次应用的值类型配置，逐字段比较以免哈希碰撞漏掉配置变化。</summary>
        private struct Settings : IEquatable<Settings>
        {
            internal Vector2 Origin;
            internal float Cell, Radius, Arrival, Threshold, Repath, Retry, LookAhead;
            internal int Budget, Mask;
            internal GridDirections Directions;
            internal bool Triggers;
            /// <summary>逐项判断影响路径查询的全部配置是否相同。</summary>
            public bool Equals(Settings other) => Origin == other.Origin && Cell == other.Cell && Radius == other.Radius
                && Arrival == other.Arrival && Threshold == other.Threshold && Repath == other.Repath && Retry == other.Retry
                && LookAhead == other.LookAhead && Budget == other.Budget && Mask == other.Mask
                && Directions == other.Directions && Triggers == other.Triggers;
        }

        public IReadOnlyList<Vector2> CurrentPath => agent?.CurrentPath ?? EmptyPath;
        public int CurrentPathIndex => agent?.CurrentPathIndex ?? 0;
        public bool HasDestination => agent != null && agent.HasDestination;
        public Vector2 Destination => agent?.Destination ?? Vector2.zero;
        public bool IsPaused => paused;
        public bool HasPath => agent != null && agent.HasPath;
        public NavigationAgentState2D State => agent?.State ?? NavigationAgentState2D.Idle;
        public PathResult2D? LastResult => agent?.LastResult;
        public Vector2 DesiredDirection => isActiveAndEnabled && ConfigurationError == null ? agent?.DesiredDirection ?? Vector2.zero : Vector2.zero;
        public float RemainingWaypointDistance => agent?.RemainingWaypointDistance ?? 0;
        public float EffectiveRadius => applied.Radius;
        public Collider2D BodyCollider => bodyCollider;
        /// <summary>返回身体刚体的真实位置；没有附属刚体时使用导航物体的位置。</summary>
        public Vector2 Position => bodyCollider != null && bodyCollider.attachedRigidbody != null
            ? bodyCollider.attachedRigidbody.position : (Vector2)transform.position;
        public string ConfigurationError { get; private set; }
        public int QueryCount => agent?.QueryCount ?? 0;

        /// <summary>在 Inspector 添加时自动找到身体，仅用于初始配置，不擅自改变碰撞层。</summary>
        private void Reset() { bodyCollider = GetComponent<Collider2D>(); }
        /// <summary>恢复时重新验证路径；显式 Pause 不被组件启用动作解除。</summary>
        private void OnEnable()
        {
            if (agent == null) return;
            if (!paused) agent.Resume();
            agent.InvalidatePath();
        }
        /// <summary>禁用后清除运动建议，保留任务供重新启用使用。</summary>
        private void OnDisable() { agent?.Pause(); }
        /// <summary>在运动器之前读取 Inspector 变化并依据真实位置推进代理。</summary>
        private void FixedUpdate()
        {
            if (!RefreshConfiguration()) return;
            if (target != observedTarget || (!ReferenceEquals(observedTarget, null) && observedTarget == null))
            {
                observedTarget = target;
                trackingTarget = target != null;
                agent.Stop();
            }
            if (trackingTarget)
            {
                if (target == null || !target.gameObject.activeInHierarchy) { agent.Stop(); return; }
                agent.UpdateDestination(target.position);
            }
            agent.Tick(Position, Time.fixedDeltaTime, source);
        }
        /// <summary>设置固定目标并解除 Transform 跟踪；任务仅由代理拥有。</summary>
        public void SetDestination(Vector2 destination)
        {
            if (!RefreshConfiguration()) throw new InvalidOperationException(ConfigurationError);
            agent.SetDestination(destination);
            target = observedTarget = null; trackingTarget = false;
        }
        /// <summary>开始跟踪目标，空目标等价于停止；目标坐标在下个固定帧读取。</summary>
        public void SetTarget(Transform value)
        {
            Stop(); target = value;
        }
        /// <summary>停止并移除跟踪目标，防止下一帧自动恢复旧任务。</summary>
        public void Stop() { target = observedTarget = null; trackingTarget = false; agent?.Stop(); }
        /// <summary>暂停任务，保持目的地与只读路径，不停止其他组件拥有的刚体。</summary>
        public void Pause() { paused = true; agent?.Pause(); }
        /// <summary>恢复并失效旧路径，重新验证暂停期间发生的地图或位置变化。</summary>
        public void Resume() { paused = false; if (isActiveAndEnabled) agent?.Resume(); agent?.InvalidatePath(); }
        /// <summary>已知传送或地图变动时立即丢弃旧路径。</summary>
        public void InvalidatePath() { agent?.InvalidatePath(); }
        /// <summary>供运动器验证本步线段；组件失效、暂停或配置错误时拒绝执行。</summary>
        public bool IsMovementClear(Vector2 start, Vector2 end)
        {
            return isActiveAndEnabled && !paused && ConfigurationError == null && source != null
                && source.IsSegmentClear(start, end, applied.Radius);
        }
        /// <summary>读取实时配置，变化时重建查询配置并把唯一任务转交新代理，不保存第二份路径。</summary>
        private bool RefreshConfiguration()
        {
            try
            {
                if (!Finite(radius) || radius < 0 || !Finite(clearance) || clearance < 0)
                    throw new ArgumentException("Radius 与 Clearance 必须为有限非负数。");
                float extent = radius;
                if (bodyCollider != null)
                {
                    if (!bodyCollider.enabled || !bodyCollider.gameObject.activeInHierarchy || bodyCollider.isTrigger)
                        throw new ArgumentException("身体碰撞体必须启用且不是 Trigger。");
                    if (bodyCollider.transform != transform && !bodyCollider.transform.IsChildOf(transform))
                        throw new ArgumentException("身体碰撞体必须位于当前物体或其子物体。");
                    if ((obstacleMask.value & (1 << bodyCollider.gameObject.layer)) != 0)
                        throw new ArgumentException("障碍层不能包含身体自身的 Layer。");
                    var bounds = bodyCollider.bounds;
                    // 用同一身体锚点计算保守圆；常见中心对称形状以局部偏移求中心，避免插值时混用物理/视觉位置。
                    Transform anchor = bodyCollider.attachedRigidbody != null ? bodyCollider.attachedRigidbody.transform : transform;
                    Vector2 center = bodyCollider is CircleCollider2D || bodyCollider is BoxCollider2D || bodyCollider is CapsuleCollider2D
                        ? bodyCollider.transform.TransformPoint(bodyCollider.offset) : bounds.center;
                    Vector2 offset = center - (Vector2)anchor.position;
                    extent = (new Vector2(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) + (Vector2)bounds.extents).magnitude;
                    if (bodyCollider is CircleCollider2D circle && circle.offset == Vector2.zero && bodyCollider.transform == anchor)
                        extent = circle.radius * Mathf.Max(Mathf.Abs(anchor.lossyScale.x), Mathf.Abs(anchor.lossyScale.y));
                }
                var next = new Settings { Origin = gridOrigin, Cell = cellSize, Radius = extent + clearance,
                    Arrival = arrivalDistance, Threshold = destinationChangeThreshold, Repath = repathInterval,
                    Retry = failureRetryInterval, LookAhead = lookAheadDistance, Budget = maxExpandedNodes,
                    Directions = directions, Mask = obstacleMask, Triggers = includeTriggers };
                var currentScene = gameObject.scene.GetPhysicsScene2D();
                if (agent == null || !applied.Equals(next) || physicsScene != currentScene || ConfigurationError != null)
                {
                    var replacement = new NavigationAgent2D(new(gridOrigin, cellSize), new(next.Radius, maxExpandedNodes, directions),
                        new(arrivalDistance, destinationChangeThreshold, repathInterval, failureRetryInterval, lookAheadDistance));
                    if (agent != null && agent.HasDestination) replacement.SetDestination(agent.Destination);
                    if (paused || !isActiveAndEnabled) replacement.Pause();
                    source = new PhysicsTraversalSource2D(currentScene, obstacleMask, includeTriggers);
                    agent = replacement; applied = next; physicsScene = currentScene;
                }
                ConfigurationError = null; return true;
            }
            catch (ArgumentException exception)
            {
                ConfigurationError = exception.Message;
                agent?.InvalidatePath(); agent?.Pause();
                return false;
            }
        }
        /// <summary>判断 Inspector 数值有限性，不用静默钳制隐藏配置错误。</summary>
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        /// <summary>显示当前唯一代理的剩余路径，不参与运行时推进。</summary>
        private void OnDrawGizmosSelected()
        {
            if (!drawPath || agent == null) return;
            Gizmos.color = Color.cyan; Vector3 previous = Position;
            for (int i = agent.CurrentPathIndex; i < agent.CurrentPath.Count; i++)
            {
                Vector3 next = agent.CurrentPath[i]; Gizmos.DrawLine(previous, next);
                Gizmos.DrawWireSphere(next, applied.Radius); previous = next;
            }
        }
    }
}
