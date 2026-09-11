using System;
using UnityEngine;
using UnityEngine.Events;

namespace Computerzhuxi.Health
{
    /// <summary>通过 Inspector 配置并向 Unity 场景提供独立生命实例。</summary>
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximum = 10;
        [SerializeField] private bool startWithFullHealth = true;
        [SerializeField, Min(0)] private int startingHealth = 10;
        [SerializeField] private UnityEvent onChanged = new UnityEvent();
        [SerializeField] private UnityEvent onDamaged = new UnityEvent();
        [SerializeField] private UnityEvent onHealed = new UnityEvent();
        [SerializeField] private UnityEvent onDied = new UnityEvent();
        [SerializeField] private UnityEvent onRevived = new UnityEvent();
        private Health health;
        public bool IsInitialized => health != null;
        public IReadOnlyHealth State { get { Initialize(); return health; } }
        public UnityEvent OnChanged => onChanged;
        public UnityEvent OnDamaged => onDamaged;
        public UnityEvent OnHealed => onHealed;
        public UnityEvent OnDied => onDied;
        public UnityEvent OnRevived => onRevived;

        /// <summary>组件唤醒时完成唯一一次初始化。</summary>
        private void Awake() { Initialize(); }
        /// <summary>幂等初始化并绑定表现事件；禁用重启不会重新创建状态。</summary>
        public void Initialize()
        {
            if (health != null) return;
            // 满血开关只决定初始状态，不允许隐藏非法的序列化配置。
            if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
            if (startingHealth < 0 || startingHealth > maximum)
                throw new ArgumentOutOfRangeException(nameof(startingHealth));
            health = new Health(startWithFullHealth ? maximum : startingHealth, maximum);
            health.Changed += NotifyChanged;
            health.Damaged += NotifyDamaged;
            health.Healed += NotifyHealed;
            health.Died += NotifyDied;
            health.Revived += NotifyRevived;
        }
        /// <summary>提交最终伤害。</summary>
        public HealthChange Damage(int amount) { Initialize(); return health.Damage(amount); }
        /// <summary>提交治疗。</summary>
        public HealthChange Heal(int amount) { Initialize(); return health.Heal(amount); }
        /// <summary>直接结束生命。</summary>
        public HealthChange Kill() { Initialize(); return health.Kill(); }
        /// <summary>显式复活死亡实例。</summary>
        public HealthChange Revive(int value) { Initialize(); return health.Revive(value); }
        /// <summary>按明确策略修改生命上限。</summary>
        public HealthChange ChangeMaximum(int value, MaximumHealthPolicy policy) { Initialize(); return health.ChangeMaximum(value, policy); }
        /// <summary>恢复状态而不产生玩法反馈。</summary>
        public HealthChange Restore(int current, int maximum) { Initialize(); return health.Restore(current, maximum); }
        /// <summary>转发状态变化给 Inspector 绑定。</summary>
        private void NotifyChanged(HealthChange change) { onChanged?.Invoke(); }
        /// <summary>转发受伤表现事件。</summary>
        private void NotifyDamaged(HealthChange change) { onDamaged?.Invoke(); }
        /// <summary>转发治疗表现事件。</summary>
        private void NotifyHealed(HealthChange change) { onHealed?.Invoke(); }
        /// <summary>转发死亡表现事件。</summary>
        private void NotifyDied(HealthChange change) { onDied?.Invoke(); }
        /// <summary>转发复活表现事件。</summary>
        private void NotifyRevived(HealthChange change) { onRevived?.Invoke(); }
    }
}
