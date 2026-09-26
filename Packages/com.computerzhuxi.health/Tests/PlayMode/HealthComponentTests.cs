using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Computerzhuxi.Health.Tests
{
    public sealed class HealthComponentTests
    {
        /// <summary>验证真实生命周期、UnityEvent 与禁用重启时的状态保持。</summary>
        [UnityTest] public IEnumerator LifecycleAndEvents()
        {
            var host=new GameObject("Health lifecycle");
            try
            {
                var h=host.AddComponent<HealthComponent>(); int damaged=0,changed=0,died=0,revived=0;
                h.OnDamaged.AddListener(()=>damaged++); h.OnChanged.AddListener(()=>changed++);
                h.OnDied.AddListener(()=>died++); h.OnRevived.AddListener(()=>revived++);
                Assert.That(h.IsInitialized,Is.True); h.Damage(3); host.SetActive(false); yield return null;
                host.SetActive(true); yield return null; Assert.That(h.State.Current,Is.EqualTo(7));
                h.Kill(); h.Revive(5); Assert.That(damaged,Is.EqualTo(1)); Assert.That(changed,Is.EqualTo(3)); Assert.That(died,Is.EqualTo(1)); Assert.That(revived,Is.EqualTo(1));
            }
            finally { Object.Destroy(host); }
        }

        /// <summary>验证组件销毁后，外部保留的核心状态不会再转发 UnityEvent。</summary>
        [UnityTest] public IEnumerator DestroyDetachesForwardedEvents()
        {
            var host = new GameObject("Health retained state");
            var component = host.AddComponent<HealthComponent>();
            // State 是公开只读入口；测试只在此处转换为实际核心类型以模拟外部继续持有它。
            var retainedCore = component.State as Health;
            Assert.That(retainedCore, Is.Not.Null);
            int forwarded = 0;
            component.OnChanged.AddListener(() => forwarded++);
            component.OnDamaged.AddListener(() => forwarded++);
            component.OnHealed.AddListener(() => forwarded++);
            component.OnDied.AddListener(() => forwarded++);
            component.OnRevived.AddListener(() => forwarded++);
            Object.Destroy(host);
            yield return null;
            retainedCore.Damage(1);
            retainedCore.Heal(1);
            retainedCore.Kill();
            retainedCore.Revive(5);
            Assert.That(retainedCore.Current, Is.EqualTo(5));
            Assert.That(forwarded, Is.Zero);
        }
    }
}
