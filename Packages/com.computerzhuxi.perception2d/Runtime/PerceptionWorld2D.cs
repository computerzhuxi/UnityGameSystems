using System;
using System.Collections.Generic;
using UnityEngine;

namespace Computerzhuxi.Perception2D
{
    /// <summary>显式隔离一组目标和观察者，按上报时刻的听觉资格分发声音。</summary>
    [DisallowMultipleComponent]
    public sealed class PerceptionWorld2D : MonoBehaviour
    {
        private readonly HashSet<PerceptionObserver2D> observers = new();
        public PerceptionTargetRegistry Registry { get; } = new();
        /// <summary>登记观察者，包括暂时停用但仍保存记忆的观察者。</summary>
        internal void RegisterObserver(PerceptionObserver2D observer) => observers.Add(observer);
        /// <summary>观察者销毁或更换环境时移除关联。</summary>
        internal void UnregisterObserver(PerceptionObserver2D observer) => observers.Remove(observer);
        /// <summary>销毁环境时立即失效注册身份，核心在自己的后续批次发布失效通知。</summary>
        private void OnDestroy()
        {
            Registry.InvalidateAll();
            observers.Clear();
        }
        /// <summary>上报声音并返回接收人数；主线程使用，零响度不产生事件。</summary>
        public int ReportNoise(NoiseEvent2D noise)
        {
            Guard.Vector(noise.Position); Guard.NonNegative(noise.Loudness, nameof(noise.Loudness));
            if (noise.MaxRange.HasValue) Guard.NonNegative(noise.MaxRange.Value, nameof(noise.MaxRange));
            if (!isActiveAndEnabled || noise.Loudness == 0) return 0;
            bool sourced = !ReferenceEquals(noise.Source, null);
            if (sourced && (noise.Source == null || !noise.Source.IsRegistered || noise.Source.World != this)) throw new ArgumentException("声源必须注册在同一感知环境。");
            int accepted = 0;
            foreach (var observer in observers)
                if (observer != null && observer.AcceptNoise(noise, Time.timeAsDouble)) accepted++;
            return accepted;
        }
    }
}
