using System;
using System.Collections;
using System.Collections.Generic;
using Computerzhuxi.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class StatCollectionLifecycleTests
{
    private sealed class Binding : IStatBinding
    {
        internal double Base = 10;
        internal double Final = 10;
        /// <summary>读取测试域基础值。</summary>
        public double GetBase() => Base;
        /// <summary>写入测试域基础值。</summary>
        public void SetBase(double value) { Base = value; }
        /// <summary>读取测试域最终值。</summary>
        public double GetFinal() => Final;
        /// <summary>提交测试域最终值。</summary>
        public void ApplyFinal(double value) { Final = value; }
    }

    private sealed class Provider : IStatBindingProvider
    {
        internal readonly Binding Binding = new Binding();
        /// <summary>为指定测试属性提供独立绑定。</summary>
        public bool TryGetBinding(string statId, out IStatBinding binding)
        { binding = statId == "attack" ? Binding : null; return binding != null; }
    }

    /// <summary>验证真实启停和销毁生命周期不重置存活组件的状态。</summary>
    [UnityTest]
    public IEnumerator DisableEnablePreservesState_DestroyReleasesComponent()
    {
        var objectA = new GameObject("stats-a");
        var objectB = new GameObject("stats-b");
        try
        {
            var a = objectA.AddComponent<StatCollectionComponent>();
            var b = objectB.AddComponent<StatCollectionComponent>();
            a.Initialize(new Provider(), new[] { new StatDefinition("attack") });
            b.Initialize(new Provider(), new[] { new StatDefinition("attack") });
            a.Initialize(new Provider(), new[] { new StatDefinition("attack") });
            a.AddModifier("attack", new StatModifier("Flat", 5, "weapon"));
            Assert.That(a.Stats.GetFinal("attack"), Is.EqualTo(15));
            Assert.That(b.Stats.GetFinal("attack"), Is.EqualTo(10));
            IReadOnlyStatRegistry oldView = a.Stats;
            a.enabled = false;
            yield return null;
            a.enabled = true;
            yield return null;
            Assert.That(a.Stats.GetFinal("attack"), Is.EqualTo(15));
            UnityEngine.Object.Destroy(objectA);
            yield return null;
            Assert.That(a == null, Is.True);
            Assert.That(oldView.StatIds, Is.Empty);
            Assert.That(oldView.TryGetFinal("attack", out _), Is.False);
            Assert.That(b.Stats.GetFinal("attack"), Is.EqualTo(10));
        }
        finally
        {
            if (objectA != null) UnityEngine.Object.Destroy(objectA);
            if (objectB != null) UnityEngine.Object.Destroy(objectB);
        }
    }
}
