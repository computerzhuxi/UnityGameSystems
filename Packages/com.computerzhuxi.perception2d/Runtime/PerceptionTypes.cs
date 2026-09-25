using System;
using UnityEngine;

namespace Computerzhuxi.Perception2D
{
    public enum PerceptionSense { Sight, Hearing }
    public enum PerceptionChangeReason { Acquired, Lost, Heard, SenseDisabled, Expired, Cleared, TargetInvalidated, SourceChanged }

    /// <summary>一次完整视觉帧中的目标事实，由调用方提供检测点和观察位置。</summary>
    public readonly struct SightObservation2D
    {
        public PerceptionTargetHandle Handle { get; }
        public Vector2 Position { get; }
        public Vector2 ReceiverPosition { get; }

        /// <summary>建立已经确认可见的目标输入。</summary>
        public SightObservation2D(PerceptionTargetHandle handle, Vector2 position, Vector2 receiverPosition)
        {
            Guard.Vector(position);
            Guard.Vector(receiverPosition);
            Handle = handle;
            Position = position;
            ReceiverPosition = receiverPosition;
        }
    }

    /// <summary>保存一次成功感知的不可变事实；位置不会跟随目标实时变化。</summary>
    public readonly struct PerceptionStimulus
    {
        public PerceptionSense Sense { get; }
        public Vector2 Position { get; }
        public Vector2 ReceiverPosition { get; }
        public double Time { get; }
        public float Strength { get; }
        public string Tag { get; }
        public bool IsCurrent { get; }
        /// <summary>建立由感知系统发布的刺激快照。</summary>
        internal PerceptionStimulus(PerceptionSense sense, Vector2 position, Vector2 receiver, double time, float strength, string tag, bool current)
        { Sense = sense; Position = position; ReceiverPosition = receiver; Time = time; Strength = strength; Tag = tag; IsCurrent = current; }
        /// <summary>结束持续感知状态但保留最后成功位置和时间。</summary>
        internal PerceptionStimulus EndCurrent() => new(Sense, Position, ReceiverPosition, Time, Strength, Tag, false);
    }

    /// <summary>一个目标在一个注册生命周期中的各感官只读记录。</summary>
    public readonly struct TargetPerceptionInfo
    {
        public PerceptionTargetHandle Handle { get; }
        public object AssociatedObject { get; }
        public PerceptionTarget2D Target => AssociatedObject as PerceptionTarget2D;
        public ulong Generation => Handle.Generation;
        public PerceptionStimulus? Sight { get; }
        public PerceptionStimulus? Hearing { get; }
        public bool IsVisible => Sight.HasValue && Sight.Value.IsCurrent;
        public Transform TargetRoot => Target != null ? Target.TargetRoot : null;
        /// <summary>生成目标当前状态快照，不向调用方暴露可变轨迹。</summary>
        internal TargetPerceptionInfo(PerceptionTargetHandle handle, object associatedObject, PerceptionStimulus? sight, PerceptionStimulus? hearing)
        { Handle = handle; AssociatedObject = associatedObject; Sight = sight; Hearing = hearing; }
        /// <summary>兼容已有包测试对不可变位置策略的直接构造。</summary>
        internal TargetPerceptionInfo(PerceptionTarget2D target, ulong generation, PerceptionStimulus? sight, PerceptionStimulus? hearing)
        { Handle = default; AssociatedObject = target; Sight = sight; Hearing = hearing; }
        /// <summary>优先当前视觉，否则采用最新记录，同时间采用主导感官。</summary>
        public bool TryGetKnownPosition(PerceptionSense dominant, out PerceptionStimulus result)
        {
            Guard.Sense(dominant);
            if (IsVisible) { result = Sight.Value; return true; }
            if (!Sight.HasValue && !Hearing.HasValue) { result = default; return false; }
            if (!Sight.HasValue) result = Hearing.Value;
            else if (!Hearing.HasValue) result = Sight.Value;
            else result = Sight.Value.Time > Hearing.Value.Time || (Sight.Value.Time == Hearing.Value.Time && dominant == PerceptionSense.Sight) ? Sight.Value : Hearing.Value;
            return true;
        }
    }

    /// <summary>无来源声音使用独立事件身份及有限记忆，不伪造目标。</summary>
    public readonly struct HeardEvent
    {
        public ulong Id { get; }
        public PerceptionStimulus Stimulus { get; }
        /// <summary>保存一次无来源声音事件。</summary>
        internal HeardEvent(ulong id, PerceptionStimulus stimulus) { Id = id; Stimulus = stimulus; }
    }

    /// <summary>感官变化通知携带原因、旧生命周期及最后记录。</summary>
    public readonly struct PerceptionChange
    {
        public PerceptionTargetHandle Handle { get; }
        public object AssociatedObject { get; }
        public PerceptionTarget2D Target => AssociatedObject as PerceptionTarget2D;
        public ulong Generation => Handle.Generation;
        public ulong EventId { get; }
        public PerceptionSense Sense { get; }
        public PerceptionChangeReason Reason { get; }
        public PerceptionStimulus Stimulus { get; }
        /// <summary>创建已提交状态对应的通知。</summary>
        internal PerceptionChange(PerceptionTargetHandle handle, object associatedObject, PerceptionSense sense, PerceptionChangeReason reason, PerceptionStimulus stimulus, ulong eventId = 0)
        { Handle = handle; AssociatedObject = associatedObject; Sense = sense; Reason = reason; Stimulus = stimulus; EventId = eventId; }
    }

    /// <summary>游戏提交的声音描述，与音频播放系统无依赖。</summary>
    public readonly struct NoiseEvent2D
    {
        public Vector2 Position { get; }
        public PerceptionTarget2D Source { get; }
        public float Loudness { get; }
        public float? MaxRange { get; }
        public string Tag { get; }
        /// <summary>验证声音输入；最大范围为空表示仅受监听范围和响度限制。</summary>
        public NoiseEvent2D(Vector2 position, float loudness = 1, PerceptionTarget2D source = null, float? maxRange = null, string tag = "")
        {
            Guard.Vector(position); Guard.NonNegative(loudness, nameof(loudness));
            if (maxRange.HasValue) Guard.NonNegative(maxRange.Value, nameof(maxRange));
            Position = position; Loudness = loudness; Source = source; MaxRange = maxRange; Tag = tag ?? "";
        }
    }

    /// <summary>初始化配置在会话创建时复制，运行时变更需显式重配。</summary>
    [Serializable]
    public sealed class PerceptionSettings2D
    {
        public bool SightEnabled = true;
        public bool HearingEnabled = true;
        public LayerMask TargetLayers = ~0;
        public LayerMask ObstacleLayers;
        [Min(0)] public float SightDistance = 6;
        [Min(0)] public float LoseSightDistance = 8;
        [Range(0, 360)] public float ViewAngle = 100;
        [Min(0.02f)] public float ScanInterval = 0.1f;
        [Min(0)] public float SightMemory = 10;
        [Min(0)] public float HearingRange = 10;
        [Min(0)] public float HearingMemory = 5;
        [Min(0.01f)] public float AnonymousMemory = 5;
        public PerceptionSense DominantSense = PerceptionSense.Sight;
        /// <summary>拒绝非法配置而非静默修正，保持编辑器与运行时契约一致。</summary>
        public PerceptionSettings2D CopyValidated()
        {
            Guard.NonNegative(SightDistance, nameof(SightDistance)); Guard.NonNegative(LoseSightDistance, nameof(LoseSightDistance));
            Guard.NonNegative(ViewAngle, nameof(ViewAngle)); Guard.NonNegative(ScanInterval, nameof(ScanInterval));
            Guard.NonNegative(SightMemory, nameof(SightMemory)); Guard.NonNegative(HearingRange, nameof(HearingRange));
            Guard.NonNegative(HearingMemory, nameof(HearingMemory)); Guard.NonNegative(AnonymousMemory, nameof(AnonymousMemory)); Guard.Sense(DominantSense);
            if (LoseSightDistance < SightDistance || ViewAngle > 360 || ScanInterval < 0.02f || AnonymousMemory <= 0) throw new ArgumentException("感知配置范围无效。");
            return (PerceptionSettings2D)MemberwiseClone();
        }
    }

    internal static class Guard
    {
        /// <summary>验证有限非负数。</summary>
        internal static void NonNegative(float value, string name) { if (float.IsNaN(value) || float.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(name); }
        /// <summary>拒绝不存在的感官标识。</summary>
        internal static void Sense(PerceptionSense sense) { if (sense != PerceptionSense.Sight && sense != PerceptionSense.Hearing) throw new ArgumentOutOfRangeException(nameof(sense)); }
        /// <summary>验证世界空间向量所有分量有限。</summary>
        internal static void Vector(Vector2 value) { if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.x) || float.IsInfinity(value.y)) throw new ArgumentException("向量必须有限。"); }
        /// <summary>验证时间轴上的有限时刻。</summary>
        internal static void Time(double value, string name) { if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name); }
    }
}
