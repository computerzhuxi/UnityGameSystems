using System;
using System.Collections;
using Computerzhuxi.Stats;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.TestTools;

public sealed class StatsLabIntegrationTests
{
#if UNITY_EDITOR
    private sealed class Binding : IStatBinding
    {
        internal double Base = 30;
        internal double Final = 30;
        /// <summary>读取程序式对照角色基础值。</summary>
        public double GetBase() => Base;
        /// <summary>写入程序式对照角色基础值。</summary>
        public void SetBase(double value) { Base = value; }
        /// <summary>读取程序式对照角色最终值。</summary>
        public double GetFinal() => Final;
        /// <summary>提交程序式对照角色最终值。</summary>
        public void ApplyFinal(double value) { Final = value; }
    }

    /// <summary>实例化真实 Lab Prefab 并验证资产装配与程序式入口共享计算结果。</summary>
    [UnityTest]
    public IEnumerator PrefabStartsWithDefinitionAssets_AndMatchesProgrammaticRegistry()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/BasicStatsDemo/ComponentRole.prefab");
        Assert.That(prefab, Is.Not.Null);
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        try
        {
            // 通过真实 Start 读取定义资产与组件绑定，而不是测试直接配置入口。
            yield return null;
            var component = instance.GetComponent<StatCollectionComponent>();
            var domain = instance.GetComponent<SampleCharacterStats>();
            Assert.That(component.IsInitialized, Is.True);
            Assert.That(component.Stats.GetFinal("maxHealth"), Is.EqualTo(100));
            Assert.That(component.Stats.GetFinal("attack"), Is.EqualTo(30));
            Assert.That(component.Stats.GetFinal("moveSpeed"), Is.EqualTo(5));
            Assert.That(domain.CurrentHealth, Is.EqualTo(100));

            var code = new StatRegistry();
            var binding = new Binding();
            using (code.Register(new StatDefinition("attack"), binding))
            {
                using (component.BeginBatch())
                using (code.BeginBatch())
                {
                    component.AddModifier("attack", new StatModifier("Flat", 20, "sword"));
                    component.AddModifier("attack", new StatModifier("Multiplier", 1.2, "sword"));
                    code.AddModifier("attack", new StatModifier("Flat", 20, "sword"));
                    code.AddModifier("attack", new StatModifier("Multiplier", 1.2, "sword"));
                }
                Assert.That(component.Stats.GetFinal("attack"), Is.EqualTo(60));
                Assert.That(component.Stats.GetFinal("attack"), Is.EqualTo(code.GetFinal("attack")));
                Assert.That(component.RemoveSource("sword"), Is.EqualTo(2));
                Assert.That(code.RemoveSource("sword"), Is.EqualTo(2));
                Assert.That(component.Stats.GetFinal("attack"), Is.EqualTo(30));
            }
        }
        finally
        {
            UnityEngine.Object.Destroy(instance);
        }
        yield return null;
    }
#endif
}
