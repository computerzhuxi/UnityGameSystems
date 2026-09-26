using UnityEngine;
using UnityEngine.SceneManagement;

namespace Computerzhuxi.Navigation2D
{
    /// <summary>提供 Inspector 配置与 Physics2D 障碍源；调用方显式 Tick 并执行移动，组件不自动驱动物体。</summary>
    [DisallowMultipleComponent]
    public sealed class NavigationAgent2DComponent : MonoBehaviour
    {
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField, Min(.01f)] private float cellSize = .5f;
        [SerializeField, Min(0)] private float radius = .2f;
        [SerializeField, Min(1)] private int maxExpandedNodes = 4096;
        [SerializeField] private GridDirections directions = GridDirections.Eight;
        [SerializeField, Min(.001f)] private float arrivalDistance = .08f;
        [SerializeField, Min(.001f)] private float destinationChangeThreshold = .2f;
        [SerializeField, Min(0)] private float repathInterval = .1f;
        [SerializeField, Min(.001f)] private float failureRetryInterval = .25f;
        private NavigationAgent2D agent;
        private PhysicsTraversalSource2D source;
        private PhysicsScene2D physicsScene;
        /// <summary>返回唯一状态拥有者；首次读取按序列化配置创建。</summary>
        public NavigationAgent2D Agent { get { EnsureInitialized(); return agent; } }
        /// <summary>首次使用时创建运行期对象，不将路径写入序列化资产。</summary>
        private void EnsureInitialized()
        {
            if (agent != null) return;
            physicsScene = gameObject.scene.GetPhysicsScene2D();
            source = new PhysicsTraversalSource2D(physicsScene, obstacleMask, false);
            agent = new NavigationAgent2D(new GridSettings2D(Vector2.zero, cellSize),
                new PathOptions2D(radius, maxExpandedNodes, directions),
                new AgentSettings2D(arrivalDistance, destinationChangeThreshold, repathInterval, failureRetryInterval));
            if (!isActiveAndEnabled) agent.Pause();
        }
        /// <summary>按调用方真实位置更新建议，通常在固定步长移动前调用；禁用时不推进。</summary>
        public void Tick(Vector2 position, float deltaTime)
        {
            if (!isActiveAndEnabled) return;
            EnsureInitialized();
            PhysicsScene2D currentScene = gameObject.scene.GetPhysicsScene2D();
            if (physicsScene != currentScene)
            {
                // 手动入口仍保留任务；切场景后必须放弃旧路径和旧场景的物理查询源。
                physicsScene = currentScene;
                source = new PhysicsTraversalSource2D(currentScene, obstacleMask, false);
                agent.InvalidatePath();
            }
            agent.Tick(position, deltaTime, source);
        }
        /// <summary>配置障碍掩码并重建代理；清除旧任务，不适合逐帧调用。</summary>
        public void ConfigurePhysics(LayerMask mask) { obstacleMask = mask; Reinitialize(); }
        /// <summary>主动重新读取 Inspector 配置并清除原任务；动态层级项目应使用纯逻辑 Agent 与自己的域适配器。</summary>
        public void Reinitialize() { agent?.Stop(); agent = null; EnsureInitialized(); }
        /// <summary>重新启用时验证新位置，避免禁用期间移动或地图变化后继续旧路径。</summary>
        private void OnEnable() { if (agent != null) { agent.Resume(); agent.InvalidatePath(); } }
        /// <summary>禁用时立即清除建议；实际移动执行器仍需自行停止速度。</summary>
        private void OnDisable() { agent?.Pause(); }
        /// <summary>显示尚未走完的路径；绘制不改变代理状态。</summary>
        private void OnDrawGizmosSelected()
        {
            if (agent == null || !agent.HasPath) return;
            Gizmos.color = Color.cyan;
            Vector3 previous = transform.position;
            for (int i = agent.CurrentPathIndex; i < agent.CurrentPath.Count; i++)
            {
                Vector3 next = agent.CurrentPath[i]; Gizmos.DrawLine(previous, next);
                Gizmos.DrawWireSphere(next, radius); previous = next;
            }
        }
    }
}
