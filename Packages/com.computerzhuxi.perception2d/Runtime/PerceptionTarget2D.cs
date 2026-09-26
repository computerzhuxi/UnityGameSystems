using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Computerzhuxi.Perception2D
{
    /// <summary>标识可感知对象及单一视觉检测点，注册代次隔离对象池生命周期。</summary>
    [DisallowMultipleComponent]
    [MovedFrom(true, sourceNamespace: "", sourceAssembly: "ARPG.Runtime", sourceClassName: "PerceptionTarget2D")]
    public sealed class PerceptionTarget2D : MonoBehaviour
    {
        [SerializeField] private PerceptionWorld2D world;
        [SerializeField] private Transform targetRoot;
        [SerializeField] private Transform sightPoint;
        [SerializeField] private bool sightDetectable = true;
        private ulong lastGeneration;
        public Transform TargetRoot => targetRoot != null ? targetRoot : transform;
        public Vector2 SightPosition => sightPoint != null ? (Vector2)sightPoint.position : (Vector2)TargetRoot.position;
        public PerceptionWorld2D World => world;
        public PerceptionTargetHandle Handle { get; private set; }
        public ulong Generation => lastGeneration;
        public bool IsRegistered => world != null && Handle.Id != 0 && world.Registry.IsValid(Handle);
        public bool SightDetectable { get => sightDetectable; set => sightDetectable = value; }
        /// <summary>显式绑定感知环境；更换环境结束原注册生命周期。</summary>
        public void Bind(PerceptionWorld2D value)
        {
            if (ReferenceEquals(world, value)) return;
            Unregister(); world = value;
            if (isActiveAndEnabled) Register();
        }
        /// <summary>启用时注册到显式配置的环境。</summary>
        private void OnEnable() => Register();
        /// <summary>停用立即终止身份，旧事件不得作用于下一生命周期。</summary>
        private void OnDisable() => Unregister();
        /// <summary>建立新的注册代次。</summary>
        private void Register()
        {
            if (world == null || IsRegistered) return;
            Handle = world.Registry.Register(this);
            lastGeneration = Handle.Generation;
        }
        /// <summary>注销当前目标并使所有关联记忆失效。</summary>
        private void Unregister()
        {
            if (Handle.Id == 0) return;
            // World 可能先被 Unity 销毁；句柄仍保留其注册表以完成幂等注销。
            Handle.Registry.Unregister(Handle);
            Handle = default;
        }
    }
}
