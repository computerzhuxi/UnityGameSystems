using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computerzhuxi.Perception2D
{
    /// <summary>通用观察者门面，通过显式环境和朝向运行独立视觉、听觉和记忆。</summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PerceptionObserver2D : MonoBehaviour
    {
        [SerializeField] private PerceptionWorld2D world;
        [SerializeField] private Transform viewOrigin;
        [SerializeField] private Transform hearingOrigin;
        [SerializeField] private PerceptionSettings2D settings = new();
        [SerializeField] private Vector2 initialFacing = Vector2.right;
        [SerializeField] private bool showDebugGizmos;
        private PerceptionSession2D session;
        private Vector2 facing = Vector2.right;
        private bool publishing;
        private bool hearingGate;
        public ulong ObservationVersion { get; private set; }
        public IReadOnlyList<TargetPerceptionInfo> Observations => Session.Observations;
        public IReadOnlyList<HeardEvent> HeardEvents => Session.HeardEvents;
        public PerceptionWorld2D World => world;
        public event Action<PerceptionChange> SenseUpdated;
        public event Action<TargetPerceptionInfo> TargetForgotten;
        public event Action ObservationsUpdated;
        private PerceptionSession2D Session => session ??= CreateSession();
        /// <summary>由场景装配或生成器绑定环境；换环境清理旧记忆，避免跨环境泄漏。</summary>
        public void Bind(PerceptionWorld2D environment)
        {
            if (publishing) throw new InvalidOperationException("通知回调中不能更换环境。");
            if (ReferenceEquals(world, environment)) return;
            if (world != null) world.UnregisterObserver(this);
            world = environment; Session.Rebind(); hearingGate = Session.Settings.HearingEnabled;
            if (world != null) world.RegisterObserver(this);
        }
        /// <summary>以验证后的配置创建唯一会话。</summary>
        private PerceptionSession2D CreateSession()
        { var result = new PerceptionSession2D(settings); hearingGate = result.HearingEnabled; return result; }
        /// <summary>运行前显式配置；活动观察者不可重配以避免悄悄丢失记忆。</summary>
        public void Configure(PerceptionWorld2D environment, PerceptionSettings2D configuration, Transform visualOrigin = null, Transform audioOrigin = null)
        {
            if (isActiveAndEnabled || publishing) throw new InvalidOperationException("请在观察者停用时配置。");
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            var validated = configuration.CopyValidated();
            if (world != null) world.UnregisterObserver(this);
            world = environment; settings = validated; viewOrigin = visualOrigin; hearingOrigin = audioOrigin; session = CreateSession();
            if (world != null) world.RegisterObserver(this);
            unchecked { ObservationVersion++; }
        }
        /// <summary>初始化有限且非零的朝向。</summary>
        private void Awake() { SetFacingDirection(initialFacing); _ = Session; }
        /// <summary>恢复时安排扫描，注册引用不依赖场景单例。</summary>
        private void OnEnable() { if (world != null) world.RegisterObserver(this); Session.RequestScan(); }
        /// <summary>禁用保留记忆，恢复批次先结束旧视觉再老化，避免沿用停用前的追踪资格。</summary>
        private void OnDisable() { if (session != null) session.Enqueue(session.SuspendSight); }
        /// <summary>销毁时结束环境注册。</summary>
        private void OnDestroy() { if (world != null) world.UnregisterObserver(this); }
        /// <summary>按缩放时间推进，暂停时不扫描也不消耗事件。</summary>
        private void Update() { if (Time.timeScale > 0) Advance(Time.deltaTime, Time.timeAsDouble); }
        /// <summary>驱动一次内部批次并在状态提交后通知所有订阅者。</summary>
        internal void Advance(float delta, double now)
        {
            if (publishing) throw new InvalidOperationException("禁止重入更新。");
            Session.Advance(delta, now, viewOrigin != null ? (Vector2)viewOrigin.position : (Vector2)transform.position, facing, transform, world);
            hearingGate = Session.HearingEnabled;
            if (!Session.Changed) return;
            unchecked { ObservationVersion++; }
            publishing = true;
            try
            {
                foreach (var change in Session.Changes) Notify(SenseUpdated, change);
                foreach (var forgotten in Session.Forgotten) Notify(TargetForgotten, forgotten);
                if (ObservationsUpdated != null) foreach (Action callback in ObservationsUpdated.GetInvocationList())
                    try { callback(); } catch (Exception error) { Debug.LogException(error, this); }
            }
            finally { publishing = false; }
        }
        /// <summary>异常隔离保证其他订阅者仍可观察已提交批次。</summary>
        private void Notify<T>(Action<T> callbacks, T value)
        {
            if (callbacks == null) return;
            foreach (Action<T> callback in callbacks.GetInvocationList())
                try { callback(value); } catch (Exception error) { Debug.LogException(error, this); }
        }
        /// <summary>提交世界空间方向；零向量保留最后有效朝向。</summary>
        public void SetFacingDirection(Vector2 direction)
        { Guard.Vector(direction); if (direction.sqrMagnitude > 0.000001f) facing = direction.normalized; }
        /// <summary>切换感官，回调内修改延后至下一批。</summary>
        public void SetSenseEnabled(PerceptionSense sense, bool enabled)
        {
            Guard.Sense(sense);
            if (!publishing && sense == PerceptionSense.Hearing) hearingGate = enabled;
            Session.Enqueue(() => Session.SetEnabled(sense, enabled));
        }
        /// <summary>查询最近已提交的感官开关。</summary>
        public bool IsSenseEnabled(PerceptionSense sense)
        { Guard.Sense(sense); return sense == PerceptionSense.Sight ? Session.SightEnabled : Session.HearingEnabled; }
        /// <summary>查询一个目标的不可变观察值。</summary>
        public bool TryGetObservation(PerceptionTarget2D target, out TargetPerceptionInfo result)
        { foreach (var item in Observations) if (item.Target == target) { result = item; return true; } result = default; return false; }
        /// <summary>将指定感官的有效目标写入调用方缓冲；当前过滤仅适用于持续视觉。</summary>
        public void GetObservations(PerceptionSense sense, List<TargetPerceptionInfo> results, bool currentOnly = false)
        {
            Guard.Sense(sense); if (results == null) throw new ArgumentNullException(nameof(results)); results.Clear();
            foreach (var item in Observations)
            { var value = sense == PerceptionSense.Sight ? item.Sight : item.Hearing; if (value.HasValue && (!currentOnly || value.Value.IsCurrent)) results.Add(item); }
        }
        /// <summary>查询可解释的综合已知位置，不读取不可见目标的真实位置。</summary>
        public bool TryGetKnownPosition(PerceptionTarget2D target, out PerceptionStimulus result)
        { if (TryGetObservation(target, out var item)) return item.TryGetKnownPosition(Session.Settings.DominantSense, out result); result = default; return false; }
        /// <summary>显式清理目标的一种感官记忆。</summary>
        public void ForgetSense(PerceptionTarget2D target, PerceptionSense sense)
        { Guard.Sense(sense); Session.Enqueue(() => Session.ForgetSense(target, sense, PerceptionChangeReason.Cleared)); }
        /// <summary>显式忘记目标的全部感官记录。</summary>
        public void ForgetTarget(PerceptionTarget2D target) => Session.Enqueue(() => Session.ForgetTarget(target, null, PerceptionChangeReason.Cleared));
        /// <summary>清理全部记忆而不改变感官配置。</summary>
        public void ClearMemory() => Session.Enqueue(Session.Clear);
        /// <summary>重置对象池生命周期，取消尚未处理的旧事件。</summary>
        public void ResetForReuse() => Session.Enqueue(Session.Reset);
        /// <summary>目标注销命令保留原代次，避免伤及重新注册的目标。</summary>
        internal void Invalidate(PerceptionTarget2D target, ulong generation) => Session.Enqueue(() => Session.ForgetTarget(target, generation, PerceptionChangeReason.TargetInvalidated));
        /// <summary>按照声音上报时的位置和开关判断接收资格。</summary>
        internal bool AcceptNoise(NoiseEvent2D noise, ulong id, double now)
        {
            _ = Session;
            if (!isActiveAndEnabled || !hearingGate) return false;
            Vector2 receiver = hearingOrigin != null ? (Vector2)hearingOrigin.position : (Vector2)transform.position;
            double range = (double)Session.Settings.HearingRange * noise.Loudness;
            if (noise.MaxRange.HasValue) range = Math.Min(range, noise.MaxRange.Value);
            double dx = (double)noise.Position.x - receiver.x, dy = (double)noise.Position.y - receiver.y;
            if (dx * dx + dy * dy > range * range) return false;
            Session.QueueNoise(noise, id, now, receiver); return true;
        }
        /// <summary>编辑模式实时读取初始世界朝向，运行时读取最后一次提交的有效朝向。</summary>
        internal Vector2 DebugFacingDirection => Application.isPlaying
            ? facing
            : initialFacing.sqrMagnitude > 0.000001f ? initialFacing.normalized : Vector2.right;
        /// <summary>显示配置范围、视觉方向及不同感官的记忆位置。</summary>
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;
            var config = session != null ? session.Settings : settings;
            Vector3 origin = viewOrigin != null ? viewOrigin.position : transform.position;
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(origin, config.SightDistance);
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(origin, config.LoseSightDistance);
            Vector2 debugFacing = DebugFacingDirection;
            float center = Mathf.Atan2(debugFacing.y, debugFacing.x) * Mathf.Rad2Deg;
            for (int sign = -1; sign <= 1; sign += 2)
            { float angle = (center + sign * config.ViewAngle * 0.5f) * Mathf.Deg2Rad; Gizmos.DrawLine(origin, origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * config.SightDistance); }
            Gizmos.color = Color.green; Gizmos.DrawWireSphere(hearingOrigin != null ? hearingOrigin.position : transform.position, config.HearingRange);
            if (session == null) return;
            foreach (var item in Observations)
            {
                if (item.Sight.HasValue) { Gizmos.color = item.IsVisible ? Color.cyan : Color.magenta; Gizmos.DrawSphere(item.Sight.Value.Position, 0.1f); }
                if (item.Hearing.HasValue) { Gizmos.color = Color.green; Gizmos.DrawSphere(item.Hearing.Value.Position, 0.12f); }
            }
            foreach (var item in HeardEvents) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(item.Stimulus.Position, 0.15f); }
        }
    }
}
