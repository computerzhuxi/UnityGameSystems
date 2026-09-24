using UnityEngine;

namespace Computerzhuxi.Navigation2D.Samples
{
    /// <summary>仅供 QuickStart 演示的运动适配器，不是正式 Runtime/API；实际项目应使用自己的运动系统。</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "Computerzhuxi.Navigation2D", sourceAssembly: "Computerzhuxi.Navigation2D", sourceClassName: "NavigationRigidbody2DMover")]
    [DisallowMultipleComponent, RequireComponent(typeof(NavigationNavigator2D), typeof(Rigidbody2D))]
    [AddComponentMenu("Navigation 2D Samples/Quick Start Example Mover 2D")]
    public sealed class QuickStartExampleMover2D : MonoBehaviour
    {
        [SerializeField, Min(0), Tooltip("世界单位/秒；运行时修改立即生效。")] private float speed = 2;
        [SerializeField, Min(0), Tooltip("实际身体扫掠的额外距离；运行时修改立即生效。")] private float collisionMargin = .01f;
        private NavigationNavigator2D navigator;
        private Rigidbody2D body;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private bool ownsMotion;
        public string ConfigurationError { get; private set; }
        public bool IsBlocked { get; private set; }
        public Vector2 AppliedVelocity { get; private set; }

        /// <summary>缓存同物体的组件，不自动改动已有刚体类型或项目物理设置。</summary>
        private void Awake() { navigator = GetComponent<NavigationNavigator2D>(); body = GetComponent<Rigidbody2D>(); }
        /// <summary>每固定帧消费只读建议，检查实际身体，再通过唯一运动入口提交位移。</summary>
        private void FixedUpdate()
        {
            IsBlocked = false; AppliedVelocity = Vector2.zero;
            ConfigurationError = ValidateConfiguration();
            if (ConfigurationError != null) { Halt(); return; }
            ownsMotion = true;
            if (!navigator.isActiveAndEnabled || navigator.IsPaused || navigator.DesiredDirection == Vector2.zero)
            { Halt(); return; }
            float dt = Time.fixedDeltaTime;
            Vector2 delta = navigator.DesiredDirection * Mathf.Min(speed * dt, navigator.RemainingWaypointDistance);
            Vector2 from = body.position;
            // Kinematic 不会被静态墙自动挡住；同时验证路径净空与实际附属碰撞体扫掠。
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = Physics2D.GetLayerCollisionMask(gameObject.layer) };
            int count = body.Cast(delta.normalized, filter, hits, delta.magnitude + collisionMargin);
            bool blocked = count == hits.Length;
            for (int i = 0; i < count; i++)
                if (Vector2.Dot(delta, hits[i].normal) < -.000001f) blocked = true;
            if (blocked || !navigator.IsMovementClear(navigator.Position, navigator.Position + delta))
            { IsBlocked = true; Halt(); return; }
            body.angularVelocity = 0;
            body.MovePosition(from + delta);
            AppliedVelocity = dt > 0 ? delta / dt : Vector2.zero;
        }
        /// <summary>拒绝不支持的运动所有权或身体配置，不把 Dynamic 静默改成 Kinematic。</summary>
        private string ValidateConfiguration()
        {
            if (body.bodyType != RigidbodyType2D.Kinematic) return "示例运动器需要 Kinematic Rigidbody2D；其他运动方式请使用自己的运动器。";
            if (!body.simulated) return "Rigidbody2D 必须启用 Simulated。";
            if ((body.constraints & (RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY)) != 0)
                return "示例运动器不支持冻结平移轴。";
            if (navigator.BodyCollider == null || navigator.BodyCollider.attachedRigidbody != body)
                return "请给导航组件指定属于此 Rigidbody2D 的身体碰撞体。";
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0
                || float.IsNaN(collisionMargin) || float.IsInfinity(collisionMargin) || collisionMargin < 0)
                return "速度与碰撞余量必须为有限非负数。";
            return null;
        }
        /// <summary>仅停止本组件已经接管的 Kinematic 运动；不清除其他类型刚体的外力。</summary>
        private void Halt()
        {
            AppliedVelocity = Vector2.zero;
            if (!ownsMotion || body == null) return;
            if (body.bodyType == RigidbodyType2D.Kinematic)
            {
                body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
                // 覆盖本帧可能尚未模拟的 MovePosition 请求，避免禁用后仍前进一步。
                if (body.simulated) body.MovePosition(body.position);
            }
            ownsMotion = false;
        }
        /// <summary>禁用时立即撤销本组件尚未执行的运动，导航任务仍归导航组件所有。</summary>
        private void OnDisable() { Halt(); }
    }
}
