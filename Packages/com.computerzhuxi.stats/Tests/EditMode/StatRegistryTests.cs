using System;
using System.Collections.Generic;
using Computerzhuxi.Stats;
using NUnit.Framework;

public sealed class StatRegistryTests
{
    private sealed class Binding : IStatBinding
    {
        internal double Base;
        internal double Final;
        internal bool FailApply;
        /// <summary>读取测试域基础值。</summary>
        public double GetBase() => Base;
        /// <summary>写入测试域基础值。</summary>
        public void SetBase(double value) { Base = value; }
        /// <summary>读取测试域最终值。</summary>
        public double GetFinal() => Final;
        /// <summary>模拟最终值提交及失败。</summary>
        public void ApplyFinal(double value)
        { if (FailApply) throw new InvalidOperationException("apply failed"); Final = value; }
    }

    private sealed class BonusStrategy : IStatCalculationStrategy
    {
        /// <summary>只支持示例额外类别。</summary>
        public bool SupportsCategory(string category) => category == "Bonus";
        /// <summary>按自定义示例规则计算。</summary>
        public double Calculate(double baseValue, IReadOnlyList<StatModifier> modifiers)
        {
            foreach (StatModifier item in modifiers) baseValue += item.Value * 2;
            return baseValue;
        }
    }

    /// <summary>验证固定和先相加，倍率随后逐项相乘。</summary>
    [Test]
    public void DefaultFormula_UsesFlatSumAndMultiplierProduct()
    {
        var registry = new StatRegistry();
        var binding = new Binding { Base = 100 };
        using (registry.Register(new StatDefinition("attack"), binding))
        {
            registry.AddModifier("attack", new StatModifier(StatModifierCategory.Flat, 20, "sword"));
            registry.AddModifier("attack", new StatModifier(StatModifierCategory.Multiplier, 1.1, "buff-a"));
            registry.AddModifier("attack", new StatModifier(StatModifierCategory.Multiplier, 1.2, "buff-b"));
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(158.4).Within(0.0000001));
            Assert.That(binding.Final, Is.EqualTo(registry.GetFinal("attack")));
        }
    }

    /// <summary>验证无修正单位元、负值和边界约束。</summary>
    [Test]
    public void EmptyModifiersAndNegativeValues_FollowExplicitBounds()
    {
        var registry = new StatRegistry();
        using (registry.Register(new StatDefinition("speed", minimum: 0, maximum: 12, rounding: StatRounding.Floor),
                   new Binding { Base = -3 }))
        {
            Assert.That(registry.GetFinal("speed"), Is.Zero);
            registry.SetBase("speed", 12.9);
            Assert.That(registry.GetFinal("speed"), Is.EqualTo(12));
            registry.AddModifier("speed", new StatModifier(StatModifierCategory.Multiplier, -2, "curse"));
            Assert.That(registry.GetFinal("speed"), Is.Zero);
        }
    }

    /// <summary>验证非有限输入和未声明类别被拒绝。</summary>
    [Test]
    public void NonFiniteInputAndUnsupportedCategory_AreRejected()
    {
        var registry = new StatRegistry();
        using (registry.Register(new StatDefinition("attack"), new Binding { Base = 1 }))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.SetBase("attack", double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StatModifier("Flat", double.PositiveInfinity, "item"));
            Assert.Throws<ArgumentException>(() => registry.AddModifier("attack", new StatModifier("Unknown", 1, "item")));
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(1));
        }
    }

    /// <summary>验证句柄精确性、来源批量移除和旧注册释放隔离。</summary>
    [Test]
    public void HandlesSourcesAndRegistrations_AreExactAndIsolated()
    {
        var first = new StatRegistry();
        var second = new StatRegistry();
        var old = first.Register(new StatDefinition("attack"), new Binding { Base = 10 });
        var other = second.Register(new StatDefinition("attack"), new Binding { Base = 20 });
        Assert.Throws<InvalidOperationException>(() => first.Register(new StatDefinition("attack"), new Binding()));
        var handle = first.AddModifier("attack", new StatModifier("Flat", 2, "sword"));
        first.AddModifier("attack", new StatModifier("Multiplier", 2, "sword"));
        Assert.That(second.RemoveModifier(handle), Is.False);
        Assert.That(first.RemoveModifier(handle), Is.True);
        Assert.That(first.RemoveModifier(handle), Is.False);
        Assert.That(first.RemoveSource("sword"), Is.EqualTo(1));
        Assert.That(first.GetFinal("attack"), Is.EqualTo(10));
        old.Dispose(); old.Dispose();
        using (first.Register(new StatDefinition("attack"), new Binding { Base = 7 }))
        { old.Dispose(); Assert.That(first.GetFinal("attack"), Is.EqualTo(7)); }
        other.Dispose();
    }

    /// <summary>验证嵌套批处理的已提交值读取与限幅后的变化通知。</summary>
    [Test]
    public void BatchKeepsCommittedFinalUntilOuterScopeEndsAndOnlyReportsRealChanges()
    {
        var registry = new StatRegistry();
        var binding = new Binding { Base = 10 };
        using (registry.Register(new StatDefinition("attack", maximum: 10), binding))
        {
            var changes = new List<StatChange>();
            registry.Changed += changes.Add;
            using (registry.BeginBatch())
            {
                registry.SetBase("attack", 20);
                using (registry.BeginBatch())
                    registry.AddModifier("attack", new StatModifier("Flat", 5, "item"));
                Assert.That(registry.GetBase("attack"), Is.EqualTo(20));
                Assert.That(registry.GetFinal("attack"), Is.EqualTo(10));
                Assert.That(binding.Final, Is.EqualTo(10));
            }
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(10));
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].PreviousBase, Is.EqualTo(10));
            Assert.That(changes[0].CurrentBase, Is.EqualTo(20));
            Assert.That(changes[0].PreviousFinal, Is.EqualTo(10));
            Assert.That(changes[0].CurrentFinal, Is.EqualTo(10));
        }
    }

    /// <summary>验证外部基础值失效、自定义类别和快照。</summary>
    [Test]
    public void ExternalBaseInvalidationAndCustomStrategy_Work()
    {
        var registry = new StatRegistry();
        var binding = new Binding { Base = 4 };
        using (registry.Register(new StatDefinition("custom", new BonusStrategy()), binding))
        {
            binding.Base = 5;
            registry.Invalidate("custom");
            registry.AddModifier("custom", new StatModifier("Bonus", 3, "item"));
            Assert.That(registry.GetFinal("custom"), Is.EqualTo(11));
            Assert.That(registry.CaptureBaseSnapshot()["custom"], Is.EqualTo(5));
        }
    }

    /// <summary>验证回调写入在下一轮提交。</summary>
    [Test]
    public void ReentrantWritesAreQueuedAfterCurrentNotification()
    {
        var registry = new StatRegistry();
        using (registry.Register(new StatDefinition("attack"), new Binding { Base = 1 }))
        {
            var observed = new List<double>();
            registry.Changed += change =>
            {
                observed.Add(change.CurrentFinal);
                if (change.CurrentFinal == 2) registry.SetBase("attack", 3);
            };
            registry.SetBase("attack", 2);
            Assert.That(observed, Is.EqualTo(new[] { 2d, 3d }));
        }
    }

    /// <summary>验证绑定失败时不发布未提交结果，并能重试。</summary>
    [Test]
    public void FailedApplyDoesNotReportUncommittedFinal()
    {
        var registry = new StatRegistry();
        var binding = new Binding { Base = 1 };
        using (registry.Register(new StatDefinition("attack"), binding))
        {
            binding.FailApply = true;
            Assert.Throws<AggregateException>(() => registry.SetBase("attack", 2));
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(1));
            binding.FailApply = false;
            registry.Invalidate("attack");
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(2));
        }
    }

    /// <summary>验证绑定回读是最终值权威来源。</summary>
    [Test]
    public void ApplyFinal_ReadsBackDomainValue()
    {
        var registry = new StatRegistry();
        var binding = new RoundedBinding { Base = 1.6 };
        using (registry.Register(new StatDefinition("attack"), binding))
        {
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(2));
            registry.SetBase("attack", 2.6);
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(3));
        }
    }

    private sealed class RoundedBinding : IStatBinding
    {
        internal double Base;
        internal double Final;
        /// <summary>读取测试基础值。</summary>
        public double GetBase() => Base;
        /// <summary>写入测试基础值。</summary>
        public void SetBase(double value) { Base = value; }
        /// <summary>读取整数化的最终值。</summary>
        public double GetFinal() => Final;
        /// <summary>模拟业务域自己的整数化规则。</summary>
        public void ApplyFinal(double value) { Final = Math.Round(value); }
    }

    /// <summary>验证批次中失败项保留，同时成功项及所有观察者收到通知。</summary>
    [Test]
    public void FailedBindingAndObserver_KeepDirtyAndReportSuccessfulChanges()
    {
        var registry = new StatRegistry();
        var first = new Binding { Base = 1 };
        var second = new Binding { Base = 1 };
        using (registry.Register(new StatDefinition("a"), first))
        using (registry.Register(new StatDefinition("b"), second))
        {
            int observed = 0;
            Action<StatChange> failingObserver = _ => throw new InvalidOperationException("observer failed");
            registry.Changed += failingObserver;
            registry.Changed += _ => observed++;
            first.FailApply = true;
            AggregateException error = Assert.Throws<AggregateException>(() =>
            {
                using (registry.BeginBatch())
                {
                    registry.SetBase("a", 2);
                    registry.SetBase("b", 3);
                }
            });
            Assert.That(error.InnerExceptions, Has.Count.EqualTo(2));
            Assert.That(registry.GetFinal("a"), Is.EqualTo(1));
            Assert.That(registry.GetFinal("b"), Is.EqualTo(3));
            Assert.That(observed, Is.EqualTo(1));
            first.FailApply = false;
            registry.Changed -= failingObserver;
            registry.Invalidate("a");
            Assert.That(registry.GetFinal("a"), Is.EqualTo(2));
        }
    }

    /// <summary>验证更新修正、属性枚举顺序及整数边界验证。</summary>
    [Test]
    public void UpdateAndEnumeration_AreStable()
    {
        var registry = new StatRegistry();
        using (registry.Register(new StatDefinition("z"), new Binding { Base = 1 }))
        using (registry.Register(new StatDefinition("a"), new Binding { Base = 1 }))
        {
            var handle = registry.AddModifier("z", new StatModifier("Flat", 2, "item"));
            Assert.That(registry.UpdateModifier(handle, new StatModifier("Flat", 4, "item")), Is.True);
            Assert.That(registry.GetFinal("z"), Is.EqualTo(5));
            Assert.That(registry.StatIds, Is.EqualTo(new[] { "a", "z" }));
            Assert.That(new List<string>(registry.CaptureBaseSnapshot().Keys), Is.EqualTo(new[] { "a", "z" }));
            Assert.That(registry.RemoveSource("item"), Is.EqualTo(1));
        }
        Assert.Throws<ArgumentException>(() => new StatDefinition("x", minimum: 0.5, rounding: StatRounding.Floor));
    }

    /// <summary>验证同步添加失败时不会遗留无法取得句柄的修正。</summary>
    [Test]
    public void FailedAddModifier_RollsBackModifierAndCanRecalculate()
    {
        var registry = new StatRegistry();
        var binding = new Binding { Base = 4 };
        using (registry.Register(new StatDefinition("attack"), binding))
        {
            binding.FailApply = true;
            Assert.Throws<AggregateException>(() => registry.AddModifier("attack", new StatModifier("Flat", 7, "item")));
            binding.FailApply = false;
            registry.Invalidate("attack");
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(4));
            Assert.That(registry.RemoveSource("item"), Is.Zero);
        }
    }

    /// <summary>验证循环回调在第六十五轮前被阻断并保留失效项。</summary>
    [Test]
    public void ReentrantLoop_IsBoundedTo64Rounds()
    {
        var registry = new StatRegistry();
        using (registry.Register(new StatDefinition("attack"), new Binding { Base = 1 }))
        {
            registry.Changed += change => registry.SetBase("attack", change.CurrentBase + 1);
            Assert.Throws<InvalidOperationException>(() => registry.SetBase("attack", 2));
            Assert.That(registry.GetFinal("attack"), Is.EqualTo(65));
            Assert.That(registry.GetBase("attack"), Is.EqualTo(66));
        }
    }
}
