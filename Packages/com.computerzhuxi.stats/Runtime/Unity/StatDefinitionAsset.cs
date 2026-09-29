using System;
using UnityEngine;

namespace Computerzhuxi.Stats
{
    /// <summary>为 Inspector 装配提供不可变运行时策略的资产工厂。</summary>
    public abstract class StatStrategyAsset : ScriptableObject
    {
        /// <summary>为单次属性注册创建计算策略。</summary>
        public abstract IStatCalculationStrategy CreateStrategy();
    }

    /// <summary>从序列化初值创建运行时属性定义，运行期间修改资产不会自动生效。</summary>
    [CreateAssetMenu(fileName = "StatDefinition", menuName = "Computerzhuxi/Stats/Definition")]
    public sealed class StatDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string statId;
        [SerializeField] private StatStrategyAsset strategy;
        [SerializeField] private bool useMinimum;
        [SerializeField] private double minimum;
        [SerializeField] private bool useMaximum;
        [SerializeField] private double maximum;
        [SerializeField] private StatRounding rounding;

        public string StatId => statId;

        /// <summary>读取序列化配置并创建不会随资产编辑变化的定义。</summary>
        public StatDefinition CreateDefinition()
        {
            IStatCalculationStrategy calculation = strategy == null ? null : strategy.CreateStrategy();
            if (strategy != null && calculation == null)
                throw new InvalidOperationException("策略资产返回了空计算策略：" + name);
            return new StatDefinition(statId, calculation,
                useMinimum ? minimum : (double?)null,
                useMaximum ? maximum : (double?)null, rounding);
        }
    }

    /// <summary>显式把属性 ID 连接到业务域权威字段，不反射字段名。</summary>
    public interface IStatBindingProvider
    {
        /// <summary>为指定属性提供属于本角色的权威数值绑定。</summary>
        bool TryGetBinding(string statId, out IStatBinding binding);
    }
}
