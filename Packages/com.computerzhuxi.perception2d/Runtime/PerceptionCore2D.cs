using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computerzhuxi.Perception2D
{
    /// <summary>普通 C# 感知状态机；输入已确认的视觉帧和声音，不查询 Unity 场景。</summary>
    public sealed class PerceptionCore2D
    {
        private sealed class Track
        {
            internal PerceptionTargetHandle Handle;
            internal object AssociatedObject;
            internal PerceptionStimulus? Sight;
            internal PerceptionStimulus? Hearing;

            /// <summary>生成当前批次的不可变查询值。</summary>
            internal TargetPerceptionInfo Snapshot() => new(Handle, AssociatedObject, Sight, Hearing);
        }

        private readonly PerceptionTargetRegistry registry;
        private readonly Dictionary<PerceptionTargetHandle, Track> tracks = new();
        private readonly List<TargetPerceptionInfo> observations = new();
        private readonly List<HeardEvent> heardEvents = new();
        private readonly Queue<Action> commands = new();
        private readonly List<PerceptionChange> changes = new();
        private readonly List<TargetPerceptionInfo> forgotten = new();
        private readonly List<PerceptionTargetHandle> remove = new();
        private PerceptionSettings2D configuration;
        private bool changed;
        private bool publishing;
        private bool advancing;
        private bool hearingGate;
        private bool resetPending;
        private bool hasTime;
        private double time;
        private double lastInputTime;
        private ulong nextEventId;
        private ulong sightSourceGeneration = 1;

        public PerceptionSettings2D Settings => configuration.CopyValidated();
        public PerceptionTargetRegistry Registry => registry;
        public IReadOnlyList<TargetPerceptionInfo> Observations { get; }
        public IReadOnlyList<HeardEvent> HeardEvents { get; }
        public bool SightEnabled { get; private set; }
        public bool HearingEnabled { get; private set; }
        public bool CanReceiveHearing => hearingGate;
        public ulong ObservationVersion { get; private set; }
        public ulong SightSourceGeneration => sightSourceGeneration;
        public event Action<PerceptionChange> SenseUpdated;
        public event Action<TargetPerceptionInfo> TargetForgotten;
        public event Action ObservationsUpdated;

        /// <summary>创建持有独立记忆的核心；多个核心可共享同一个注册表。</summary>
        public PerceptionCore2D(PerceptionTargetRegistry registry, PerceptionSettings2D settings = null)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            configuration = (settings ?? new PerceptionSettings2D()).CopyValidated();
            SightEnabled = configuration.SightEnabled;
            HearingEnabled = configuration.HearingEnabled;
            hearingGate = HearingEnabled;
            Observations = observations.AsReadOnly();
            HeardEvents = heardEvents.AsReadOnly();
        }

        /// <summary>提交一次完整视觉帧；调用后可立即复用输入缓冲。</summary>
        public void SubmitSightFrame(IReadOnlyList<SightObservation2D> frame, double atTime)
            => SubmitSightFrame(frame, atTime, sightSourceGeneration);

        /// <summary>提交带视觉来源代次的完整帧；过期来源的排队帧不会生效。</summary>
        public void SubmitSightFrame(IReadOnlyList<SightObservation2D> frame, double atTime, ulong sourceGeneration)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (resetPending || sourceGeneration != sightSourceGeneration) return;
            var copy = new Dictionary<PerceptionTargetHandle, SightObservation2D>();
            for (int index = 0; index < frame.Count; index++)
            {
                SightObservation2D value = frame[index];
                registry.ValidateOwner(value.Handle);
                Guard.Vector(value.Position);
                Guard.Vector(value.ReceiverPosition);
                // 同一目标在一帧内只保留最后一个检测点，不重复发布获得事件。
                copy[value.Handle] = value;
            }

            // 只有全部目标输入都合法且来源仍有效，才占用这条时间轴的水位。
            ValidateInputTime(atTime);
            commands.Enqueue(() => ApplySightFrame(copy, atTime, sourceGeneration));
        }

        /// <summary>结束当前视觉并切换来源代次，保留已获得的记忆。</summary>
        public ulong EndSightSource()
        {
            sightSourceGeneration = checked(sightSourceGeneration + 1);
            commands.Enqueue(() => EndAllSight(PerceptionChangeReason.SourceChanged));
            return sightSourceGeneration;
        }

        /// <summary>上报已经由调用方确认听到的声音，不重复进行距离判断。</summary>
        public void ReportHearing(Vector2 position, Vector2 receiverPosition, double atTime, float strength = 1,
            PerceptionTargetHandle source = default, string tag = "")
        {
            Guard.Vector(position);
            Guard.Vector(receiverPosition);
            Guard.NonNegative(strength, nameof(strength));
            if (source.Registry != null) registry.ValidateOwner(source);
            if (resetPending || !hearingGate || strength == 0) return;
            if (source.Registry != null && !registry.IsValid(source)) return;
            ValidateInputTime(atTime);
            ulong eventId = checked(++nextEventId);
            string copiedTag = tag ?? "";
            commands.Enqueue(() => ApplyHearing(position, receiverPosition, atTime, strength, source, copiedTag, eventId));
        }

        /// <summary>排队切换感官；听觉接收门在非回调期间立即关闭。</summary>
        public void SetSenseEnabled(PerceptionSense sense, bool enabled)
        {
            Guard.Sense(sense);
            if (sense == PerceptionSense.Hearing && !publishing) hearingGate = enabled;
            commands.Enqueue(() => ApplySenseEnabled(sense, enabled));
        }

        /// <summary>读取最近提交的感官状态。</summary>
        public bool IsSenseEnabled(PerceptionSense sense)
        {
            Guard.Sense(sense);
            return sense == PerceptionSense.Sight ? SightEnabled : HearingEnabled;
        }

        /// <summary>移除指定目标的一种感官记忆。</summary>
        public void ForgetSense(PerceptionTargetHandle handle, PerceptionSense sense)
        {
            registry.ValidateOwner(handle);
            Guard.Sense(sense);
            commands.Enqueue(() => RemoveSense(handle, sense, PerceptionChangeReason.Cleared));
        }

        /// <summary>移除指定生命周期的全部目标记忆。</summary>
        public void ForgetTarget(PerceptionTargetHandle handle)
        {
            registry.ValidateOwner(handle);
            commands.Enqueue(() => RemoveTarget(handle, PerceptionChangeReason.Cleared));
        }

        /// <summary>清除全部记忆，不改变感官开关。</summary>
        public void ClearMemory() => commands.Enqueue(() => Clear(PerceptionChangeReason.Cleared));

        /// <summary>重置对象池复用状态，取消已经排队的旧输入。</summary>
        public void ResetForReuse()
        {
            commands.Clear();
            // 旧命令已取消，其未来时间也不得阻止新生命周期的重置批次。
            lastInputTime = hasTime ? time : 0;
            resetPending = true;
            sightSourceGeneration = checked(sightSourceGeneration + 1);
            commands.Enqueue(() =>
            {
                Clear(PerceptionChangeReason.Cleared);
                SightEnabled = configuration.SightEnabled;
                HearingEnabled = configuration.HearingEnabled;
                hearingGate = HearingEnabled;
                resetPending = false;
                changed = true;
            });
        }

        /// <summary>运行时更新合法配置，同时保留当前记忆和感官开关。</summary>
        public void UpdateSettings(PerceptionSettings2D settings)
        {
            PerceptionSettings2D copy = (settings ?? throw new ArgumentNullException(nameof(settings))).CopyValidated();
            commands.Enqueue(() => { configuration = copy; changed = true; });
        }

        /// <summary>按调用方提供的时间推进一个原子批次，所有通知读取最终查询状态。</summary>
        public void Advance(double now)
        {
            Guard.Time(now, nameof(now));
            if (advancing || publishing) throw new InvalidOperationException("禁止重入感知更新。");
            if (hasTime && now < time || now < lastInputTime) throw new ArgumentOutOfRangeException(nameof(now), "感知时间不能倒退。");
            advancing = true;
            try
            {
                time = now;
                hasTime = true;
                changes.Clear();
                forgotten.Clear();
                changed = false;
                int count = commands.Count;
                for (int index = 0; index < count && commands.Count > 0; index++) commands.Dequeue()();
                InvalidateTargets();
                Expire(now);
                RemoveEmptyTracks();
                forgotten.RemoveAll(item => tracks.ContainsKey(item.Handle));
                if (!changed) return;
                observations.Clear();
                foreach (Track track in tracks.Values) observations.Add(track.Snapshot());
                unchecked { ObservationVersion++; }
                Publish();
            }
            finally { advancing = false; }
        }

        /// <summary>查找指定生命周期的只读记录。</summary>
        public bool TryGetObservation(PerceptionTargetHandle handle, out TargetPerceptionInfo result)
        {
            registry.ValidateOwner(handle);
            foreach (TargetPerceptionInfo item in Observations)
                if (item.Handle.Equals(handle)) { result = item; return true; }
            result = default;
            return false;
        }

        /// <summary>取得当前视觉或最新有效记忆中的已知位置。</summary>
        public bool TryGetKnownPosition(PerceptionTargetHandle handle, out PerceptionStimulus result)
        {
            if (TryGetObservation(handle, out TargetPerceptionInfo info))
                return info.TryGetKnownPosition(configuration.DominantSense, out result);
            result = default;
            return false;
        }

        /// <summary>过滤指定感官的已提交记录到调用方缓冲。</summary>
        public void GetObservations(PerceptionSense sense, List<TargetPerceptionInfo> results, bool currentOnly = false)
        {
            Guard.Sense(sense);
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            foreach (TargetPerceptionInfo item in Observations)
            {
                PerceptionStimulus? value = sense == PerceptionSense.Sight ? item.Sight : item.Hearing;
                if (value.HasValue && (!currentOnly || value.Value.IsCurrent)) results.Add(item);
            }
        }

        /// <summary>验证新输入时间与当前批次属于同一条单调时间轴。</summary>
        private void ValidateInputTime(double atTime)
        {
            Guard.Time(atTime, nameof(atTime));
            if (hasTime && atTime < time || atTime < lastInputTime)
                throw new ArgumentOutOfRangeException(nameof(atTime), "感知输入时间不能倒退。");
            lastInputTime = atTime;
        }

        /// <summary>应用完整帧；缺席目标只有在本次确实提交视觉帧时才丢失。</summary>
        private void ApplySightFrame(Dictionary<PerceptionTargetHandle, SightObservation2D> frame, double atTime, ulong sourceGeneration)
        {
            if (sourceGeneration != sightSourceGeneration || !SightEnabled) return;
            foreach (KeyValuePair<PerceptionTargetHandle, SightObservation2D> pair in frame)
            {
                if (!registry.IsValid(pair.Key)) continue;
                Track track = GetTrack(pair.Key);
                bool wasCurrent = track.Sight.HasValue && track.Sight.Value.IsCurrent;
                SightObservation2D input = pair.Value;
                track.Sight = new PerceptionStimulus(PerceptionSense.Sight, input.Position, input.ReceiverPosition, atTime, 1, "", true);
                changed = true;
                if (!wasCurrent) AddChange(track, PerceptionSense.Sight, PerceptionChangeReason.Acquired, track.Sight.Value);
            }

            foreach (Track track in tracks.Values)
                if (!frame.ContainsKey(track.Handle)) EndSight(track, PerceptionChangeReason.Lost);
        }

        /// <summary>应用一条有效声音；旧生命周期的排队声音被丢弃。</summary>
        private void ApplyHearing(Vector2 position, Vector2 receiver, double atTime, float strength,
            PerceptionTargetHandle source, string tag, ulong eventId)
        {
            if (!HearingEnabled || source.Registry != null && !registry.IsValid(source)) return;
            var stimulus = new PerceptionStimulus(PerceptionSense.Hearing, position, receiver, atTime, strength, tag, false);
            if (source.Registry == null)
            {
                heardEvents.Add(new HeardEvent(eventId, stimulus));
                changes.Add(new PerceptionChange(default, null, PerceptionSense.Hearing, PerceptionChangeReason.Heard, stimulus, eventId));
            }
            else
            {
                Track track = GetTrack(source);
                if (!track.Hearing.HasValue || atTime >= track.Hearing.Value.Time) track.Hearing = stimulus;
                AddChange(track, PerceptionSense.Hearing, PerceptionChangeReason.Heard, stimulus, eventId);
            }
            changed = true;
        }

        /// <summary>获取身份对应的内部记录；注册表只提供关联对象而不读取其状态。</summary>
        private Track GetTrack(PerceptionTargetHandle handle)
        {
            if (!tracks.TryGetValue(handle, out Track track))
            {
                track = new Track { Handle = handle, AssociatedObject = registry.GetAssociatedObject(handle) };
                tracks.Add(handle, track);
            }
            return track;
        }

        /// <summary>切换感官并结束被关闭的持续视觉。</summary>
        private void ApplySenseEnabled(PerceptionSense sense, bool enabled)
        {
            if (sense == PerceptionSense.Hearing)
            {
                if (HearingEnabled != enabled) changed = true;
                HearingEnabled = enabled;
                hearingGate = enabled;
                return;
            }

            if (SightEnabled == enabled) return;
            SightEnabled = enabled;
            changed = true;
            if (!enabled) EndAllSight(PerceptionChangeReason.SenseDisabled);
        }

        /// <summary>结束所有当前视觉而保留最后成功观察事实。</summary>
        private void EndAllSight(PerceptionChangeReason reason)
        {
            foreach (Track track in tracks.Values) EndSight(track, reason);
        }

        /// <summary>结束一个目标的当前视觉，丢失时间不覆盖最后成功观察时间。</summary>
        private void EndSight(Track track, PerceptionChangeReason reason)
        {
            if (!track.Sight.HasValue || !track.Sight.Value.IsCurrent) return;
            track.Sight = track.Sight.Value.EndCurrent();
            AddChange(track, PerceptionSense.Sight, reason, track.Sight.Value);
            changed = true;
        }

        /// <summary>移除指定感官的事实。</summary>
        private void RemoveSense(PerceptionTargetHandle handle, PerceptionSense sense, PerceptionChangeReason reason)
        {
            if (!tracks.TryGetValue(handle, out Track track)) return;
            PerceptionStimulus? value = sense == PerceptionSense.Sight ? track.Sight : track.Hearing;
            if (!value.HasValue) return;
            AddChange(track, sense, reason, value.Value);
            if (sense == PerceptionSense.Sight) track.Sight = null;
            else track.Hearing = null;
            changed = true;
        }

        /// <summary>移除指定目标的所有感官事实并保留整体遗忘通知。</summary>
        private void RemoveTarget(PerceptionTargetHandle handle, PerceptionChangeReason reason)
        {
            if (!tracks.TryGetValue(handle, out Track track)) return;
            if (track.Sight.HasValue) AddChange(track, PerceptionSense.Sight, reason, track.Sight.Value);
            if (track.Hearing.HasValue) AddChange(track, PerceptionSense.Hearing, reason, track.Hearing.Value);
            forgotten.Add(track.Snapshot());
            tracks.Remove(handle);
            changed = true;
        }

        /// <summary>清除目标和匿名声音记忆。</summary>
        private void Clear(PerceptionChangeReason reason)
        {
            remove.Clear();
            foreach (PerceptionTargetHandle handle in tracks.Keys) remove.Add(handle);
            foreach (PerceptionTargetHandle handle in remove) RemoveTarget(handle, reason);
            foreach (HeardEvent item in heardEvents)
                changes.Add(new PerceptionChange(default, null, PerceptionSense.Hearing, reason, item.Stimulus, item.Id));
            changed |= heardEvents.Count > 0;
            heardEvents.Clear();
        }

        /// <summary>在批次内清理注册表中已失效的生命周期。</summary>
        private void InvalidateTargets()
        {
            remove.Clear();
            foreach (PerceptionTargetHandle handle in tracks.Keys)
                if (!registry.IsValid(handle)) remove.Add(handle);
            foreach (PerceptionTargetHandle handle in remove) RemoveTarget(handle, PerceptionChangeReason.TargetInvalidated);
        }

        /// <summary>按感官分别过期；当前视觉始终保留。</summary>
        private void Expire(double now)
        {
            foreach (Track track in tracks.Values)
            {
                if (track.Sight.HasValue && !track.Sight.Value.IsCurrent && configuration.SightMemory > 0 && now - track.Sight.Value.Time >= configuration.SightMemory)
                    RemoveSense(track.Handle, PerceptionSense.Sight, PerceptionChangeReason.Expired);
                if (track.Hearing.HasValue && configuration.HearingMemory > 0 && now - track.Hearing.Value.Time >= configuration.HearingMemory)
                    RemoveSense(track.Handle, PerceptionSense.Hearing, PerceptionChangeReason.Expired);
            }
            for (int index = heardEvents.Count - 1; index >= 0; index--)
            {
                HeardEvent item = heardEvents[index];
                if (now - item.Stimulus.Time < configuration.AnonymousMemory) continue;
                heardEvents.RemoveAt(index);
                changes.Add(new PerceptionChange(default, null, PerceptionSense.Hearing, PerceptionChangeReason.Expired, item.Stimulus, item.Id));
                changed = true;
            }
        }

        /// <summary>将两种感官都不存在的轨迹从最终查询状态移除。</summary>
        private void RemoveEmptyTracks()
        {
            remove.Clear();
            foreach (Track track in tracks.Values)
                if (!track.Sight.HasValue && !track.Hearing.HasValue) remove.Add(track.Handle);
            foreach (PerceptionTargetHandle handle in remove)
            {
                forgotten.Add(tracks[handle].Snapshot());
                tracks.Remove(handle);
                changed = true;
            }
        }

        /// <summary>记录目标变化及其生命周期快照。</summary>
        private void AddChange(Track track, PerceptionSense sense, PerceptionChangeReason reason, PerceptionStimulus stimulus, ulong eventId = 0)
            => changes.Add(new PerceptionChange(track.Handle, track.AssociatedObject, sense, reason, stimulus, eventId));

        /// <summary>在查询视图提交后按固定顺序隔离发布所有订阅者异常。</summary>
        private void Publish()
        {
            publishing = true;
            try
            {
                foreach (PerceptionChange change in changes) Notify(SenseUpdated, change);
                foreach (TargetPerceptionInfo item in forgotten) Notify(TargetForgotten, item);
                if (ObservationsUpdated != null)
                    foreach (Action callback in ObservationsUpdated.GetInvocationList())
                        try { callback(); } catch (Exception error) { Debug.LogException(error); }
            }
            finally { publishing = false; }
        }

        /// <summary>逐一通知订阅者，避免一个回调异常阻止其他回调。</summary>
        private static void Notify<T>(Action<T> callbacks, T value)
        {
            if (callbacks == null) return;
            foreach (Action<T> callback in callbacks.GetInvocationList())
                try { callback(value); } catch (Exception error) { Debug.LogException(error); }
        }
    }
}
