using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Computerzhuxi.Health.Tests
{
    public sealed class HealthCoreTests
    {
        /// <summary>验证合法初值与生死推导。</summary>
        [TestCase(0,1)] [TestCase(5,10)] [TestCase(10,10)]
        public void ValidInitial(int current, int max)
        { var h = new Health(current,max); Assert.That(h.Current,Is.EqualTo(current)); Assert.That(h.Maximum,Is.EqualTo(max)); Assert.That(h.IsAlive,Is.EqualTo(current>0)); }
        /// <summary>验证非法状态不会创建实例。</summary>
        [TestCase(-1,10)] [TestCase(11,10)] [TestCase(0,0)] [TestCase(0,-1)]
        public void InvalidInitial(int current,int max) { Assert.Throws<ArgumentOutOfRangeException>(()=>new Health(current,max)); }
        /// <summary>验证伤害截断与输入约束。</summary>
        [TestCase(-2,10)] [TestCase(0,10)] [TestCase(3,7)] [TestCase(20,0)] [TestCase(int.MaxValue,0)]
        public void DamageBounds(int amount,int expected)
        { var h=new Health(10,10); var r=h.Damage(amount); Assert.That(h.Current,Is.EqualTo(expected)); Assert.That(r.ActualAmount,Is.EqualTo(10-expected)); }
        /// <summary>验证治疗截断与溢出保护。</summary>
        [TestCase(-2,5)] [TestCase(0,5)] [TestCase(3,8)] [TestCase(20,10)] [TestCase(int.MaxValue,10)]
        public void HealBounds(int amount,int expected)
        { var h=new Health(5,10); h.Heal(amount); Assert.That(h.Current,Is.EqualTo(expected)); }
        /// <summary>验证死亡拒绝普通操作以及重复死亡复活。</summary>
        [Test] public void DeathAndRevive()
        { var h=new Health(10,10); Assert.That(h.Kill().HasChanged,Is.True); Assert.That(h.Kill().HasChanged,Is.False); Assert.That(h.Damage(2).HasChanged,Is.False); Assert.That(h.Heal(2).HasChanged,Is.False); Assert.That(h.Revive(4).HasChanged,Is.True); Assert.That(h.Revive(8).HasChanged,Is.False); Assert.That(h.Current,Is.EqualTo(4)); }
        /// <summary>验证最大生命策略的增加、降低、截断与回满。</summary>
        [Test] public void MaximumPolicies()
        { var h=new Health(8,10); h.ChangeMaximum(20,MaximumHealthPolicy.PreserveCurrent); Assert.That(h.Current,Is.EqualTo(8)); h.ChangeMaximum(5,MaximumHealthPolicy.PreserveCurrent); Assert.That(h.Current,Is.EqualTo(5)); h.Kill(); h.ChangeMaximum(12,MaximumHealthPolicy.Refill); Assert.That(h.Current,Is.EqualTo(12)); }
        /// <summary>验证非法命令不改变状态。</summary>
        [Test] public void InvalidCommands()
        { var h=new Health(5,10); Assert.Throws<ArgumentOutOfRangeException>(()=>h.Revive(0)); Assert.Throws<ArgumentOutOfRangeException>(()=>h.Revive(11)); Assert.Throws<ArgumentOutOfRangeException>(()=>h.ChangeMaximum(0,MaximumHealthPolicy.Refill)); Assert.Throws<ArgumentOutOfRangeException>(()=>h.ChangeMaximum(10,(MaximumHealthPolicy)99)); Assert.Throws<ArgumentOutOfRangeException>(()=>h.Restore(20,10)); Assert.That(h.Current,Is.EqualTo(5)); }
        /// <summary>锁定事件参数、次数、顺序以及恢复的独立语义。</summary>
        [Test] public void EventsAndRestore()
        {
            var h=new Health(10,10); var events=new List<string>(); HealthChange captured=default;
            h.Damaged+=r=>{ events.Add("damage"); captured=r; };
            h.Healed+=r=>events.Add("heal"); h.Changed+=r=>events.Add("change");
            h.Died+=r=>events.Add("die"); h.Revived+=r=>events.Add("revive");
            h.Damage(20); CollectionAssert.AreEqual(new[]{"damage","change","die"},events);
            Assert.That(captured.Before.Current,Is.EqualTo(10)); Assert.That(captured.After.Current,Is.Zero); Assert.That(captured.ActualAmount,Is.EqualTo(10));
            events.Clear(); h.Restore(5,10); CollectionAssert.AreEqual(new[]{"change"},events);
            events.Clear(); h.Heal(1); CollectionAssert.AreEqual(new[]{"heal","change"},events);
            events.Clear(); h.Kill(); CollectionAssert.AreEqual(new[]{"change","die"},events);
            events.Clear(); h.Revive(1); CollectionAssert.AreEqual(new[]{"change","revive"},events);
            events.Clear(); h.Restore(0,10); CollectionAssert.AreEqual(new[]{"change"},events);
            events.Clear(); h.ChangeMaximum(10,MaximumHealthPolicy.Refill); CollectionAssert.AreEqual(new[]{"change","revive"},events);
            events.Clear(); h.Heal(1); h.Damage(0); h.Restore(10,10); h.ChangeMaximum(10,MaximumHealthPolicy.Refill); Assert.That(events,Is.Empty);
        }
        /// <summary>验证实例隔离和回调同步重入拒绝。</summary>
        [Test] public void IsolationAndReentry()
        { var a=new Health(10,10); var b=new Health(10,10); a.Changed+=r=>Assert.Throws<InvalidOperationException>(()=>a.Heal(1)); a.Damage(2); Assert.That(a.Current,Is.EqualTo(8)); Assert.That(b.Current,Is.EqualTo(10)); a.Damage(2); Assert.That(a.Current,Is.EqualTo(6)); }
    }
}
