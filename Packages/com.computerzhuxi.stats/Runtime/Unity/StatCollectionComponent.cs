using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computerzhuxi.Stats
{
    /// <summary>用定义资产和显式绑定提供者装配通用属性运行时。</summary>
    [DisallowMultipleComponent]
    public sealed class StatCollectionComponent : MonoBehaviour
    {
        [SerializeField, Tooltip("实现 IStatBindingProvider 的同角色组件。")]
        private MonoBehaviour bindingProvider;
        [SerializeField, Tooltip("只在首次初始化时读取的属性定义资产。")]
        private StatDefinitionAsset[] definitions = Array.Empty<StatDefinitionAsset>();

        private StatRegistry registry;
        private readonly List<StatRegistration> registrations = new List<StatRegistration>();

        public bool IsInitialized => registry != null;
        public IReadOnlyStatRegistry Stats => registry;

        /// <summary>在其他脚本完成 Awake 后，自动执行一次资产装配。</summary>
        private void Start()
        {
            if (!IsInitialized) Initialize();
        }

        /// <summary>首次初始化时读取定义资产和显式绑定提供者。</summary>
        public void Initialize()
        {
            if (IsInitialized) return;
            if (!(bindingProvider is IStatBindingProvider provider))
                throw new InvalidOperationException("Stats 需要实现 IStatBindingProvider 的绑定组件。");
            if (definitions == null || definitions.Length == 0)
                throw new InvalidOperationException("Stats 至少需要一个定义资产。");
            var runtimeDefinitions = new List<StatDefinition>(definitions.Length);
            foreach (StatDefinitionAsset asset in definitions)
            {
                if (asset == null) throw new InvalidOperationException("Stats 定义列表包含空资产。");
                runtimeDefinitions.Add(asset.CreateDefinition());
            }
            Initialize(provider, runtimeDefinitions);
        }

        /// <summary>编程式装配组件，共享相同 Registry 规则与状态。</summary>
        public void Initialize(IStatBindingProvider provider, IReadOnlyList<StatDefinition> runtimeDefinitions)
        {
            if (IsInitialized) return;
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (runtimeDefinitions == null || runtimeDefinitions.Count == 0)
                throw new ArgumentException("至少需要一个属性定义。", nameof(runtimeDefinitions));
            var candidate = new StatRegistry();
            var pending = new List<StatRegistration>();
            try
            {
                using (candidate.BeginBatch())
                {
                    foreach (StatDefinition definition in runtimeDefinitions)
                    {
                        if (definition == null) throw new ArgumentException("定义不能为空。", nameof(runtimeDefinitions));
                        if (!provider.TryGetBinding(definition.Id, out IStatBinding binding) || binding == null)
                            throw new InvalidOperationException("绑定提供者缺少属性 " + definition.Id);
                        pending.Add(candidate.Register(definition, binding));
                    }
                }
            }
            catch
            {
                foreach (StatRegistration registration in pending) registration.Dispose();
                throw;
            }
            registrations.AddRange(pending);
            registry = candidate;
        }

        /// <summary>禁用组件时保留状态；销毁时释放全部注册与修正。</summary>
        private void OnDestroy()
        {
            foreach (StatRegistration registration in registrations) registration.Dispose();
            registrations.Clear();
            registry = null;
        }

        /// <summary>经业务域绑定写入永久基础值。</summary>
        public void SetBase(string statId, double value) => Required().SetBase(statId, value);

        /// <summary>外部业务域改变基础值后使属性失效。</summary>
        public void Invalidate(string statId) => Required().Invalidate(statId);

        /// <summary>添加来源明确的修正并返回精确句柄。</summary>
        public StatModifierHandle AddModifier(string statId, StatModifier modifier) => Required().AddModifier(statId, modifier);

        /// <summary>通过精确句柄更新修正内容。</summary>
        public bool UpdateModifier(StatModifierHandle handle, StatModifier modifier) => Required().UpdateModifier(handle, modifier);

        /// <summary>通过精确句柄移除修正。</summary>
        public bool RemoveModifier(StatModifierHandle handle) => Required().RemoveModifier(handle);

        /// <summary>在装备或 Buff 结束时移除该来源的所有修正。</summary>
        public int RemoveSource(string source) => Required().RemoveSource(source);

        /// <summary>开始可嵌套的属性批处理。</summary>
        public IDisposable BeginBatch() => Required().BeginBatch();

        /// <summary>取得基础值快照，由游戏自行保存和恢复。</summary>
        public IReadOnlyDictionary<string, double> CaptureBaseSnapshot() => Required().CaptureBaseSnapshot();

        /// <summary>要求组件已经完成显式或自动初始化。</summary>
        private StatRegistry Required()
        {
            if (registry == null) throw new InvalidOperationException("Stats 尚未初始化。");
            return registry;
        }
    }
}
