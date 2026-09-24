using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Computerzhuxi.Navigation2D.Samples;
/// <summary>运行真实样例的路径、批量快照、避让和外部运动链。</summary>
public sealed class CrowdLabTests
{
    /// <summary>迎面与停止障碍都必须真实抵达，且不能靠穿过圆形身体通过。</summary>
    [UnityTest] public IEnumerator Demonstration_ReachesBothGoalsWithoutOverlap()
    {
        var host = new GameObject("Crowd sample integration");
        try
        {
            var demo = host.AddComponent<CrowdNavigationDemo>(); yield return null;
            for (int scenario = 0; scenario < 2; scenario++)
            {
                demo.SetScenario(scenario);
                for (int frame = 0; frame < 600 && demo.CompletedCount < (scenario == 0 ? 2 : 1); frame++)
                    yield return new WaitForFixedUpdate();
                Assert.That(demo.CompletedCount, Is.EqualTo(scenario == 0 ? 2 : 1), "scenario " + scenario);
                Assert.That(demo.MinimumGap, Is.GreaterThan(-.005f));
            }
        }
        finally { Object.Destroy(host); }
    }
}
