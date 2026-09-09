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
    }
}
