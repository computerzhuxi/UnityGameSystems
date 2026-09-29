using System;
using System.Collections.Generic;

namespace Computerzhuxi.Stats
{
    /// <summary>标识一次注册；释放后不会影响同 ID 的后续注册。</summary>
    public sealed class StatRegistration : IDisposable
    {
        private readonly StatRegistry owner;
        internal readonly long Token;
        internal readonly string Id;
        private bool disposed;

        /// <summary>建立与单次注册关联的释放句柄。</summary>
        internal StatRegistration(StatRegistry owner, string id, long token)
        { this.owner = owner; Id = id; Token = token; }

        /// <summary>幂等释放当前注册及其修正。</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            owner.Release(this);
        }
    }

    /// <summary>精确指向一次修正的不可变句柄。</summary>
    public readonly struct StatModifierHandle : IEquatable<StatModifierHandle>
    {
        internal readonly Guid RegistryId;
        internal readonly long Token;
        /// <summary>建立只在来源 Registry 内有效的修正句柄。</summary>
        internal StatModifierHandle(Guid registryId, long token)
        { RegistryId = registryId; Token = token; }
        /// <summary>按 Registry 身份和修正令牌比较。</summary>
        public bool Equals(StatModifierHandle other) => RegistryId == other.RegistryId && Token == other.Token;
        /// <summary>比较两个句柄是否指向同一修正。</summary>
        public override bool Equals(object obj) => obj is StatModifierHandle other && Equals(other);
        /// <summary>返回句柄身份的散列码。</summary>
        public override int GetHashCode() => RegistryId.GetHashCode() ^ Token.GetHashCode();
    }

    /// <summary>拥有注册、修正、失效队列和最终值提交的通用运行时。</summary>
    public sealed class StatRegistry : IReadOnlyStatRegistry
    {
        private sealed class Entry
        {
            internal StatDefinition Definition;
            internal IStatBinding Binding;
            internal long Token;
            internal double LastBase;
            internal double LastFinal;
            internal readonly SortedDictionary<long, StatModifier> Modifiers = new SortedDictionary<long, StatModifier>();
        }

        private sealed class BatchScope : IDisposable
        {
            private StatRegistry owner;
            /// <summary>持有需要结束的一层批处理。</summary>
            internal BatchScope(StatRegistry owner) { this.owner = owner; }

            /// <summary>结束一层批处理，最外层结束时统一提交失效属性。</summary>
            public void Dispose()
            {
                StatRegistry current = owner;
                if (current == null) return;
                owner = null;
                current.EndBatch();
            }
        }

        private readonly Guid registryId = Guid.NewGuid();
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private readonly Dictionary<long, string> modifierOwners = new Dictionary<long, string>();
        private readonly HashSet<string> dirty = new HashSet<string>(StringComparer.Ordinal);
        private long nextToken;
        private int batchDepth;
        private bool processing;
        private bool invokingBinding;

        public event Action<StatChange> Changed;

        /// <summary>取得按稳定 ID 排序的当前注册标识快照。</summary>
        public IReadOnlyList<string> StatIds
        {
            get
            {
                GuardBindingReentry();
                List<string> ids = new List<string>(entries.Keys);
                ids.Sort(StringComparer.Ordinal);
                return ids.AsReadOnly();
            }
        }

        /// <summary>注册业务域绑定并计算初次最终值；同实例重复 ID 被拒绝。</summary>
        public StatRegistration Register(StatDefinition definition, IStatBinding binding)
        {
            GuardBindingReentry();
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            if (entries.ContainsKey(definition.Id)) throw new InvalidOperationException("属性 ID 已注册：" + definition.Id);
            double baseValue = InvokeBinding(binding.GetBase);
            double finalValue = InvokeBinding(binding.GetFinal);
            StatNumber.RequireFinite(baseValue, nameof(baseValue));
            StatNumber.RequireFinite(finalValue, nameof(finalValue));
            Entry entry = new Entry { Definition = definition, Binding = binding,
                Token = ++nextToken, LastBase = baseValue, LastFinal = finalValue };
            entries.Add(definition.Id, entry);
            try { Invalidate(definition.Id); }
            catch
            {
                // 首次绑定失败时撤掉尚未公开的注册；绑定自身的副作用不承诺回滚。
                foreach (long token in entry.Modifiers.Keys) modifierOwners.Remove(token);
                entries.Remove(definition.Id);
                dirty.Remove(definition.Id);
                throw;
            }
            return new StatRegistration(this, definition.Id, entry.Token);
        }

        /// <summary>读取业务域当前基础值，包括批处理中已写入的新值。</summary>
        public double GetBase(string statId)
        {
            GuardBindingReentry();
            double value = InvokeBinding(Required(statId).Binding.GetBase);
            StatNumber.RequireFinite(value, nameof(value));
            return value;
        }

        /// <summary>读取业务域已提交的最终值；批处理中不提前计算。</summary>
        public double GetFinal(string statId)
        { GuardBindingReentry(); return Required(statId).LastFinal; }

        /// <summary>尝试读取当前基础值。</summary>
        public bool TryGetBase(string statId, out double value)
        {
            GuardBindingReentry();
            if (statId != null && entries.ContainsKey(statId)) { value = GetBase(statId); return true; }
            value = 0d; return false;
        }

        /// <summary>尝试读取已提交的最终值。</summary>
        public bool TryGetFinal(string statId, out double value)
        {
            GuardBindingReentry();
            if (statId != null && entries.TryGetValue(statId, out Entry entry)) { value = entry.LastFinal; return true; }
            value = 0d; return false;
        }

        /// <summary>经业务绑定写入永久基础值，然后使最终值失效。</summary>
        public void SetBase(string statId, double value)
        {
            GuardBindingReentry();
            StatNumber.RequireFinite(value, nameof(value));
            Entry entry = Required(statId);
            InvokeBinding(() => entry.Binding.SetBase(value));
            Invalidate(statId);
        }

        /// <summary>外部业务域直接改变基础值后请求重新计算。</summary>
        public void Invalidate(string statId)
        {
            GuardBindingReentry();
            Required(statId);
            dirty.Add(statId);
            ProcessDirty();
        }

        /// <summary>增加单项修正并返回只属于此 Registry 的精确句柄。</summary>
        public StatModifierHandle AddModifier(string statId, StatModifier modifier)
        {
            GuardBindingReentry();
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            Entry entry = Required(statId);
            if (!InvokeBinding(() => entry.Definition.Strategy.SupportsCategory(modifier.Category)))
                throw new ArgumentException("策略不支持修正类别 " + modifier.Category, nameof(modifier));
            long token = ++nextToken;
            entry.Modifiers.Add(token, modifier);
            modifierOwners.Add(token, statId);
            try { Invalidate(statId); }
            catch
            {
                // 未交出句柄时不能让修正残留；已发生的绑定副作用由调用者处理。
                entry.Modifiers.Remove(token);
                modifierOwners.Remove(token);
                dirty.Add(statId);
                throw;
            }
            return new StatModifierHandle(registryId, token);
        }

        /// <summary>用同一精确句柄替换修正内容，保持句柄生命周期不变。</summary>
        public bool UpdateModifier(StatModifierHandle handle, StatModifier modifier)
        {
            GuardBindingReentry();
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            if (handle.RegistryId != registryId || !modifierOwners.TryGetValue(handle.Token, out string statId)) return false;
            Entry entry = Required(statId);
            if (!InvokeBinding(() => entry.Definition.Strategy.SupportsCategory(modifier.Category)))
                throw new ArgumentException("策略不支持修正类别 " + modifier.Category, nameof(modifier));
            StatModifier previous = entry.Modifiers[handle.Token];
            entry.Modifiers[handle.Token] = modifier;
            try { Invalidate(statId); }
            catch
            {
                entry.Modifiers[handle.Token] = previous;
                dirty.Add(statId);
                throw;
            }
            return true;
        }

        /// <summary>只移除精确句柄所代表的修正；重复或跨实例移除返回 false。</summary>
        public bool RemoveModifier(StatModifierHandle handle)
        {
            GuardBindingReentry();
            if (handle.RegistryId != registryId || !modifierOwners.TryGetValue(handle.Token, out string statId)) return false;
            modifierOwners.Remove(handle.Token);
            if (!entries.TryGetValue(statId, out Entry entry)) return false;
            entry.Modifiers.Remove(handle.Token);
            Invalidate(statId);
            return true;
        }

        /// <summary>移除所有属性中属于同一来源的修正，适合装备或 Buff 生命周期。</summary>
        public int RemoveSource(string source)
        {
            GuardBindingReentry();
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("来源不能为空。", nameof(source));
            int removed = 0;
            using (BeginBatch())
            {
                foreach (KeyValuePair<string, Entry> pair in entries)
                {
                    List<long> matching = new List<long>();
                    foreach (KeyValuePair<long, StatModifier> item in pair.Value.Modifiers)
                        if (string.Equals(item.Value.Source, source, StringComparison.Ordinal)) matching.Add(item.Key);
                    foreach (long token in matching)
                    {
                        pair.Value.Modifiers.Remove(token);
                        modifierOwners.Remove(token);
                        removed++;
                    }
                    if (matching.Count > 0) Invalidate(pair.Key);
                }
            }
            return removed;
        }

        /// <summary>开始可嵌套批处理；提交前最终值始终返回上次已提交值。</summary>
        public IDisposable BeginBatch()
        {
            GuardBindingReentry();
            batchDepth++;
            return new BatchScope(this);
        }

        /// <summary>获取仅含永久基础值的快照，供调用方决定存档格式。</summary>
        public IReadOnlyDictionary<string, double> CaptureBaseSnapshot()
        {
            GuardBindingReentry();
            SortedDictionary<string, double> snapshot = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (string id in StatIds) snapshot.Add(id, GetBase(id));
            return snapshot;
        }

        /// <summary>只在句柄仍指向当前注册时释放，防止旧句柄清理新注册。</summary>
        internal void Release(StatRegistration registration)
        {
            GuardBindingReentry();
            if (!entries.TryGetValue(registration.Id, out Entry entry) || entry.Token != registration.Token) return;
            foreach (long token in entry.Modifiers.Keys) modifierOwners.Remove(token);
            entries.Remove(registration.Id);
            dirty.Remove(registration.Id);
        }

        /// <summary>结束嵌套层并在最外层处理积累的变更。</summary>
        private void EndBatch()
        {
            batchDepth--;
            if (batchDepth < 0) throw new InvalidOperationException("批处理深度无效。");
            ProcessDirty();
        }

        /// <summary>逐轮提交失效属性并通知；单项失败不丢失其他项和成功事实。</summary>
        private void ProcessDirty()
        {
            if (batchDepth > 0 || processing) return;
            processing = true;
            try
            {
                int rounds = 0;
                while (dirty.Count > 0)
                {
                    if (++rounds > 64) throw new InvalidOperationException("属性变化超过 64 轮，可能存在回调循环。");
                    List<string> wave = new List<string>(dirty);
                    wave.Sort(StringComparer.Ordinal);
                    dirty.Clear();
                    List<StatChange> changes = new List<StatChange>();
                    List<Exception> errors = new List<Exception>();
                    foreach (string id in wave)
                    {
                        if (!entries.TryGetValue(id, out Entry entry)) continue;
                        try
                        {
                            double baseValue = InvokeBinding(entry.Binding.GetBase);
                            StatNumber.RequireFinite(baseValue, nameof(baseValue));
                            List<StatModifier> modifiers = new List<StatModifier>(entry.Modifiers.Values);
                            double result = entry.Definition.Constrain(InvokeBinding(
                                () => entry.Definition.Strategy.Calculate(baseValue, modifiers)));
                            double previousBase = entry.LastBase;
                            double previousFinal = entry.LastFinal;
                            if (!result.Equals(previousFinal))
                                InvokeBinding(() => entry.Binding.ApplyFinal(result));
                            double actualFinal = InvokeBinding(entry.Binding.GetFinal);
                            StatNumber.RequireFinite(actualFinal, nameof(actualFinal));
                            entry.LastBase = baseValue;
                            entry.LastFinal = actualFinal;
                            if (!baseValue.Equals(previousBase) || !actualFinal.Equals(previousFinal))
                                changes.Add(new StatChange(id, previousBase, baseValue, previousFinal, actualFinal));
                        }
                        catch (Exception exception)
                        {
                            // 失败项保留以供显式失效重试，其他项继续提交。
                            dirty.Add(id);
                            errors.Add(new InvalidOperationException("属性提交失败：" + id, exception));
                        }
                    }
                    foreach (StatChange change in changes)
                    {
                        Action<StatChange> handlers = Changed;
                        if (handlers == null) continue;
                        foreach (Delegate handler in handlers.GetInvocationList())
                        {
                            try { ((Action<StatChange>)handler)(change); }
                            catch (Exception exception) { errors.Add(exception); }
                        }
                    }
                    if (errors.Count > 0) throw new AggregateException("属性提交或观察者通知失败。", errors);
                }
            }
            finally { processing = false; }
        }

        /// <summary>取得已注册属性或抛出清楚的调用错误。</summary>
        private Entry Required(string statId)
        {
            if (statId == null) throw new ArgumentNullException(nameof(statId));
            if (!entries.TryGetValue(statId, out Entry entry)) throw new KeyNotFoundException("属性未注册：" + statId);
            return entry;
        }

        /// <summary>拒绝绑定和策略委托同步调用当前 Registry，避免在提交中改换身份。</summary>
        private void GuardBindingReentry()
        {
            if (invokingBinding) throw new InvalidOperationException("绑定或策略回调不能同步调用同一个 StatRegistry。");
        }

        /// <summary>在受保护边界内执行绑定或策略查询。</summary>
        private T InvokeBinding<T>(Func<T> callback)
        {
            GuardBindingReentry();
            invokingBinding = true;
            try { return callback(); }
            finally { invokingBinding = false; }
        }

        /// <summary>在受保护边界内执行绑定写入。</summary>
        private void InvokeBinding(Action callback)
        {
            GuardBindingReentry();
            invokingBinding = true;
            try { callback(); }
            finally { invokingBinding = false; }
        }
    }
}
