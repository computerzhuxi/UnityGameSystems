using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Runtime.CompilerServices;

namespace Computerzhuxi.Perception2D.Tests
{
    /// <summary>在没有 GameObject 的环境中验证共享注册表和核心批次契约。</summary>
    public sealed class PerceptionCoreTests
    {
        /// <summary>公开配置是防御性快照，不能绕开验证或修改核心当前参数。</summary>
        [Test]
        public void SettingsSnapshot_CannotMutateCore()
        {
            var core = new PerceptionCore2D(new PerceptionTargetRegistry());
            PerceptionSettings2D exposed = core.Settings;
            exposed.SightMemory = float.NaN;
            Assert.That(core.Settings.SightMemory, Is.EqualTo(10));
            var next = new PerceptionSettings2D { HearingRange = 20 };
            core.UpdateSettings(next);
            next.HearingRange = float.NaN;
            core.Advance(1);
            Assert.That(core.Settings.HearingRange, Is.EqualTo(20));
        }

        /// <summary>非法帧和已淘汰来源不得污染后续有效输入的时间水位。</summary>
        [Test]
        public void RejectedInputs_DoNotAdvanceTimeWatermark()
        {
            var registry = new PerceptionTargetRegistry();
            var core = new PerceptionCore2D(registry);
            core.Advance(1);
            Assert.Throws<ArgumentException>(() => core.SubmitSightFrame(
                new[] { new SightObservation2D(default, Vector2.zero, Vector2.zero) }, 100));
            core.Advance(2);
            ulong oldSource = core.SightSourceGeneration;
            core.EndSightSource();
            core.Advance(2);
            core.SubmitSightFrame(Array.Empty<SightObservation2D>(), 1, oldSource);
            PerceptionTargetHandle dead = registry.Register();
            registry.Unregister(dead);
            core.ReportHearing(Vector2.zero, Vector2.zero, 100, source: dead);
            core.Advance(3);
            Assert.That(core.Observations, Is.Empty);
        }

        /// <summary>完整重置取消已排队的未来输入及其时间水位。</summary>
        [Test]
        public void Reset_CancelsQueuedFutureInputTime()
        {
            var core = new PerceptionCore2D(new PerceptionTargetRegistry());
            core.SubmitSightFrame(Array.Empty<SightObservation2D>(), 100);
            core.ResetForReuse();
            Assert.DoesNotThrow(() => core.Advance(1));
        }

        /// <summary>注销释放活动条目，仍存活的对象池对象重注册使用原编号和新代次。</summary>
        [Test]
        public void Registry_ReleasesInactiveEntriesAndObjects()
        {
            var registry = new PerceptionTargetRegistry();
            object pooled = new();
            PerceptionTargetHandle old = registry.Register(pooled);
            registry.Unregister(old);
            Assert.That(registry.ActiveCount, Is.Zero);
            PerceptionTargetHandle current = registry.Register(pooled);
            Assert.That(current.Id, Is.EqualTo(old.Id));
            Assert.That(current.Generation, Is.GreaterThan(old.Generation));
            Assert.That(registry.IsValid(old), Is.False);
            registry.Unregister(current);
            for (int index = 0; index < 1000; index++)
            {
                PerceptionTargetHandle anonymous = registry.Register();
                registry.Unregister(anonymous);
            }
            Assert.That(registry.ActiveCount, Is.Zero);
            WeakReference released = CreateAndUnregisterObject(registry);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.That(released.IsAlive, Is.False);
        }

        /// <summary>把临时关联对象的生命周期隔离在单独栈帧中，检查弱身份映射不强持有它。</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CreateAndUnregisterObject(PerceptionTargetRegistry registry)
        {
            object temporary = new();
            var weak = new WeakReference(temporary);
            PerceptionTargetHandle handle = registry.Register(temporary);
            registry.Unregister(handle);
            return weak;
        }
        /// <summary>未提交帧保留视觉，空帧结束视觉，非空帧结束缺席目标。</summary>
        [Test]
        public void CompleteSightFrames_SeparateNoFrameFromEmptyAndMissing()
        {
            var registry = new PerceptionTargetRegistry();
            PerceptionTargetHandle first = registry.Register();
            PerceptionTargetHandle second = registry.Register();
            var core = new PerceptionCore2D(registry);
            var frame = new List<SightObservation2D>
            {
                new(first, Vector2.right, Vector2.zero),
                new(second, Vector2.up, Vector2.zero),
                new(first, Vector2.right * 2, Vector2.zero)
            };
            int acquired = 0;
            core.SenseUpdated += change => { if (change.Reason == PerceptionChangeReason.Acquired) acquired++; };
            core.SubmitSightFrame(frame, 1);
            frame.Clear();
            core.Advance(1);
            Assert.That(acquired, Is.EqualTo(2));
            Assert.That(core.Observations.Count, Is.EqualTo(2));
            Assert.That(core.TryGetObservation(first, out TargetPerceptionInfo firstInfo), Is.True);
            Assert.That(firstInfo.Sight.Value.Position, Is.EqualTo(Vector2.right * 2));

            core.Advance(2);
            Assert.That(core.TryGetObservation(first, out firstInfo) && firstInfo.IsVisible, Is.True);
            core.SubmitSightFrame(new[] { new SightObservation2D(first, Vector2.right * 3, Vector2.zero) }, 3);
            core.Advance(3);
            Assert.That(acquired, Is.EqualTo(2));
            Assert.That(core.TryGetObservation(second, out TargetPerceptionInfo secondInfo) && secondInfo.IsVisible, Is.False);
            Assert.That(secondInfo.Sight.Value.Position, Is.EqualTo(Vector2.up));
            core.SubmitSightFrame(Array.Empty<SightObservation2D>(), 4);
            core.Advance(4);
            Assert.That(core.TryGetObservation(first, out firstInfo) && firstInfo.IsVisible, Is.False);
            Assert.That(firstInfo.Sight.Value.Position, Is.EqualTo(Vector2.right * 3));
        }

        /// <summary>多个观察者共享身份但分别记忆，旧代声音和错误注册表输入都被隔离。</summary>
        [Test]
        public void RegistryGeneration_SeparatesObserversAndPooledInput()
        {
            var registry = new PerceptionTargetRegistry();
            object identity = new();
            PerceptionTargetHandle oldHandle = registry.Register(identity);
            var first = new PerceptionCore2D(registry);
            var second = new PerceptionCore2D(registry);
            first.ReportHearing(Vector2.right, Vector2.zero, 1, source: oldHandle);
            first.Advance(1);
            second.Advance(1);
            Assert.That(first.Observations.Count, Is.EqualTo(1));
            Assert.That(second.Observations, Is.Empty);

            first.ReportHearing(Vector2.up, Vector2.zero, 2, source: oldHandle);
            registry.Unregister(oldHandle);
            PerceptionTargetHandle next = registry.Register(identity);
            Assert.That(next.Id, Is.EqualTo(oldHandle.Id));
            Assert.That(next.Generation, Is.GreaterThan(oldHandle.Generation));
            first.Advance(2);
            Assert.That(first.Observations, Is.Empty);
            Assert.Throws<ArgumentException>(() => first.SubmitSightFrame(
                new[] { new SightObservation2D(new PerceptionTargetRegistry().Register(), Vector2.zero, Vector2.zero) }, 3));
        }

        /// <summary>视觉来源代次阻断旧帧，来源切换只结束视觉而保留听觉。</summary>
        [Test]
        public void SightSourceSwitch_RejectsQueuedOldFramesAndKeepsHearing()
        {
            var registry = new PerceptionTargetRegistry();
            PerceptionTargetHandle handle = registry.Register();
            var core = new PerceptionCore2D(registry);
            core.SubmitSightFrame(new[] { new SightObservation2D(handle, Vector2.right, Vector2.zero) }, 1);
            core.ReportHearing(Vector2.up, Vector2.zero, 1, source: handle);
            core.Advance(1);
            ulong oldSource = core.SightSourceGeneration;
            core.SubmitSightFrame(new[] { new SightObservation2D(handle, Vector2.right * 2, Vector2.zero) }, 2, oldSource);
            core.EndSightSource();
            core.Advance(2);
            Assert.That(core.TryGetObservation(handle, out TargetPerceptionInfo info), Is.True);
            Assert.That(info.IsVisible, Is.False);
            Assert.That(info.Sight.Value.Position, Is.EqualTo(Vector2.right));
            Assert.That(info.Hearing.Value.Position, Is.EqualTo(Vector2.up));
        }

        /// <summary>独立过期、时间验证和综合位置选择保持在核心单一实现中。</summary>
        [Test]
        public void MemoryAndTime_UseLastSuccessfulObservation()
        {
            var registry = new PerceptionTargetRegistry();
            PerceptionTargetHandle handle = registry.Register();
            var core = new PerceptionCore2D(registry, new PerceptionSettings2D { SightMemory = 2, HearingMemory = 5, AnonymousMemory = 1 });
            core.SubmitSightFrame(new[] { new SightObservation2D(handle, Vector2.right, Vector2.zero) }, 1);
            core.Advance(1);
            core.ReportHearing(Vector2.up, Vector2.zero, 2, source: handle);
            core.ReportHearing(Vector2.one, Vector2.zero, 2);
            core.SubmitSightFrame(Array.Empty<SightObservation2D>(), 2);
            core.Advance(2);
            Assert.That(core.TryGetKnownPosition(handle, out PerceptionStimulus known) && known.Position == Vector2.up, Is.True);
            core.Advance(3);
            Assert.That(core.HeardEvents, Is.Empty);
            core.Advance(4);
            Assert.That(core.TryGetObservation(handle, out TargetPerceptionInfo info), Is.True);
            Assert.That(info.Sight.HasValue, Is.False);
            Assert.That(info.Hearing.HasValue, Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => core.Advance(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.ReportHearing(Vector2.zero, Vector2.zero, double.NaN));
        }

        /// <summary>最终查询先于事件提交，回调中的命令进入下一批，异常不阻止后续订阅者。</summary>
        [Test]
        public void Events_CommitBeforeCallbacksAndDeferMutations()
        {
            var registry = new PerceptionTargetRegistry();
            PerceptionTargetHandle handle = registry.Register();
            var core = new PerceptionCore2D(registry);
            var order = new List<string>();
            core.SenseUpdated += change =>
            {
                if (change.Reason == PerceptionChangeReason.Heard)
                    Assert.That(core.Observations.Count, Is.EqualTo(1));
                order.Add("sense");
                if (change.Reason == PerceptionChangeReason.Heard) core.ForgetTarget(handle);
                Assert.Throws<InvalidOperationException>(() => core.Advance(1));
            };
            core.SenseUpdated += _ => throw new InvalidOperationException("core-subscriber-test");
            core.SenseUpdated += _ => order.Add("after-error");
            core.TargetForgotten += _ => order.Add("forgotten");
            core.ObservationsUpdated += () => order.Add("batch");
            core.ReportHearing(Vector2.zero, Vector2.zero, 1, source: handle);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: core-subscriber-test");
            core.Advance(1);
            CollectionAssert.AreEqual(new[] { "sense", "after-error", "batch" }, order);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: core-subscriber-test");
            core.Advance(2);
            CollectionAssert.AreEqual(new[] { "sense", "after-error", "batch", "sense", "after-error", "forgotten", "batch" }, order);
        }
    }
}
