using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace Computerzhuxi.Perception2D.Tests
{
    /// <summary>通过真实 Unity 帧验证暂停、禁用和重新启用。</summary>
    public sealed class PerceptionLifecycleTests
    {
        /// <summary>实际启用过的 World 销毁时失效句柄，目标随后停用和重绑仍可安全完成。</summary>
        [UnityTest] public IEnumerator DestroyedWorld_InvalidatesTargetsAndAllowsRebinding()
        {
            var worldObject = new GameObject("OwnedWorldLifecycle");
            var targetObject = new GameObject("OwnedTargetLifecycle");
            GameObject replacementObject = null;
            try
            {
                var world = worldObject.AddComponent<PerceptionWorld2D>();
                var target = targetObject.AddComponent<PerceptionTarget2D>();
                target.Bind(world);
                var core = new PerceptionCore2D(world.Registry);
                PerceptionTargetHandle oldHandle = target.Handle;
                core.ReportHearing(Vector2.right, Vector2.zero, 1, source: oldHandle);
                core.Advance(1);
                Assert.That(core.Observations.Count, Is.EqualTo(1));

                Object.Destroy(worldObject);
                yield return null;
                Assert.That(core.Registry.IsValid(oldHandle), Is.False);
                Assert.Throws<System.ObjectDisposedException>(() => core.Registry.Register());
                Assert.That(target.IsRegistered, Is.False);
                core.Advance(2);
                Assert.That(core.Observations, Is.Empty);

                target.enabled = false;
                Assert.That(target.Handle.Id, Is.Zero);
                replacementObject = new GameObject("ReplacementWorldLifecycle");
                var replacement = replacementObject.AddComponent<PerceptionWorld2D>();
                target.Bind(replacement);
                target.enabled = true;
                Assert.That(target.IsRegistered, Is.True);
                Assert.That(target.Handle.Equals(oldHandle), Is.False);
            }
            finally
            {
                if (worldObject != null) Object.Destroy(worldObject);
                if (targetObject != null) Object.Destroy(targetObject);
                if (replacementObject != null) Object.Destroy(replacementObject);
            }
        }
        /// <summary>大扫描间隔下启停观察者立即重新采样，旧目标只按发现距离判断。</summary>
        [UnityTest] public IEnumerator RestartWithLongInterval_UsesDiscoveryDistance()
        {
            var root = new GameObject("OwnedLongIntervalRestart");
            try
            {
                var world = root.AddComponent<PerceptionWorld2D>();
                var eye = new GameObject("Observer");
                eye.transform.SetParent(root.transform);
                eye.SetActive(false);
                var observer = eye.AddComponent<PerceptionObserver2D>();
                observer.Configure(world, new PerceptionSettings2D { ScanInterval = 10 });
                eye.SetActive(true);
                var source = new GameObject("Target");
                source.transform.SetParent(root.transform);
                source.transform.position = Vector2.right * 2;
                source.AddComponent<CircleCollider2D>();
                var target = source.AddComponent<PerceptionTarget2D>();
                target.Bind(world);
                yield return null;
                Assert.That(observer.TryGetObservation(target, out TargetPerceptionInfo info) && info.IsVisible, Is.True);
                observer.enabled = false;
                source.transform.position = Vector2.right * 7;
                Physics2D.SyncTransforms();
                observer.enabled = true;
                yield return null;
                Assert.That(observer.TryGetObservation(target, out info) && info.IsVisible, Is.False);
            }
            finally { Object.Destroy(root); }
        }
        /// <summary>候选和遮挡都来自指定二维物理场景，移动观察者场景结束旧视觉来源。</summary>
        [UnityTest] public IEnumerator LocalPhysicsScenes_IsolateCandidatesAndOcclusion()
        {
            Scene first = SceneManager.CreateScene("PerceptionLocalFirst", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            Scene second = SceneManager.CreateScene("PerceptionLocalSecond", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            try
            {
                var worldObject = new GameObject("World");
                SceneManager.MoveGameObjectToScene(worldObject, first);
                var world = worldObject.AddComponent<PerceptionWorld2D>();
                var eye = new GameObject("Observer");
                eye.SetActive(false);
                SceneManager.MoveGameObjectToScene(eye, first);
                var observer = eye.AddComponent<PerceptionObserver2D>();
                observer.Configure(world, new PerceptionSettings2D { ViewAngle = 360, ObstacleLayers = 1 << 8 });
                eye.SetActive(true);
                var firstObject = new GameObject("FirstTarget");
                SceneManager.MoveGameObjectToScene(firstObject, first);
                firstObject.transform.position = Vector2.right * 2;
                firstObject.AddComponent<CircleCollider2D>();
                var firstTarget = firstObject.AddComponent<PerceptionTarget2D>();
                firstTarget.Bind(world);
                var secondObject = new GameObject("SecondTarget");
                SceneManager.MoveGameObjectToScene(secondObject, second);
                secondObject.transform.position = Vector2.right * 3;
                secondObject.AddComponent<CircleCollider2D>();
                var secondTarget = secondObject.AddComponent<PerceptionTarget2D>();
                secondTarget.Bind(world);
                var wall = new GameObject("OtherSceneWall");
                wall.layer = 8;
                wall.transform.position = Vector2.right;
                SceneManager.MoveGameObjectToScene(wall, second);
                wall.AddComponent<BoxCollider2D>();

                yield return null;
                Assert.That(observer.TryGetObservation(firstTarget, out TargetPerceptionInfo visible) && visible.IsVisible, Is.True);
                Assert.That(observer.TryGetObservation(secondTarget, out _), Is.False);
                wall.SetActive(false);
                observer.SetPhysicsSceneOverride(second.GetPhysicsScene2D());
                yield return null;
                Assert.That(observer.TryGetObservation(secondTarget, out visible) && visible.IsVisible, Is.True);
                Assert.That(observer.TryGetObservation(firstTarget, out visible) && visible.IsVisible, Is.False);
                observer.SetPhysicsSceneOverride(null);
                yield return null;
                Assert.That(observer.TryGetObservation(firstTarget, out visible) && visible.IsVisible, Is.True);
                wall.SetActive(true);
                SceneManager.MoveGameObjectToScene(wall, first);
                yield return new WaitForSeconds(0.15f);
                Assert.That(observer.TryGetObservation(firstTarget, out visible) && visible.IsVisible, Is.False);

                SceneManager.MoveGameObjectToScene(eye, second);
                yield return null;
                Assert.That(observer.TryGetObservation(secondTarget, out visible) && visible.IsVisible, Is.True);
                Assert.That(observer.TryGetObservation(firstTarget, out visible) && visible.IsVisible, Is.False);
            }
            finally
            {
                SceneManager.UnloadSceneAsync(first);
                SceneManager.UnloadSceneAsync(second);
            }
        }
        /// <summary>恢复首批清理超龄视觉，停用前的丢失距离不能当作新发现距离。</summary>
        [UnityTest] public IEnumerator Resume_ExpiresOldSightBeforeRediscovery()
        {
            var root=new GameObject("OwnedResumeTest");
            try
            {
                var world=root.AddComponent<PerceptionWorld2D>();
                var eye=new GameObject("Observer"); eye.transform.SetParent(root.transform); eye.SetActive(false);
                var observer=eye.AddComponent<PerceptionObserver2D>(); observer.Configure(world,new PerceptionSettings2D { SightMemory=0.05f }); eye.SetActive(true);
                var source=new GameObject("Target"); source.transform.SetParent(root.transform); source.transform.position=Vector2.right*2;
                source.AddComponent<CircleCollider2D>(); var target=source.AddComponent<PerceptionTarget2D>(); target.Bind(world); Physics2D.SyncTransforms();
                yield return null; Assert.That(observer.TryGetObservation(target,out var info)&&info.IsVisible,Is.True);
                observer.enabled=false; source.transform.position=Vector2.right*7; Physics2D.SyncTransforms();
                yield return new WaitForSeconds(0.1f); observer.enabled=true; yield return null;
                Assert.That(observer.TryGetObservation(target,out _),Is.False);
            }
            finally { Object.Destroy(root); }
        }
        /// <summary>真实启停目标创建新代次，旧声音不能复活旧记录。</summary>
        [UnityTest] public IEnumerator PooledTarget_StartsANewGeneration()
        {
            var root = new GameObject("OwnedPoolTest");
            try
            {
                var world = root.AddComponent<PerceptionWorld2D>();
                var eye = new GameObject("Observer"); eye.transform.SetParent(root.transform); eye.SetActive(false);
                var observer = eye.AddComponent<PerceptionObserver2D>(); observer.Configure(world,new PerceptionSettings2D { SightEnabled = false }); eye.SetActive(true);
                var source = new GameObject("Source"); source.transform.SetParent(root.transform); var target=source.AddComponent<PerceptionTarget2D>(); target.Bind(world);
                ulong generation=target.Generation; world.ReportNoise(new NoiseEvent2D(Vector2.zero,source:target)); source.SetActive(false); source.SetActive(true);
                yield return null; Assert.That(target.Generation,Is.GreaterThan(generation)); Assert.That(observer.Observations,Is.Empty);
                world.ReportNoise(new NoiseEvent2D(Vector2.zero,source:target)); yield return null; Assert.That(observer.Observations.Count,Is.EqualTo(1));
                source.SetActive(false); yield return null; Assert.That(observer.Observations,Is.Empty);
            }
            finally { Object.Destroy(root); }
        }
        /// <summary>暂停不老化；禁用后重新启用按经过的游戏时间过期。</summary>
        [UnityTest] public IEnumerator PauseDisableAndResume()
        {
            var root = new GameObject("OwnedTestRoot"); float oldScale = Time.timeScale;
            try
            {
                var world = root.AddComponent<PerceptionWorld2D>();
                var child = new GameObject("Observer"); child.transform.SetParent(root.transform); child.SetActive(false);
                var observer = child.AddComponent<PerceptionObserver2D>(); observer.Configure(world, new PerceptionSettings2D { SightEnabled = false, AnonymousMemory = 0.05f }); child.SetActive(true);
                world.ReportNoise(new NoiseEvent2D(Vector2.zero)); yield return null; Assert.That(observer.HeardEvents.Count, Is.EqualTo(1));
                Time.timeScale = 0; yield return new WaitForSecondsRealtime(0.1f); Assert.That(observer.HeardEvents.Count, Is.EqualTo(1));
                Time.timeScale = 1; observer.enabled = false; yield return new WaitForSeconds(0.1f); observer.enabled = true; yield return null; Assert.That(observer.HeardEvents, Is.Empty);
            }
            finally { Time.timeScale = oldScale; Object.Destroy(root); }
        }
    }
}
