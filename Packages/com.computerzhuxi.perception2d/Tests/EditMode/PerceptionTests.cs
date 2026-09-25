using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Computerzhuxi.Perception2D.Tests
{
    /// <summary>在独占临时对象上验证感官、记忆、身份与事件契约。</summary>
    public sealed class PerceptionTests
    {
        private sealed class FixedSightScanner : PhysicsSightScanner2D
        {
            internal PerceptionTargetHandle Target;
            internal int Calls;
            internal float LastHearingRange;
            /// <summary>返回指定完整帧，用于验证扫描器切换与来源结束。</summary>
            public override void Scan(PhysicsScene2D scene, PerceptionTargetRegistry registry, PerceptionSettings2D settings,
                Vector2 origin, Vector2 facing, Transform owner, IReadOnlyList<TargetPerceptionInfo> observations,
                List<SightObservation2D> results)
            {
                Calls++;
                LastHearingRange = settings.HearingRange;
                results.Clear();
                if (Target.Id != 0) results.Add(new SightObservation2D(Target, Vector2.right, origin));
            }
        }
        private readonly List<GameObject> objects = new();
        private PerceptionWorld2D world;
        /// <summary>创建与其他测试隔离的环境。</summary>
        [SetUp] public void Setup() { world = ObjectAt("World", Vector2.zero).AddComponent<PerceptionWorld2D>(); }
        /// <summary>包只向自身测试授予内部访问，不包含消费项目的友元授权。</summary>
        [Test] public void InternalAccess_IsLimitedToPackageTests()
        {
            var friends=typeof(PerceptionObserver2D).Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute),false)
                .Cast<InternalsVisibleToAttribute>().Select(value=>value.AssemblyName).ToArray();
            CollectionAssert.AreEquivalent(new[]{"Computerzhuxi.Perception2D.EditModeTests"},friends);
        }
        /// <summary>编辑器中 Gizmo 跟随初始朝向的序列化修改，零方向与运行时初始回退一致。</summary>
        [Test] public void GizmoFacing_UsesLiveSerializedDirectionOutsidePlayMode()
        {
            var observer=Observer(); var serialized=new SerializedObject(observer);
            var direction=serialized.FindProperty("initialFacing");
            direction.vector2Value=Vector2.up*3; serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(observer.DebugFacingDirection,Is.EqualTo(Vector2.up));
            direction.vector2Value=Vector2.left; serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(observer.DebugFacingDirection,Is.EqualTo(Vector2.left));
            direction.vector2Value=Vector2.zero; serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(observer.DebugFacingDirection,Is.EqualTo(Vector2.right));
        }
        /// <summary>只销毁本测试登记的临时对象。</summary>
        [TearDown] public void Cleanup() { for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]); objects.Clear(); }
        /// <summary>创建并登记测试对象。</summary>
        private GameObject ObjectAt(string name, Vector2 position) { var go = new GameObject(name); go.transform.position = position; objects.Add(go); return go; }
        /// <summary>创建绑定环境的目标。</summary>
        private PerceptionTarget2D Target(Vector2 position, int colliders = 1)
        {
            var go = ObjectAt("Target", position); var target = go.AddComponent<PerceptionTarget2D>(); target.Bind(world);
            for (int i = 0; i < colliders; i++) { var child = ObjectAt("Collider", position); child.transform.SetParent(go.transform); child.AddComponent<CircleCollider2D>().radius = 0.1f; }
            Physics2D.SyncTransforms(); return target;
        }
        /// <summary>创建显式配置的观察者。</summary>
        private PerceptionObserver2D Observer(PerceptionSettings2D settings = null)
        {
            var go = ObjectAt("Observer", Vector2.zero); go.SetActive(false);
            var observer = go.AddComponent<PerceptionObserver2D>(); observer.Configure(world, settings ?? new PerceptionSettings2D()); go.SetActive(true); observer.SetFacingDirection(Vector2.right); return observer;
        }
        /// <summary>组件手动帧与普通 C# 核心在相同输入和时间下得到相同感知事实。</summary>
        [Test] public void ManualComponentAndCore_ShareIdenticalStateRules()
        {
            var target = Target(Vector2.right);
            var observer = Observer();
            observer.SetAutomaticSight(false);
            var plain = new PerceptionCore2D(world.Registry);
            var componentEvents = new List<PerceptionChangeReason>();
            var coreEvents = new List<PerceptionChangeReason>();
            observer.SenseUpdated += change => componentEvents.Add(change.Reason);
            plain.SenseUpdated += change => coreEvents.Add(change.Reason);
            var frame = new[] { new SightObservation2D(target.Handle, Vector2.right, Vector2.zero) };
            observer.SubmitSightFrame(frame, 1);
            plain.SubmitSightFrame(frame, 1);
            observer.Advance(0, 1);
            plain.Advance(1);
            Assert.That(observer.Observations.Count, Is.EqualTo(plain.Observations.Count));
            Assert.That(observer.Observations[0].Sight.Value.Position, Is.EqualTo(plain.Observations[0].Sight.Value.Position));
            Assert.That(observer.Observations[0].IsVisible, Is.EqualTo(plain.Observations[0].IsVisible));

            observer.SubmitSightFrame(Array.Empty<SightObservation2D>(), 2);
            plain.SubmitSightFrame(Array.Empty<SightObservation2D>(), 2);
            observer.Advance(0, 2);
            plain.Advance(2);
            Assert.That(observer.Observations[0].IsVisible, Is.False);
            Assert.That(observer.Observations[0].Sight.Value.Time, Is.EqualTo(plain.Observations[0].Sight.Value.Time));

            observer.Core.ReportHearing(Vector2.up, Vector2.zero, 3, source: target.Handle);
            plain.ReportHearing(Vector2.up, Vector2.zero, 3, source: target.Handle);
            observer.Advance(0, 3);
            plain.Advance(3);
            Assert.That(observer.Observations[0].Hearing.Value.Position, Is.EqualTo(plain.Observations[0].Hearing.Value.Position));
            observer.Advance(0, 8);
            plain.Advance(8);
            Assert.That(observer.Observations[0].Hearing.HasValue, Is.False);
            CollectionAssert.AreEqual(coreEvents, componentEvents);
        }

        /// <summary>手动帧仅在手动模式接受，替换扫描器结束旧来源并使用新完整帧。</summary>
        [Test] public void SightModeAndScannerReplacement_IsolateSources()
        {
            var target = Target(Vector2.right);
            var observer = Observer();
            Assert.Throws<InvalidOperationException>(() => observer.SubmitSightFrame(
                new[] { new SightObservation2D(target.Handle, Vector2.right, Vector2.zero) }, 1));
            var first = new FixedSightScanner { Target = target.Handle };
            observer.SetSightScanner(first);
            observer.Advance(0, 1);
            Assert.That(first.Calls, Is.EqualTo(1));
            Assert.That(observer.TryGetObservation(target, out var info) && info.IsVisible, Is.True);
            int switched = 0;
            observer.SenseUpdated += change => { if (change.Reason == PerceptionChangeReason.SourceChanged) switched++; };
            var empty = new FixedSightScanner();
            observer.SetSightScanner(empty);
            observer.Advance(0, 2);
            Assert.That(empty.Calls, Is.EqualTo(1));
            Assert.That(switched, Is.EqualTo(1));
            Assert.That(observer.TryGetObservation(target, out info) && info.IsVisible, Is.False);
            observer.SetAutomaticSight(false);
            observer.SubmitSightFrame(new[] { new SightObservation2D(target.Handle, Vector2.right, Vector2.zero) }, 3);
            observer.Advance(0, 3);
            Assert.That(observer.TryGetObservation(target, out info) && info.IsVisible, Is.True);
        }

        /// <summary>大扫描间隔下重新启用视觉仍立即采样，并使用发现距离。</summary>
        [Test] public void SightReenable_ImmediatelyScansAtDiscoveryDistance()
        {
            var observer = Observer(new PerceptionSettings2D { ScanInterval = 10 });
            var target = Target(Vector2.right * 2);
            observer.Advance(0, 1);
            observer.SetSenseEnabled(PerceptionSense.Sight, false);
            observer.Advance(0, 2);
            target.transform.position = Vector2.right * 7;
            Physics2D.SyncTransforms();
            observer.SetSenseEnabled(PerceptionSense.Sight, true);
            observer.Advance(0, 3);
            Assert.That(observer.TryGetObservation(target, out var info) && info.IsVisible, Is.False);
            target.transform.position = Vector2.right * 2;
            Physics2D.SyncTransforms();
            observer.SetSenseEnabled(PerceptionSense.Sight, false);
            observer.Advance(0, 4);
            observer.SetSenseEnabled(PerceptionSense.Sight, true);
            observer.Advance(0, 5);
            Assert.That(observer.TryGetObservation(target, out info) && info.IsVisible, Is.True);
        }

        /// <summary>大扫描间隔下重置先提交，再于下一批立即重新发现。</summary>
        [Test] public void Reset_RescansOnNextBatchWithLongInterval()
        {
            var observer = Observer(new PerceptionSettings2D { ScanInterval = 10 });
            var target = Target(Vector2.right * 2);
            observer.Advance(0, 1);
            observer.ResetForReuse();
            observer.Advance(0, 2);
            Assert.That(observer.Observations, Is.Empty);
            observer.Advance(0, 2.1);
            Assert.That(observer.TryGetObservation(target, out var info) && info.IsVisible, Is.True);
        }

        /// <summary>替换或销毁组件后，外部保留的旧核心不能继续转发组件通知。</summary>
        [Test] public void ReplacedAndDestroyedCores_DoNotForwardObserverEvents()
        {
            var observer = Observer(new PerceptionSettings2D { SightEnabled = false });
            PerceptionTarget2D target = Target(Vector2.right);
            int notifications = 0;
            observer.SenseUpdated += _ => notifications++;
            PerceptionCore2D old = observer.Core;
            var next = ObjectAt("NextWorld", Vector2.zero).AddComponent<PerceptionWorld2D>();
            observer.Bind(next);
            old.ReportHearing(Vector2.right, Vector2.zero, 1, source: target.Handle);
            old.Advance(1);
            Assert.That(notifications, Is.Zero);

            observer.enabled = false;
            PerceptionCore2D configuredOld = observer.Core;
            observer.Configure(next, new PerceptionSettings2D { SightEnabled = false });
            configuredOld.ReportHearing(Vector2.zero, Vector2.zero, 2);
            configuredOld.Advance(2);
            Assert.That(notifications, Is.Zero);

            PerceptionCore2D destroyedCore = observer.Core;
            UnityEngine.Object.DestroyImmediate(observer.gameObject);
            destroyedCore.ReportHearing(Vector2.zero, Vector2.zero, 3);
            destroyedCore.Advance(3);
            Assert.That(notifications, Is.Zero);
        }

        /// <summary>运行时更新配置保留核心、感官开关与现有记忆。</summary>
        [Test] public void RuntimeSettings_KeepCoreAndMemory()
        {
            var observer = Observer(new PerceptionSettings2D { SightEnabled = false });
            var target = Target(Vector2.right);
            observer.Core.ReportHearing(Vector2.up, Vector2.zero, 1, source: target.Handle);
            observer.Advance(0, 1);
            PerceptionCore2D original = observer.Core;
            observer.UpdateSettings(new PerceptionSettings2D { SightEnabled = false, HearingRange = 20 });
            observer.Advance(0, 2);
            Assert.That(observer.Core, Is.SameAs(original));
            Assert.That(observer.TryGetObservation(target, out var info), Is.True);
            Assert.That(info.Hearing.Value.Position, Is.EqualTo(Vector2.up));
            Assert.That(observer.Core.Settings.HearingRange, Is.EqualTo(20));
        }
        /// <summary>同批重置取消待提交配置时，组件扫描配置仍与核心保持一致。</summary>
        [Test] public void Reset_CancelsPendingComponentSettings()
        {
            var observer = Observer();
            var scanner = new FixedSightScanner();
            observer.SetSightScanner(scanner);
            observer.UpdateSettings(new PerceptionSettings2D { HearingRange = 20 });
            observer.ResetForReuse();
            observer.Advance(0, 1);
            observer.Advance(0, 2);
            Assert.That(observer.Core.Settings.HearingRange, Is.EqualTo(10));
            Assert.That(scanner.LastHearingRange, Is.EqualTo(10));
        }
        /// <summary>密集子碰撞体不能截断结果，也不能重复目标。</summary>
        [Test] public void DenseColliders_AreCompleteAndNormalized()
        {
            var observer = Observer(); var first = Target(new Vector2(2, 0), 40); var second = Target(new Vector2(3, 0));
            observer.Advance(0.1f, 1); Assert.That(observer.Observations.Count, Is.EqualTo(2)); Assert.That(observer.TryGetObservation(first, out var info), Is.True); Assert.That(info.IsVisible, Is.True);
            second.transform.position = new Vector2(-3, 0); Physics2D.SyncTransforms(); observer.Advance(0.1f, 2);
            Assert.That(observer.TryGetObservation(second, out info), Is.True); Assert.That(info.IsVisible, Is.False);
        }
        /// <summary>已发现目标使用丢失距离，但仍受角度和遮挡约束。</summary>
        [Test] public void Sight_HysteresisAngleAndOcclusion()
        {
            var config = new PerceptionSettings2D { ObstacleLayers = 1 << 8 }; var observer = Observer(config); var target = Target(new Vector2(5, 0));
            observer.Advance(0.1f, 0); target.transform.position = new Vector2(7, 0); Physics2D.SyncTransforms(); observer.Advance(0.1f, 1);
            observer.TryGetObservation(target, out var info); Assert.That(info.IsVisible, Is.True);
            var wall = ObjectAt("Wall", new Vector2(3, 0)); wall.layer = 8; var collider = wall.AddComponent<BoxCollider2D>(); collider.isTrigger = true; Physics2D.SyncTransforms();
            observer.Advance(0.1f, 2); observer.TryGetObservation(target, out info); Assert.That(info.IsVisible, Is.True);
            collider.isTrigger = false; Physics2D.SyncTransforms(); observer.Advance(0.1f, 3); observer.TryGetObservation(target, out info); Assert.That(info.IsVisible, Is.False); Assert.That(info.Sight.Value.Position.x, Is.EqualTo(7));
            wall.SetActive(false); Physics2D.SyncTransforms(); observer.Advance(0.1f, 4); observer.TryGetObservation(target, out info); Assert.That(info.IsVisible, Is.False, "重新发现不能使用丢失距离");
        }
        /// <summary>声音位置固定，两种感官独立过期且最新记忆参与综合位置。</summary>
        [Test] public void HearingAndSight_AgeIndependently()
        {
            var observer = Observer(new PerceptionSettings2D { SightMemory = 1, HearingMemory = 5 }); var target = Target(new Vector2(2, 0));
            double now = Time.timeAsDouble; observer.Advance(0.1f, now); observer.SetSenseEnabled(PerceptionSense.Sight, false); observer.Advance(0, now);
            Assert.That(world.ReportNoise(new NoiseEvent2D(new Vector2(4, 0), source: target)), Is.EqualTo(1)); observer.Advance(0, now + 0.1);
            target.transform.position = new Vector2(100, 0); observer.Advance(0, now + 1.1);
            observer.TryGetObservation(target, out var info); Assert.That(info.Sight.HasValue, Is.False); Assert.That(info.Hearing.Value.Position.x, Is.EqualTo(4));
            Assert.That(observer.TryGetKnownPosition(target, out var position), Is.True); Assert.That(position.Sense, Is.EqualTo(PerceptionSense.Hearing));
            observer.Advance(0, now + 6); Assert.That(observer.Observations, Is.Empty);
        }
        /// <summary>声音范围与来源空值明确，关闭期间声音不补发。</summary>
        [Test] public void Hearing_GatesRangeAndAnonymousExpiry()
        {
            var observer = Observer(new PerceptionSettings2D { SightEnabled = false, HearingRange = 2, AnonymousMemory = 1 });
            Assert.That(world.ReportNoise(new NoiseEvent2D(new Vector2(3, 0))), Is.Zero);
            Assert.That(world.ReportNoise(new NoiseEvent2D(new Vector2(3, 0), 2, maxRange: 2)), Is.Zero);
            Assert.That(world.ReportNoise(new NoiseEvent2D(new Vector2(3, 0), 2)), Is.EqualTo(1));
            observer.Advance(0, Time.timeAsDouble); Assert.That(observer.HeardEvents.Count, Is.EqualTo(1));
            observer.SetSenseEnabled(PerceptionSense.Hearing, false); Assert.That(world.ReportNoise(new NoiseEvent2D(Vector2.zero)), Is.Zero);
            observer.Advance(0, Time.timeAsDouble); observer.SetSenseEnabled(PerceptionSense.Hearing, true); observer.Advance(0, Time.timeAsDouble + 2);
            Assert.That(observer.HeardEvents, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => new NoiseEvent2D(Vector2.zero, -1));
            Assert.That(world.ReportNoise(new NoiseEvent2D(Vector2.zero, 0)), Is.Zero);
        }
        /// <summary>对象池新代次不能继承旧声音，观察者重置也清除待处理事件。</summary>
        [Test] public void RegistrationAndReset_InvalidateQueuedNoise()
        {
            var observer = Observer(new PerceptionSettings2D { SightEnabled = false }); var target = Target(Vector2.right);
            ulong generation = target.Generation; world.ReportNoise(new NoiseEvent2D(Vector2.right, source: target)); target.Bind(null); target.Bind(world);
            Assert.That(target.Generation, Is.GreaterThan(generation)); observer.Advance(0, Time.timeAsDouble); Assert.That(observer.Observations, Is.Empty);
            observer.ResetForReuse(); world.ReportNoise(new NoiseEvent2D(Vector2.zero)); observer.Advance(0, Time.timeAsDouble); Assert.That(observer.HeardEvents, Is.Empty);
        }
        /// <summary>事件读取已提交状态，回调清理延后，订阅异常不阻断后续订阅者。</summary>
        [Test] public void Events_AreAtomicDeferredAndExceptionIsolated()
        {
            var observer = Observer(); Target(Vector2.right); var order = new List<string>();
            observer.SenseUpdated += change => { Assert.That(observer.Observations.Count, Is.EqualTo(1)); order.Add("sense"); observer.ClearMemory(); };
            observer.SenseUpdated += change => throw new InvalidOperationException("subscriber-test");
            observer.SenseUpdated += change => order.Add("after-error"); observer.ObservationsUpdated += () => order.Add("batch");
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: subscriber-test"); observer.Advance(0, 0);
            CollectionAssert.AreEqual(new[] { "sense", "after-error", "batch" }, order); Assert.That(observer.Observations.Count, Is.EqualTo(1));
        }
        /// <summary>不同观察者和环境不会共享记忆。</summary>
        [Test] public void WorldsAndObservers_AreIsolated()
        {
            var observer = Observer(); var another = Observer(new PerceptionSettings2D { HearingEnabled = false, SightEnabled = false });
            world.ReportNoise(new NoiseEvent2D(Vector2.zero)); observer.Advance(0, Time.timeAsDouble); another.Advance(0, Time.timeAsDouble);
            Assert.That(observer.HeardEvents.Count, Is.EqualTo(1)); Assert.That(another.HeardEvents, Is.Empty);
            var otherWorld = ObjectAt("OtherWorld", Vector2.zero).AddComponent<PerceptionWorld2D>(); Assert.That(otherWorld.ReportNoise(new NoiseEvent2D(Vector2.zero)), Is.Zero);
        }
        /// <summary>同时间记录由主导感官决定，但当前视觉优先。</summary>
        [Test] public void PositionPolicy_PrefersCurrentThenFreshThenDominant()
        {
            var sight = new PerceptionStimulus(PerceptionSense.Sight, Vector2.right, Vector2.zero, 1, 1, "", false);
            var hearing = new PerceptionStimulus(PerceptionSense.Hearing, Vector2.up, Vector2.zero, 1, 1, "", false);
            var info = new TargetPerceptionInfo(null, 1, sight, hearing); info.TryGetKnownPosition(PerceptionSense.Hearing, out var result); Assert.That(result.Position, Is.EqualTo(Vector2.up));
        }
        /// <summary>视觉关闭立即结束下一批可见状态，零时长记忆一直保留至显式删除。</summary>
        [Test] public void DisableAndForget_KeepIndependentMemory()
        {
            var observer=Observer(new PerceptionSettings2D { SightMemory=0 }); var target=Target(Vector2.right);
            observer.Advance(0,0); int lost=0, forgotten=0;
            observer.SenseUpdated+=c=>{if(c.Reason==PerceptionChangeReason.SenseDisabled)lost++;}; observer.TargetForgotten+=_=>forgotten++;
            observer.SetSenseEnabled(PerceptionSense.Sight,false); observer.Advance(0,1); observer.Advance(0,1000);
            Assert.That(lost,Is.EqualTo(1)); Assert.That(observer.Observations.Count,Is.EqualTo(1)); Assert.That(observer.Observations[0].IsVisible,Is.False);
            observer.ForgetTarget(target); observer.Advance(0,1001); Assert.That(observer.Observations,Is.Empty); Assert.That(forgotten,Is.EqualTo(1));
        }
        /// <summary>回调清理只在下一批生效，清理后才发布目标遗忘。</summary>
        [Test] public void ReentrantClear_IsDeferredToNextBatch()
        {
            var observer=Observer(new PerceptionSettings2D { SightEnabled=false }); var target=Target(Vector2.right); var order=new List<string>();
            observer.SenseUpdated+=change=>{order.Add(change.Reason.ToString()); if(change.Reason==PerceptionChangeReason.Heard)observer.ClearMemory();};
            observer.TargetForgotten+=_=>order.Add("Forgotten"); observer.ObservationsUpdated+=()=>order.Add("Batch");
            world.ReportNoise(new NoiseEvent2D(Vector2.right,source:target)); double now=Time.timeAsDouble; observer.Advance(0,now);
            Assert.That(observer.Observations.Count,Is.EqualTo(1)); observer.Advance(0,now);
            CollectionAssert.AreEqual(new[]{"Heard","Batch","Cleared","Forgotten","Batch"},order); Assert.That(observer.Observations,Is.Empty);
        }
        /// <summary>一批多个声音不会因目标聚合丢失通知，记忆只保留最新位置。</summary>
        [Test] public void MultipleNoises_NotifyEachAndKeepLatest()
        {
            var observer=Observer(new PerceptionSettings2D { SightEnabled=false }); var target=Target(Vector2.right); int events=0; observer.SenseUpdated+=_=>events++;
            world.ReportNoise(new NoiseEvent2D(Vector2.right,source:target)); world.ReportNoise(new NoiseEvent2D(Vector2.up,source:target)); observer.Advance(0,Time.timeAsDouble);
            Assert.That(events,Is.EqualTo(2)); Assert.That(observer.Observations.Count,Is.EqualTo(1)); Assert.That(observer.Observations[0].Hearing.Value.Position,Is.EqualTo(Vector2.up));
        }
        /// <summary>绑定后立即接受的声音和开关不能被延迟环境重置吞掉。</summary>
        [Test] public void Bind_PreservesNewEnvironmentCommands()
        {
            var observer=Observer(new PerceptionSettings2D { SightEnabled=false });
            var next=ObjectAt("NextWorld",Vector2.zero).AddComponent<PerceptionWorld2D>();
            observer.Bind(next); observer.SetSenseEnabled(PerceptionSense.Sight,true);
            Assert.That(next.ReportNoise(new NoiseEvent2D(Vector2.zero)),Is.EqualTo(1));
            observer.Advance(0,Time.timeAsDouble);
            Assert.That(observer.IsSenseEnabled(PerceptionSense.Sight),Is.True);
            Assert.That(observer.HeardEvents.Count,Is.EqualTo(1));
        }
        /// <summary>重置取消旧声音，包括当前批次尚未发布的声音通知。</summary>
        [Test] public void Reset_DoesNotPublishOldQueuedNoise()
        {
            var observer=Observer(new PerceptionSettings2D { SightEnabled=false }); int heard=0;
            observer.SenseUpdated+=change=>{if(change.Reason==PerceptionChangeReason.Heard)heard++;};
            world.ReportNoise(new NoiseEvent2D(Vector2.zero)); observer.ResetForReuse();
            observer.Advance(0,Time.timeAsDouble);
            Assert.That(observer.HeardEvents,Is.Empty); Assert.That(heard,Is.Zero);
        }
        /// <summary>同批重新发现的同代目标仍存在时，不发布整体遗忘。</summary>
        [Test] public void ClearAndReacquire_DoesNotPublishForgotten()
        {
            var observer=Observer(); var target=Target(Vector2.right); observer.Advance(0,0); int forgotten=0;
            observer.TargetForgotten+=_=>forgotten++; observer.ClearMemory(); observer.Advance(0.2f,1);
            Assert.That(observer.TryGetObservation(target,out var info)&&info.IsVisible,Is.True);
            Assert.That(forgotten,Is.Zero);
        }
        /// <summary>长时间未扫描后确认丢失，已过期视觉不得泄漏到本批查询。</summary>
        [Test] public void LateScan_ExpiresLostSightInSameBatch()
        {
            var observer=Observer(new PerceptionSettings2D { SightMemory=1 }); var target=Target(Vector2.right);
            observer.Advance(0,0); target.transform.position=Vector2.right*100; Physics2D.SyncTransforms();
            observer.Advance(10,10);
            Assert.That(observer.TryGetObservation(target,out _),Is.False);
        }
        /// <summary>角度零只沿朝向发现，完整圆角可检测背后目标。</summary>
        [TestCase(0,false)] [TestCase(360,true)] public void ViewAngle_Boundaries(float angle,bool expected)
        {
            var observer=Observer(new PerceptionSettings2D { ViewAngle=angle }); var target=Target(Vector2.left); observer.Advance(0,0);
            Assert.That(observer.TryGetObservation(target,out _),Is.EqualTo(expected));
        }
        /// <summary>非法配置在创建前失败，不依赖静默修正。</summary>
        [Test] public void InvalidSettingsAndDirections_AreRejected()
        {
            Assert.Throws<ArgumentException>(()=>new PerceptionSettings2D { SightDistance=10,LoseSightDistance=5 }.CopyValidated());
            Assert.Throws<ArgumentOutOfRangeException>(()=>new PerceptionSettings2D { HearingRange=float.NaN }.CopyValidated());
            var observer=Observer(); Assert.Throws<ArgumentException>(()=>observer.SetFacingDirection(new Vector2(float.PositiveInfinity,0)));
        }
    }
}
