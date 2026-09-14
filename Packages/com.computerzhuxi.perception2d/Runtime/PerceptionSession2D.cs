using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computerzhuxi.Perception2D
{
    /// <summary>唯一拥有观察者记忆、扫描时钟和批次通知；不包含游戏决策。</summary>
    internal sealed class PerceptionSession2D
    {
        private sealed class Track
        {
            internal PerceptionTarget2D Target;
            internal ulong Generation;
            internal PerceptionStimulus? Sight, Hearing;
            /// <summary>生成只读记录供查询和通知使用。</summary>
            internal TargetPerceptionInfo Snapshot() => new(Target, Generation, Sight, Hearing);
        }
        internal readonly PerceptionSettings2D Settings;
        private readonly Dictionary<PerceptionTarget2D, Track> tracks = new();
        private readonly List<TargetPerceptionInfo> observations = new();
        private readonly List<HeardEvent> heard = new();
        private readonly Queue<Action> commands = new();
        private readonly List<Collider2D> colliders = new(16);
        private readonly List<RaycastHit2D> hits = new(8);
        private readonly HashSet<PerceptionTarget2D> seen = new();
        private readonly List<PerceptionTarget2D> remove = new();
        internal readonly List<PerceptionChange> Changes = new();
        internal readonly List<TargetPerceptionInfo> Forgotten = new();
        internal IReadOnlyList<TargetPerceptionInfo> Observations { get; }
        internal IReadOnlyList<HeardEvent> HeardEvents { get; }
        internal bool SightEnabled, HearingEnabled;
        internal bool Changed;
        private float timer;
        /// <summary>复制配置并建立长期复用的只读集合包装。</summary>
        internal PerceptionSession2D(PerceptionSettings2D settings)
        {
            Settings = settings.CopyValidated(); SightEnabled = Settings.SightEnabled; HearingEnabled = Settings.HearingEnabled;
            Observations = observations.AsReadOnly(); HeardEvents = heard.AsReadOnly();
        }
        /// <summary>命令在下个批次执行，回调不能重入当前状态提交。</summary>
        internal void Enqueue(Action command) => commands.Enqueue(command);
        /// <summary>获取现有记录；生命周期不同的旧目标必须先被清理。</summary>
        private Track GetTrack(PerceptionTarget2D target)
        {
            if (tracks.TryGetValue(target, out var track) && track.Generation != target.Generation)
            { RemoveTrack(track, PerceptionChangeReason.TargetInvalidated); tracks.Remove(target); track = null; }
            if (track == null) { track = new Track { Target = target, Generation = target.Generation }; tracks.Add(target, track); }
            return track;
        }
        /// <summary>记录声音上报时的来源代次，丢弃注销后才到达的旧声音。</summary>
        internal void QueueNoise(NoiseEvent2D noise, ulong id, double time, Vector2 receiver)
        {
            bool sourced = !ReferenceEquals(noise.Source, null);
            ulong generation = sourced ? noise.Source.Generation : 0;
            Enqueue(() =>
            {
                if (sourced && (noise.Source == null || !noise.Source.IsRegistered || noise.Source.Generation != generation)) return;
                var stimulus = new PerceptionStimulus(PerceptionSense.Hearing, noise.Position, receiver, time, noise.Loudness, noise.Tag, false);
                if (sourced) GetTrack(noise.Source).Hearing = stimulus;
                else heard.Add(new HeardEvent(id, stimulus));
                Changes.Add(new PerceptionChange(noise.Source, generation, PerceptionSense.Hearing, PerceptionChangeReason.Heard, stimulus, id)); Changed = true;
            });
        }
        /// <summary>切换感官；视觉关闭结束当前可见状态但不删除记忆。</summary>
        internal void SetEnabled(PerceptionSense sense, bool enabled)
        {
            if (sense == PerceptionSense.Hearing) { if (HearingEnabled != enabled) Changed = true; HearingEnabled = enabled; return; }
            if (SightEnabled == enabled) return;
            SightEnabled = enabled; Changed = true;
            if (enabled) timer = 0;
            else foreach (var track in tracks.Values) EndSight(track, PerceptionChangeReason.SenseDisabled);
        }
        /// <summary>结束视觉状态且保留上一次成功确认的事实。</summary>
        private void EndSight(Track track, PerceptionChangeReason reason)
        {
            if (!track.Sight.HasValue || !track.Sight.Value.IsCurrent) return;
            track.Sight = track.Sight.Value.EndCurrent(); Changed = true;
            Changes.Add(new PerceptionChange(track.Target, track.Generation, PerceptionSense.Sight, reason, track.Sight.Value));
        }
        /// <summary>删除指定感官记录，整体目标在批次收尾统一删除。</summary>
        internal void ForgetSense(PerceptionTarget2D target, PerceptionSense sense, PerceptionChangeReason reason)
        {
            if (ReferenceEquals(target, null) || !tracks.TryGetValue(target, out var track)) return;
            var value = sense == PerceptionSense.Sight ? track.Sight : track.Hearing;
            if (!value.HasValue) return;
            Changes.Add(new PerceptionChange(target, track.Generation, sense, reason, value.Value));
            if (sense == PerceptionSense.Sight) track.Sight = null; else track.Hearing = null;
            Changed = true;
        }
        /// <summary>删除目标的所有感官记录。</summary>
        internal void ForgetTarget(PerceptionTarget2D target, ulong? generation, PerceptionChangeReason reason)
        {
            if (ReferenceEquals(target, null) || !tracks.TryGetValue(target, out var track) || (generation.HasValue && generation != track.Generation)) return;
            RemoveTrack(track, reason); tracks.Remove(target);
        }
        /// <summary>保存删除原因及原记录后移除目标事实。</summary>
        private void RemoveTrack(Track track, PerceptionChangeReason reason)
        {
            if (track.Sight.HasValue) Changes.Add(new PerceptionChange(track.Target, track.Generation, PerceptionSense.Sight, reason, track.Sight.Value));
            if (track.Hearing.HasValue) Changes.Add(new PerceptionChange(track.Target, track.Generation, PerceptionSense.Hearing, reason, track.Hearing.Value));
            Forgotten.Add(track.Snapshot()); Changed = true;
        }
        /// <summary>清除目标和无来源事件，不改变感官开关。</summary>
        internal void Clear()
        {
            foreach (var track in tracks.Values) RemoveTrack(track, PerceptionChangeReason.Cleared);
            tracks.Clear();
            foreach (var item in heard) Changes.Add(new PerceptionChange(null, 0, PerceptionSense.Hearing, PerceptionChangeReason.Cleared, item.Stimulus, item.Id));
            Changed |= heard.Count > 0; heard.Clear();
        }
        /// <summary>重置对象池生命周期，取消尚未投递的旧声音与命令。</summary>
        internal void Reset()
        {
            commands.Clear(); Changes.Clear(); Forgotten.Clear(); ResetState();
        }
        /// <summary>切换环境时取消旧环境命令，保留绑定后才提交的新命令。</summary>
        internal void Rebind()
        {
            commands.Clear(); Enqueue(ResetState);
        }
        /// <summary>清理旧状态并恢复配置，不取消属于新环境的排队操作。</summary>
        private void ResetState()
        {
            Clear(); SightEnabled = Settings.SightEnabled; HearingEnabled = Settings.HearingEnabled; timer = 0; Changed = true;
        }
        /// <summary>启用观察者时安排立即扫描。</summary>
        internal void RequestScan() => timer = 0;
        /// <summary>暂停观察结束持续视觉，保留事实等待恢复批次独立老化。</summary>
        internal void SuspendSight()
        {
            foreach (var track in tracks.Values) EndSight(track, PerceptionChangeReason.Lost);
        }
        /// <summary>执行单个原子批次；时间由宿主统一提供。</summary>
        internal void Advance(float delta, double now, Vector2 origin, Vector2 facing, Transform owner, PerceptionWorld2D world)
        {
            Changes.Clear(); Forgotten.Clear(); Changed = false;
            // 固定本批命令边界，通知阶段提交的操作留待下一批。
            int count = commands.Count;
            for (int i = 0; i < count && commands.Count > 0; i++) commands.Dequeue()();
            remove.Clear();
            foreach (var track in tracks.Values)
                if (track.Target == null || !track.Target.IsRegistered || track.Target.Generation != track.Generation || track.Target.World != world) remove.Add(track.Target);
            foreach (var target in remove) ForgetTarget(target, null, PerceptionChangeReason.TargetInvalidated);
            // 恢复时先老化，再重新采样，过期信息不能带入下一次生命周期判断。
            Expire(now);
            timer -= delta;
            if (SightEnabled && world != null && world.isActiveAndEnabled && timer <= 0)
            { timer = Settings.ScanInterval; Scan(origin, facing, owner, world, now); Changed = true; }
            // 长帧或恢复后的扫描可能刚结束已超龄视觉，本批即清理，不泄漏给决策层。
            Expire(now);
            remove.Clear();
            foreach (var track in tracks.Values) if (!track.Sight.HasValue && !track.Hearing.HasValue) remove.Add(track.Target);
            foreach (var target in remove) { Forgotten.Add(tracks[target].Snapshot()); tracks.Remove(target); Changed = true; }
            // 同代目标可能在清理后于本批重获；整体遗忘必须与最终提交状态一致。
            Forgotten.RemoveAll(item => tracks.TryGetValue(item.Target, out var track) && track.Generation == item.Generation);
            if (Changed)
            {
                observations.Clear();
                foreach (var track in tracks.Values) observations.Add(track.Snapshot());
            }
        }
        /// <summary>按感官独立过期，持续确认的视觉不按时间删除。</summary>
        private void Expire(double now)
        {
            foreach (var track in tracks.Values)
            {
                if (track.Sight.HasValue && !track.Sight.Value.IsCurrent && Settings.SightMemory > 0 && now - track.Sight.Value.Time >= Settings.SightMemory)
                    ForgetSense(track.Target, PerceptionSense.Sight, PerceptionChangeReason.Expired);
                if (track.Hearing.HasValue && Settings.HearingMemory > 0 && now - track.Hearing.Value.Time >= Settings.HearingMemory)
                    ForgetSense(track.Target, PerceptionSense.Hearing, PerceptionChangeReason.Expired);
            }
            for (int i = heard.Count - 1; i >= 0; i--)
                if (now - heard[i].Stimulus.Time >= Settings.AnonymousMemory)
                { var item = heard[i]; heard.RemoveAt(i); Changes.Add(new PerceptionChange(null, 0, PerceptionSense.Hearing, PerceptionChangeReason.Expired, item.Stimulus, item.Id)); Changed = true; }
        }
        /// <summary>复用可增长物理结果，目标身份去重后按距离、视角、遮挡检测。</summary>
        private void Scan(Vector2 origin, Vector2 facing, Transform owner, PerceptionWorld2D world, double now)
        {
            var filter = new ContactFilter2D(); filter.SetLayerMask(Settings.TargetLayers); filter.useTriggers = true;
            Physics2D.OverlapCircle(origin, Settings.LoseSightDistance, filter, colliders); seen.Clear();
            foreach (var collider in colliders)
            {
                if (collider == null) continue;
                var target = collider.GetComponentInParent<PerceptionTarget2D>();
                if (target == null || !target.IsRegistered || target.World != world || !target.SightDetectable || target.TargetRoot == owner || target.transform.IsChildOf(owner) || !seen.Add(target)) continue;
                bool current = tracks.TryGetValue(target, out var old) && old.Generation == target.Generation && old.Sight.HasValue && old.Sight.Value.IsCurrent;
                Vector2 point = target.SightPosition; Vector2 offset = point - origin;
                float radius = current ? Settings.LoseSightDistance : Settings.SightDistance;
                bool visible = offset.sqrMagnitude <= radius * radius && (offset.sqrMagnitude <= 0.000001f || Vector2.Angle(facing, offset) <= Settings.ViewAngle * 0.5f);
                if (visible && Settings.ObstacleLayers.value != 0)
                {
                    var blockers = new ContactFilter2D(); blockers.SetLayerMask(Settings.ObstacleLayers); blockers.useTriggers = false;
                    Physics2D.Linecast(origin, point, blockers, hits);
                    foreach (var hit in hits)
                    {
                        if (hit.collider == null || hit.collider.transform.IsChildOf(owner) || hit.collider.GetComponentInParent<PerceptionTarget2D>() == target) continue;
                        visible = false; break;
                    }
                }
                if (!visible) { if (old != null) EndSight(old, PerceptionChangeReason.Lost); continue; }
                var track = GetTrack(target);
                var stimulus = new PerceptionStimulus(PerceptionSense.Sight, point, origin, now, 1, "", true);
                track.Sight = stimulus;
                if (!current) Changes.Add(new PerceptionChange(target, target.Generation, PerceptionSense.Sight, PerceptionChangeReason.Acquired, stimulus));
            }
            foreach (var track in tracks.Values)
                if (!seen.Contains(track.Target) || !track.Target.SightDetectable) EndSight(track, PerceptionChangeReason.Lost);
        }
    }
}
