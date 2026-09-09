using System;

namespace Computerzhuxi.Health
{
    public enum MaximumHealthPolicy { PreserveCurrent, Refill }
    public enum HealthChangeReason { Damage, Heal, Kill, Revive, Maximum, Restore }

    /// <summary>描述不可变的生命状态，生死始终由当前生命推导。</summary>
    public readonly struct HealthSnapshot
    {
        public int Current { get; }
        public int Maximum { get; }
        public bool IsAlive => Current > 0;
        public bool IsDead => !IsAlive;
        public float Normalized => (float)Current / Maximum;
        /// <summary>创建只由生命核心提供的有效状态快照。</summary>
        internal HealthSnapshot(int current, int maximum) { Current = current; Maximum = maximum; }
    }

    /// <summary>保存一次命令的不可变结果，零变化结果不会触发事件。</summary>
    public readonly struct HealthChange
    {
        public HealthChangeReason Reason { get; }
        public HealthSnapshot Before { get; }
        public HealthSnapshot After { get; }
        public bool HasChanged => Before.Current != After.Current || Before.Maximum != After.Maximum;
        public int Delta => After.Current - Before.Current;
        public int ActualAmount => Math.Abs(Delta);
        /// <summary>建立命令原因与前后状态的对应关系。</summary>
        internal HealthChange(HealthChangeReason reason, HealthSnapshot before, HealthSnapshot after)
        { Reason = reason; Before = before; After = after; }
    }

    /// <summary>仅向观察者提供生命事实和事件，不暴露写入命令。</summary>
    public interface IReadOnlyHealth
    {
        int Current { get; }
        int Maximum { get; }
        bool IsAlive { get; }
        bool IsDead { get; }
        float Normalized { get; }
        HealthSnapshot Snapshot { get; }
        event Action<HealthChange> Changed;
        event Action<HealthChange> Damaged;
        event Action<HealthChange> Healed;
        event Action<HealthChange> Died;
        event Action<HealthChange> Revived;
    }

    /// <summary>独立拥有生命状态并按确定顺序发布变化；仅供单线程同步调用。</summary>
    public sealed class Health : IReadOnlyHealth
    {
        private int current;
        private int maximum;
        private bool publishing;
        public int Current => current;
        public int Maximum => maximum;
        public bool IsAlive => current > 0;
        public bool IsDead => !IsAlive;
        public float Normalized => (float)current / maximum;
        public HealthSnapshot Snapshot => new HealthSnapshot(current, maximum);
        public event Action<HealthChange> Changed;
        public event Action<HealthChange> Damaged;
        public event Action<HealthChange> Healed;
        public event Action<HealthChange> Died;
        public event Action<HealthChange> Revived;

        /// <summary>使用合法初值建立独立实例，不发布初始化事件。</summary>
        public Health(int current, int maximum)
        { Validate(current, maximum); this.current = current; this.maximum = maximum; }

        /// <summary>扣除存活目标的生命；非正输入和死亡目标均不变化。</summary>
        public HealthChange Damage(int amount)
        { Guard(); return Commit(amount > 0 && IsAlive ? Math.Max(0, current - amount) : current, maximum, HealthChangeReason.Damage); }

        /// <summary>恢复存活目标生命，使用宽整数避免过量治疗溢出。</summary>
        public HealthChange Heal(int amount)
        { Guard(); return Commit(amount > 0 && IsAlive ? (int)Math.Min(maximum, (long)current + amount) : current, maximum, HealthChangeReason.Heal); }

        /// <summary>直接令目标死亡，不将强制死亡解释为受到伤害。</summary>
        public HealthChange Kill() { Guard(); return Commit(0, maximum, HealthChangeReason.Kill); }

        /// <summary>以指定正生命复活死亡目标，存活目标不会被重新设置。</summary>
        public HealthChange Revive(int value)
        {
            Guard();
            if (value < 1 || value > maximum) throw new ArgumentOutOfRangeException(nameof(value));
            return Commit(IsDead ? value : current, maximum, HealthChangeReason.Revive);
        }

        /// <summary>原子修改上限并保持当前值或回满，回满允许复活。</summary>
        public HealthChange ChangeMaximum(int value, MaximumHealthPolicy policy)
        {
            Guard();
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
            if (policy != MaximumHealthPolicy.PreserveCurrent && policy != MaximumHealthPolicy.Refill)
                throw new ArgumentOutOfRangeException(nameof(policy));
            return Commit(policy == MaximumHealthPolicy.Refill ? value : Math.Min(current, value), value, HealthChangeReason.Maximum);
        }

        /// <summary>原子恢复完整状态，仅发布 Changed，避免读档重复触发玩法结算。</summary>
        public HealthChange Restore(int value, int maximum)
        { Guard(); Validate(value, maximum); return Commit(value, maximum, HealthChangeReason.Restore); }

        /// <summary>拒绝事件内同步重入，保证一条命令的全部通知不会被嵌套写入打断。</summary>
        private void Guard()
        { if (publishing) throw new InvalidOperationException("生命事件回调内不允许同步提交生命命令。"); }

        /// <summary>拒绝非法初始或恢复状态，不静默修正调用方错误。</summary>
        private static void Validate(int value, int maximum)
        {
            if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
            if (value < 0 || value > maximum) throw new ArgumentOutOfRangeException(nameof(value));
        }

        /// <summary>先提交完整状态再发布通知，即使订阅者抛错也恢复重入保护。</summary>
        private HealthChange Commit(int value, int max, HealthChangeReason reason)
        {
            var result = new HealthChange(reason, Snapshot, new HealthSnapshot(value, max));
            if (!result.HasChanged) return result;
            current = value;
            maximum = max;
            publishing = true;
            try
            {
                if (reason == HealthChangeReason.Damage) Damaged?.Invoke(result);
                if (reason == HealthChangeReason.Heal) Healed?.Invoke(result);
                Changed?.Invoke(result);
                // 状态恢复只同步事实，不重放死亡奖励或复活表现。
                if (reason != HealthChangeReason.Restore)
                {
                    if (result.Before.IsAlive && result.After.IsDead) Died?.Invoke(result);
                    if (result.Before.IsDead && result.After.IsAlive) Revived?.Invoke(result);
                }
            }
            finally { publishing = false; }
            return result;
        }
    }
}
