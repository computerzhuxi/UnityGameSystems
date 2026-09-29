using System;
using System.Collections.Generic;

namespace Computerzhuxi.Stats
{
    /// <summary>定义可绑定属性的稳定标识、计算策略和最终值约束。</summary>
    public sealed class StatDefinition
    {
        public string Id { get; }
        public IStatCalculationStrategy Strategy { get; }
        public double? Minimum { get; }
        public double? Maximum { get; }
        public StatRounding Rounding { get; }

        /// <summary>建立不可变属性定义；省略策略时使用先加固定值再逐项相乘。</summary>
        public StatDefinition(string id, IStatCalculationStrategy strategy = null,
            double? minimum = null, double? maximum = null,
            StatRounding rounding = StatRounding.None)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("属性 ID 不能为空。", nameof(id));
            if (minimum.HasValue) StatNumber.RequireFinite(minimum.Value, nameof(minimum));
            if (maximum.HasValue) StatNumber.RequireFinite(maximum.Value, nameof(maximum));
            if (minimum > maximum) throw new ArgumentException("下限不能大于上限。");
            if (!Enum.IsDefined(typeof(StatRounding), rounding)) throw new ArgumentOutOfRangeException(nameof(rounding));
            if (rounding != StatRounding.None
                && ((minimum.HasValue && Math.Truncate(minimum.Value) != minimum.Value)
                    || (maximum.HasValue && Math.Truncate(maximum.Value) != maximum.Value)))
                throw new ArgumentException("整数取整模式要求整数上下限。");
            Id = id;
            Strategy = strategy ?? DefaultStatCalculationStrategy.Instance;
            Minimum = minimum;
            Maximum = maximum;
            Rounding = rounding;
        }

        /// <summary>先限幅再取整；整数模式的边界也必须是整数。</summary>
        public double Constrain(double value)
        {
            StatNumber.RequireFinite(value, nameof(value));
            if (Minimum.HasValue) value = Math.Max(Minimum.Value, value);
            if (Maximum.HasValue) value = Math.Min(Maximum.Value, value);
            switch (Rounding)
            {
                case StatRounding.Floor: value = Math.Floor(value); break;
                case StatRounding.Ceiling: value = Math.Ceiling(value); break;
                case StatRounding.Nearest: value = Math.Round(value, 0, MidpointRounding.AwayFromZero); break;
            }
            return value;
        }
    }

    /// <summary>最终值取整方式。</summary>
    public enum StatRounding { None, Floor, Ceiling, Nearest }

    /// <summary>业务域对基础值和最终值的唯一权威绑定。</summary>
    public interface IStatBinding
    {
        /// <summary>读取业务域权威基础值。</summary>
        double GetBase();
        /// <summary>写入业务域权威基础值。</summary>
        void SetBase(double value);
        /// <summary>读取业务域权威最终值。</summary>
        double GetFinal();
        /// <summary>提交计算结果给业务域。</summary>
        void ApplyFinal(double value);
    }

    /// <summary>计算器声明支持的修正类别并计算未约束结果。</summary>
    public interface IStatCalculationStrategy
    {
        /// <summary>声明策略是否接纳指定修正类别。</summary>
        bool SupportsCategory(string category);
        /// <summary>根据基础值和修正列表计算未约束结果。</summary>
        double Calculate(double baseValue, IReadOnlyList<StatModifier> modifiers);
    }

    /// <summary>默认策略支持的固定值和倍率类别。</summary>
    public static class StatModifierCategory
    {
        public const string Flat = "Flat";
        public const string Multiplier = "Multiplier";
    }

    /// <summary>来源和数值不可变的单项属性修正。</summary>
    public sealed class StatModifier
    {
        public string Category { get; }
        public double Value { get; }
        public string Source { get; }

        /// <summary>建立修正；来源用于按业务所有者成组撤销。</summary>
        public StatModifier(string category, double value, string source)
        {
            if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("修正类别不能为空。", nameof(category));
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("修正来源不能为空。", nameof(source));
            StatNumber.RequireFinite(value, nameof(value));
            Category = category;
            Value = value;
            Source = source;
        }
    }

    /// <summary>默认公式：(Base + 固定加成合计) × 各倍率乘积。</summary>
    public sealed class DefaultStatCalculationStrategy : IStatCalculationStrategy
    {
        public static readonly DefaultStatCalculationStrategy Instance = new DefaultStatCalculationStrategy();
        private DefaultStatCalculationStrategy() { }

        /// <summary>仅接受固定加成和倍率。</summary>
        public bool SupportsCategory(string category) => category == StatModifierCategory.Flat || category == StatModifierCategory.Multiplier;

        /// <summary>分别累加固定值、累乘倍率并计算未约束最终值。</summary>
        public double Calculate(double baseValue, IReadOnlyList<StatModifier> modifiers)
        {
            StatNumber.RequireFinite(baseValue, nameof(baseValue));
            double flat = 0d;
            double multiplier = 1d;
            for (int index = 0; index < modifiers.Count; index++)
            {
                StatModifier modifier = modifiers[index];
                if (modifier.Category == StatModifierCategory.Flat) flat += modifier.Value;
                else if (modifier.Category == StatModifierCategory.Multiplier) multiplier *= modifier.Value;
                else throw new ArgumentException("默认策略不支持修正类别 " + modifier.Category);
            }
            double result = (baseValue + flat) * multiplier;
            StatNumber.RequireFinite(result, nameof(result));
            return result;
        }
    }

    /// <summary>公开且不可变的最终值变化事实。</summary>
    public readonly struct StatChange
    {
        public string StatId { get; }
        public double PreviousBase { get; }
        public double CurrentBase { get; }
        public double PreviousFinal { get; }
        public double CurrentFinal { get; }

        /// <summary>保存一次成功提交的基础值和最终值变化。</summary>
        public StatChange(string statId, double previousBase, double currentBase,
            double previousFinal, double currentFinal)
        {
            StatId = statId;
            PreviousBase = previousBase;
            CurrentBase = currentBase;
            PreviousFinal = previousFinal;
            CurrentFinal = currentFinal;
        }
    }

    /// <summary>只向消费者暴露属性读数与最终变化。</summary>
    public interface IReadOnlyStatRegistry
    {
        /// <summary>任意基础值或最终值变化时发布已提交状态。</summary>
        event Action<StatChange> Changed;
        /// <summary>返回按 ID 排序的当前属性标识快照。</summary>
        IReadOnlyList<string> StatIds { get; }
        /// <summary>读取业务域当前基础值。</summary>
        double GetBase(string statId);
        /// <summary>读取业务域已提交最终值。</summary>
        double GetFinal(string statId);
        /// <summary>尝试读取业务域当前基础值。</summary>
        bool TryGetBase(string statId, out double value);
        /// <summary>尝试读取业务域已提交最终值。</summary>
        bool TryGetFinal(string statId, out double value);
    }

    /// <summary>拒绝进入计算链的非有限浮点数。</summary>
    internal static class StatNumber
    {
        /// <summary>验证数值不是 NaN 或正负无穷。</summary>
        internal static void RequireFinite(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameter, "属性数值必须是有限数。");
        }
    }
}
