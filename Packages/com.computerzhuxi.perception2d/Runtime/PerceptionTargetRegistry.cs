using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Computerzhuxi.Perception2D
{
    /// <summary>一个注册表内目标生命周期的不可变身份。</summary>
    public readonly struct PerceptionTargetHandle : IEquatable<PerceptionTargetHandle>
    {
        internal PerceptionTargetRegistry Registry { get; }
        public ulong Id { get; }
        public ulong Generation { get; }
        internal PerceptionTargetHandle(PerceptionTargetRegistry registry, ulong id, ulong generation)
        {
            Registry = registry;
            Id = id;
            Generation = generation;
        }

        /// <summary>比较注册表、目标编号和生命周期代次。</summary>
        public bool Equals(PerceptionTargetHandle other) => ReferenceEquals(Registry, other.Registry) && Id == other.Id && Generation == other.Generation;
        /// <summary>比较目标身份。</summary>
        public override bool Equals(object obj) => obj is PerceptionTargetHandle other && Equals(other);
        /// <summary>生成包含注册表归属的哈希值。</summary>
        public override int GetHashCode() => ((Registry == null ? 0 : RuntimeHelpers.GetHashCode(Registry)) * 397) ^ Id.GetHashCode() ^ Generation.GetHashCode();
    }

    /// <summary>共享目标身份；每个观察者另持有自己的感知记忆。</summary>
    public sealed class PerceptionTargetRegistry
    {
        private sealed class Entry
        {
            internal ulong Generation;
            internal object AssociatedObject;
        }

        private readonly Dictionary<ulong, Entry> entries = new();
        private sealed class IdentitySlot
        {
            internal ulong Id;
            internal ulong Generation;
        }

        // 弱键保留仍在外部存活的对象池槽位；注销后注册表不再强持有目标。
        private readonly ConditionalWeakTable<object, IdentitySlot> identities = new();
        private ulong nextId;
        private bool invalidated;
        internal int ActiveCount => entries.Count;

        /// <summary>注册一个身份，可选关联外部对象但注册表不读取其状态。</summary>
        public PerceptionTargetHandle Register(object associatedObject = null)
        {
            if (invalidated) throw new ObjectDisposedException(nameof(PerceptionTargetRegistry), "感知环境已经销毁。");
            if (associatedObject != null && identities.TryGetValue(associatedObject, out IdentitySlot slot))
            {
                if (entries.ContainsKey(slot.Id)) throw new InvalidOperationException("目标已经注册。");
                slot.Generation = checked(slot.Generation + 1);
                entries.Add(slot.Id, new Entry { Generation = slot.Generation, AssociatedObject = associatedObject });
                return new PerceptionTargetHandle(this, slot.Id, slot.Generation);
            }

            ulong id = checked(++nextId);
            entries.Add(id, new Entry { Generation = 1, AssociatedObject = associatedObject });
            if (associatedObject != null) identities.Add(associatedObject, new IdentitySlot { Id = id, Generation = 1 });
            return new PerceptionTargetHandle(this, id, 1);
        }

        /// <summary>注销一个生命周期，使此前排队的输入失效。</summary>
        public void Unregister(PerceptionTargetHandle handle)
        {
            ValidateOwner(handle);
            if (entries.TryGetValue(handle.Id, out Entry entry) && entry.Generation == handle.Generation)
                entries.Remove(handle.Id);
        }

        /// <summary>检查句柄是否仍指向当前已注册生命周期。</summary>
        public bool IsValid(PerceptionTargetHandle handle)
        {
            ValidateOwner(handle);
            return entries.TryGetValue(handle.Id, out Entry entry) && entry.Generation == handle.Generation;
        }

        /// <summary>取得注册时的关联对象，不将其解释为位置或状态。</summary>
        public object GetAssociatedObject(PerceptionTargetHandle handle)
        {
            ValidateOwner(handle);
            return entries.TryGetValue(handle.Id, out Entry entry) && entry.Generation == handle.Generation
                ? entry.AssociatedObject : null;
        }

        /// <summary>环境销毁时失效所有活动身份；各核心在下一批自行清理旧记忆。</summary>
        internal void InvalidateAll()
        {
            invalidated = true;
            entries.Clear();
        }

        /// <summary>拒绝来自其他注册表或未初始化的句柄。</summary>
        internal void ValidateOwner(PerceptionTargetHandle handle)
        {
            if (!ReferenceEquals(handle.Registry, this)) throw new ArgumentException("目标句柄不属于此注册表。", nameof(handle));
        }
    }
}
