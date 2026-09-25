using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Computerzhuxi.Perception2D
{
    /// <summary>默认 Unity 门面：装配核心、物理扫描和世界声音路由。</summary>
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
        [SerializeField] private bool automaticSight = true;

        private readonly PerceptionTargetRegistry unboundRegistry = new();
        private readonly List<SightObservation2D> sightFrame = new();
        private PerceptionCore2D core;
        private PerceptionCore2D attachedCore;
        private PerceptionSettings2D pendingSettings;
        private PhysicsSightScanner2D scanner;
        private PhysicsScene2D? physicsSceneOverride;
        private PhysicsScene2D? activePhysicsScene;
        private Vector2 facing = Vector2.right;
        private float scanTimer;
        private bool freshSightSource = true;
        private bool publishing;
        private bool sightDisableRequested;
        private bool sightEnableRequested;
        private bool resetCommitPending;

        public PerceptionCore2D Core => core ??= CreateCore();
        public ulong ObservationVersion => Core.ObservationVersion;
        public IReadOnlyList<TargetPerceptionInfo> Observations => Core.Observations;
        public IReadOnlyList<HeardEvent> HeardEvents => Core.HeardEvents;
        public PerceptionWorld2D World => world;
        public bool AutomaticSight => automaticSight;
        public event Action<PerceptionChange> SenseUpdated;
        public event Action<TargetPerceptionInfo> TargetForgotten;
        public event Action ObservationsUpdated;

        /// <summary>建立独立核心；事件转发在观察者启用或推进前挂接。</summary>
        private PerceptionCore2D CreateCore()
        {
            var result = new PerceptionCore2D(world != null ? world.Registry : unboundRegistry, settings);
            return result;
        }

        /// <summary>在观察者启用期间建立核心事件转发，重复调用前先解除旧订阅。</summary>
        private void AttachCore(PerceptionCore2D current)
        {
            if (current == null) return;
            if (ReferenceEquals(attachedCore, current)) return;
            DetachCore(attachedCore);
            current.SenseUpdated += ForwardSense;
            current.TargetForgotten += ForwardForgotten;
            current.ObservationsUpdated += ForwardUpdated;
            attachedCore = current;
        }

        /// <summary>解除旧核心到组件事件的转发，避免外部保留旧引用后产生伪通知。</summary>
        private void DetachCore(PerceptionCore2D previous)
        {
            if (previous == null) return;
            previous.SenseUpdated -= ForwardSense;
            previous.TargetForgotten -= ForwardForgotten;
            previous.ObservationsUpdated -= ForwardUpdated;
            if (ReferenceEquals(attachedCore, previous)) attachedCore = null;
        }

        /// <summary>绑定默认世界；跨注册表绑定开启新环境生命周期。</summary>
        public void Bind(PerceptionWorld2D environment)
        {
            if (publishing) throw new InvalidOperationException("通知回调中不能更换环境。");
            if (ReferenceEquals(world, environment)) return;
            if (world != null) world.UnregisterObserver(this);
            PerceptionCore2D previous = core;
            world = environment;
            // 不同世界拥有不同目标注册表，旧世界身份不能带入新世界。
            if (previous != null && !ReferenceEquals(previous.Registry, world != null ? world.Registry : unboundRegistry))
            {
                DetachCore(previous);
                core = CreateCore();
                if (isActiveAndEnabled) AttachCore(core);
                sightDisableRequested = false;
                sightEnableRequested = false;
                resetCommitPending = false;
                pendingSettings = null;
            }
            else if (previous != null) previous.EndSightSource();
            if (world != null) world.RegisterObserver(this);
            scanTimer = 0;
            freshSightSource = true;
        }

        /// <summary>运行前设置默认世界、配置和检测位置。</summary>
        public void Configure(PerceptionWorld2D environment, PerceptionSettings2D configuration,
            Transform visualOrigin = null, Transform audioOrigin = null)
        {
            if (isActiveAndEnabled || publishing) throw new InvalidOperationException("请在观察者停用时配置。");
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            PerceptionSettings2D validated = configuration.CopyValidated();
            if (world != null) world.UnregisterObserver(this);
            world = environment;
            settings = validated;
            viewOrigin = visualOrigin;
            hearingOrigin = audioOrigin;
            DetachCore(core);
            core = CreateCore();
            sightDisableRequested = false;
            sightEnableRequested = false;
            resetCommitPending = false;
            pendingSettings = null;
            if (world != null) world.RegisterObserver(this);
            scanTimer = 0;
            freshSightSource = true;
        }

        /// <summary>运行时更新合法参数并安排下一次立即扫描，不清除核心记忆。</summary>
        public void UpdateSettings(PerceptionSettings2D configuration)
        {
            PerceptionSettings2D validated = (configuration ?? throw new ArgumentNullException(nameof(configuration))).CopyValidated();
            Core.UpdateSettings(validated);
            pendingSettings = validated;
            scanTimer = 0;
        }

        /// <summary>替换负责单次扫描的实现；空值恢复默认扫描器。</summary>
        public void SetSightScanner(PhysicsSightScanner2D replacement)
        {
            scanner = replacement ?? new PhysicsSightScanner2D();
            Core.EndSightSource();
            scanTimer = 0;
            freshSightSource = true;
        }

        /// <summary>切换自动扫描或手动完整视觉帧模式。</summary>
        public void SetAutomaticSight(bool enabled)
        {
            if (automaticSight == enabled) return;
            automaticSight = enabled;
            Core.EndSightSource();
            scanTimer = 0;
            freshSightSource = true;
        }

        /// <summary>指定候选和遮挡共同使用的物理场景；空值恢复组件所属场景。</summary>
        public void SetPhysicsSceneOverride(PhysicsScene2D? scene)
        {
            if (scene.HasValue && !scene.Value.IsValid()) throw new ArgumentException("二维物理场景无效。", nameof(scene));
            physicsSceneOverride = scene;
            Core.EndSightSource();
            activePhysicsScene = scene ?? gameObject.scene.GetPhysicsScene2D();
            scanTimer = 0;
            freshSightSource = true;
        }

        /// <summary>手动提交完整视觉帧，适用于自定义输入模式。</summary>
        public void SubmitSightFrame(IReadOnlyList<SightObservation2D> frame, double atTime)
        {
            if (automaticSight) throw new InvalidOperationException("请先切换到手动视觉模式。");
            Core.SubmitSightFrame(frame, atTime);
        }

        /// <summary>初始化有限且非零的观察朝向。</summary>
        private void Awake()
        {
            SetFacingDirection(initialFacing);
            _ = Core;
        }

        /// <summary>启用时安排扫描并注册世界声音接收者。</summary>
        private void OnEnable()
        {
            AttachCore(Core);
            if (world != null) world.RegisterObserver(this);
            scanTimer = 0;
            freshSightSource = true;
            activePhysicsScene = null;
        }

        /// <summary>禁用时结束旧持续视觉，保留分感官记忆。</summary>
        private void OnDisable()
        {
            if (core != null) core.EndSightSource();
            DetachCore(core);
        }

        /// <summary>销毁时从世界声音路由移除观察者。</summary>
        private void OnDestroy()
        {
            if (world != null) world.UnregisterObserver(this);
            DetachCore(core);
        }

        /// <summary>按缩放时间驱动；暂停时不扫描、不老化、不发布事件。</summary>
        private void Update()
        {
            if (Time.timeScale > 0) Advance(Time.deltaTime, Time.timeAsDouble);
        }

        /// <summary>执行至多一次物理扫描并提交核心批次。</summary>
        internal void Advance(float delta, double now)
        {
            Guard.NonNegative(delta, nameof(delta));
            if (publishing) throw new InvalidOperationException("禁止在回调中重入更新。");
            PerceptionCore2D state = Core;
            if (isActiveAndEnabled) AttachCore(state);
            if (resetCommitPending)
            {
                // 重置先独立提交，下一次推进才用发现距离采样，避免丢弃帧后等待旧扫描间隔。
                state.Advance(now);
                settings = state.Settings;
                resetCommitPending = false;
                scanTimer = 0;
                freshSightSource = true;
                return;
            }
            PhysicsScene2D scene = physicsSceneOverride ?? gameObject.scene.GetPhysicsScene2D();
            if (automaticSight && activePhysicsScene.HasValue && !activePhysicsScene.Value.Equals(scene))
            {
                // 物理场景变化是新的视觉来源，旧场景的追踪距离不得用于新场景发现。
                state.EndSightSource();
                freshSightSource = true;
                scanTimer = 0;
            }
            activePhysicsScene = automaticSight ? scene : null;
            scanTimer -= delta;
            bool canScan = sightEnableRequested || state.SightEnabled && !sightDisableRequested;
            PerceptionSettings2D scanSettings = pendingSettings ?? settings;
            if (automaticSight && canScan && world != null && world.isActiveAndEnabled && scanTimer <= 0)
            {
                scanTimer = scanSettings.ScanInterval;
                Vector2 origin = viewOrigin != null ? (Vector2)viewOrigin.position : (Vector2)transform.position;
                (scanner ??= new PhysicsSightScanner2D()).Scan(scene, world.Registry, scanSettings, origin, facing,
                    transform, freshSightSource ? Array.Empty<TargetPerceptionInfo>() : state.Observations, sightFrame);
                state.SubmitSightFrame(sightFrame, now);
                freshSightSource = false;
                sightEnableRequested = false;
            }
            state.Advance(now);
            settings = state.Settings;
            if (ReferenceEquals(pendingSettings, scanSettings)) pendingSettings = null;
        }

        /// <summary>提交世界方向；零向量保留最后有效朝向。</summary>
        public void SetFacingDirection(Vector2 direction)
        {
            Guard.Vector(direction);
            if (direction.sqrMagnitude > 0.000001f) facing = direction.normalized;
        }

        /// <summary>切换指定感官，回调期间的修改延后至下一批。</summary>
        public void SetSenseEnabled(PerceptionSense sense, bool enabled)
        {
            PerceptionCore2D state = Core;
            state.SetSenseEnabled(sense, enabled);
            if (sense != PerceptionSense.Sight) return;
            if (!enabled)
            {
                sightDisableRequested = true;
                sightEnableRequested = false;
                return;
            }
            if (sightDisableRequested || !state.SightEnabled)
            {
                // 恢复视觉必须立即用发现距离采样，不能沿用关闭前的扫描倒计时。
                scanTimer = 0;
                freshSightSource = true;
                sightEnableRequested = true;
            }
            sightDisableRequested = false;
        }

        /// <summary>查询最近提交的感官开关。</summary>
        public bool IsSenseEnabled(PerceptionSense sense) => Core.IsSenseEnabled(sense);

        /// <summary>查询一个 Unity 目标的已提交记录。</summary>
        public bool TryGetObservation(PerceptionTarget2D target, out TargetPerceptionInfo result)
        {
            foreach (TargetPerceptionInfo item in Observations)
                if (item.Target == target) { result = item; return true; }
            result = default;
            return false;
        }

        /// <summary>将指定感官的有效目标写入调用方缓冲。</summary>
        public void GetObservations(PerceptionSense sense, List<TargetPerceptionInfo> results, bool currentOnly = false)
            => Core.GetObservations(sense, results, currentOnly);

        /// <summary>查询目标已知位置，不读取不可见目标的实时位置。</summary>
        public bool TryGetKnownPosition(PerceptionTarget2D target, out PerceptionStimulus result)
        {
            if (TryGetObservation(target, out TargetPerceptionInfo item))
                return item.TryGetKnownPosition(Core.Settings.DominantSense, out result);
            result = default;
            return false;
        }

        /// <summary>显式清除目标一种感官的记忆。</summary>
        public void ForgetSense(PerceptionTarget2D target, PerceptionSense sense)
        {
            if (target != null && target.Handle.Id != 0) Core.ForgetSense(target.Handle, sense);
        }

        /// <summary>显式忘记目标全部感官记录。</summary>
        public void ForgetTarget(PerceptionTarget2D target)
        {
            if (target != null && target.Handle.Id != 0) Core.ForgetTarget(target.Handle);
        }

        /// <summary>清除全部记忆而不修改感官开关。</summary>
        public void ClearMemory() => Core.ClearMemory();

        /// <summary>完整重置观察者的对象池生命周期。</summary>
        public void ResetForReuse()
        {
            Core.ResetForReuse();
            pendingSettings = null;
            resetCommitPending = true;
            sightDisableRequested = false;
            sightEnableRequested = false;
            scanTimer = 0;
            freshSightSource = true;
        }

        /// <summary>默认声音入口在世界距离筛选后提交已经确认的听觉输入。</summary>
        internal bool AcceptNoise(NoiseEvent2D noise, double now)
        {
            if (!isActiveAndEnabled || !Core.CanReceiveHearing) return false;
            Vector2 receiver = hearingOrigin != null ? (Vector2)hearingOrigin.position : (Vector2)transform.position;
            double range = (double)settings.HearingRange * noise.Loudness;
            if (noise.MaxRange.HasValue) range = Math.Min(range, noise.MaxRange.Value);
            double dx = (double)noise.Position.x - receiver.x;
            double dy = (double)noise.Position.y - receiver.y;
            if (dx * dx + dy * dy > range * range) return false;
            PerceptionTargetHandle source = noise.Source != null ? noise.Source.Handle : default;
            Core.ReportHearing(noise.Position, receiver, now, noise.Loudness, source, noise.Tag);
            return true;
        }

        /// <summary>编辑模式读取序列化方向，运行时读取最后有效方向。</summary>
        internal Vector2 DebugFacingDirection => Application.isPlaying
            ? facing
            : initialFacing.sqrMagnitude > 0.000001f ? initialFacing.normalized : Vector2.right;

        /// <summary>转发核心感官事件，并隔离组件订阅者异常。</summary>
        private void ForwardSense(PerceptionChange change) => Notify(SenseUpdated, change);

        /// <summary>转发核心目标遗忘事件。</summary>
        private void ForwardForgotten(TargetPerceptionInfo item) => Notify(TargetForgotten, item);

        /// <summary>在核心最终提交后转发批次更新事件。</summary>
        private void ForwardUpdated()
        {
            if (ObservationsUpdated == null) return;
            publishing = true;
            try
            {
                foreach (Action callback in ObservationsUpdated.GetInvocationList())
                    try { callback(); } catch (Exception error) { Debug.LogException(error, this); }
            }
            finally { publishing = false; }
        }

        /// <summary>逐个转发组件事件，避免订阅异常中断同批通知。</summary>
        private void Notify<T>(Action<T> callbacks, T value)
        {
            if (callbacks == null) return;
            publishing = true;
            try
            {
                foreach (Action<T> callback in callbacks.GetInvocationList())
                    try { callback(value); } catch (Exception error) { Debug.LogException(error, this); }
            }
            finally { publishing = false; }
        }

        /// <summary>绘制视觉、听觉范围及已提交感官记忆。</summary>
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;
            PerceptionSettings2D config = core != null ? core.Settings : settings;
            Vector3 origin = viewOrigin != null ? viewOrigin.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, config.SightDistance);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, config.LoseSightDistance);
            Vector2 direction = DebugFacingDirection;
            float center = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                float angle = (center + sign * config.ViewAngle * 0.5f) * Mathf.Deg2Rad;
                Gizmos.DrawLine(origin, origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * config.SightDistance);
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(hearingOrigin != null ? hearingOrigin.position : transform.position, config.HearingRange);
            if (core == null) return;
            foreach (TargetPerceptionInfo item in Observations)
            {
                if (item.Sight.HasValue)
                {
                    Gizmos.color = item.IsVisible ? Color.cyan : Color.magenta;
                    Gizmos.DrawSphere(item.Sight.Value.Position, 0.1f);
                }
                if (item.Hearing.HasValue)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(item.Hearing.Value.Position, 0.12f);
                }
            }
            foreach (HeardEvent item in HeardEvents)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(item.Stimulus.Position, 0.15f);
            }
        }
    }
}
