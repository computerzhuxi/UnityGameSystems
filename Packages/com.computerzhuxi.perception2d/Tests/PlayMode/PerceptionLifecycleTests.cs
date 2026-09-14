using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Computerzhuxi.Perception2D.Tests
{
    /// <summary>通过真实 Unity 帧验证暂停、禁用和重新启用。</summary>
    public sealed class PerceptionLifecycleTests
    {
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
