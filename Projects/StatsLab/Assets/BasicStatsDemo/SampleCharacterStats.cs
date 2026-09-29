using System;
using System.Collections.Generic;
using Computerzhuxi.Stats;
using UnityEngine;

/// <summary>演示由业务域持有基础生命上限、最终生命上限和当前生命。</summary>
public sealed class SampleCharacterStats : MonoBehaviour, IStatBindingProvider
{
    [SerializeField] private double baseMaxHealth = 100;
    [SerializeField] private double baseAttack = 30;
    [SerializeField] private double baseMoveSpeed = 5;

    private double maxHealth;
    private double currentHealth;
    private double attack;
    private double moveSpeed;

    public double CurrentHealth => currentHealth;
    public double MaxHealth => maxHealth;
    public double Attack => attack;
    public double MoveSpeed => moveSpeed;

    /// <summary>从配置初值建立每个示例角色自己的运行时状态。</summary>
    private void Awake()
    {
        maxHealth = baseMaxHealth;
        currentHealth = maxHealth;
        attack = baseAttack;
        moveSpeed = baseMoveSpeed;
    }

    /// <summary>把稳定属性 ID 显式连接到本角色独享的业务字段。</summary>
    public bool TryGetBinding(string statId, out IStatBinding binding)
    {
        switch (statId)
        {
            case "maxHealth":
                binding = new SampleBinding(() => baseMaxHealth, value => baseMaxHealth = value,
                    () => maxHealth, value =>
                    {
                        maxHealth = value;
                        currentHealth = Math.Min(currentHealth, maxHealth);
                    });
                return true;
            case "attack":
                binding = new SampleBinding(() => baseAttack, value => baseAttack = value,
                    () => attack, value => attack = value);
                return true;
            case "moveSpeed":
                binding = new SampleBinding(() => baseMoveSpeed, value => baseMoveSpeed = value,
                    () => moveSpeed, value => moveSpeed = value);
                return true;
            default:
                binding = null;
                return false;
        }
    }

    /// <summary>封装样例字段的显式读写委托，不让通用包反射业务字段。</summary>
    private sealed class SampleBinding : IStatBinding
    {
        private readonly Func<double> getBase;
        private readonly Action<double> setBase;
        private readonly Func<double> getFinal;
        private readonly Action<double> applyFinal;

        /// <summary>记录业务域四个方向的权威访问器。</summary>
        internal SampleBinding(Func<double> getBase, Action<double> setBase,
            Func<double> getFinal, Action<double> applyFinal)
        { this.getBase = getBase; this.setBase = setBase; this.getFinal = getFinal; this.applyFinal = applyFinal; }

        /// <summary>读取业务基础值。</summary>
        public double GetBase() => getBase();
        /// <summary>写入业务基础值。</summary>
        public void SetBase(double value) => setBase(value);
        /// <summary>读取业务最终值。</summary>
        public double GetFinal() => getFinal();
        /// <summary>提交业务最终值。</summary>
        public void ApplyFinal(double value) => applyFinal(value);
    }
}
