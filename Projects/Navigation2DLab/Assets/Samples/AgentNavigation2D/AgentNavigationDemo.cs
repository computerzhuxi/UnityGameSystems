using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Computerzhuxi.Navigation2D.Samples
{
    /// <summary>演示组件便利入口与外部运动执行器；代理拥有导航状态，样例拥有位置、目标动画和速度。</summary>
    public sealed class AgentNavigationDemo : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float moveSpeed = 2;
        private NavigationAgent2DComponent navigation;
        private Transform marker, goal;
        private GameObject wall;
        private Sprite sprite;
        private bool movingTarget, smoke;
        private int frames;
        private float elapsed;
        /// <summary>创建示例对象，配置障碍层后再创建组件的唯一代理。</summary>
        private void Start()
        {
            smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "--agent-smoke") >= 0;
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new(.5f, .5f), Texture2D.whiteTexture.width);
            wall = Visual("Wall", Vector2.zero, new(.5f, 3), Color.gray); wall.layer = 8; wall.AddComponent<BoxCollider2D>();
            marker = Visual("Agent", new(-3, 0), Vector2.one * .4f, Color.cyan).transform;
            goal = Visual("Goal", new(3, 0), Vector2.one * .2f, Color.green).transform;
            // 显式配置掩码，不依赖消费者项目的层名称。
            navigation = marker.gameObject.AddComponent<NavigationAgent2DComponent>(); navigation.ConfigurePhysics(1 << 8);
            Physics2D.SyncTransforms(); navigation.Agent.SetDestination(goal.position);
        }
        /// <summary>创建由样例负责销毁的可见物体。</summary>
        private GameObject Visual(string label, Vector2 position, Vector2 scale, Color color)
        {
            var go = new GameObject(label); SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            go.transform.SetParent(transform); go.transform.position = position; go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; return go;
        }
        /// <summary>提交目标与真实位置后消费建议，限幅位移避免一帧跨过转角。</summary>
        private void Update()
        {
            if (navigation == null) return;
            elapsed += Time.deltaTime;
            if (movingTarget) goal.position = new Vector2(3, Mathf.Sin(elapsed) * 2);
            var agent = navigation.Agent;
            agent.UpdateDestination(goal.position); navigation.Tick(marker.position, Time.deltaTime);
            marker.position += (Vector3)(agent.DesiredDirection * Mathf.Min(moveSpeed * Time.deltaTime, agent.RemainingWaypointDistance));
            if (smoke && ++frames == 30)
            {
                bool pass = agent.QueryCount > 0 && agent.LastResult.HasValue && agent.LastResult.Value.Succeeded && marker.position.x > -3;
                Debug.Log(pass ? "NAVIGATION_AGENT_SMOKE_PASS" : "NAVIGATION_AGENT_SMOKE_FAIL"); Application.Quit(pass ? 0 : 1);
            }
        }
        /// <summary>通过公开接口提供移动目标、暂停、动态障碍及重启操作。</summary>
        private void OnGUI()
        {
            if (navigation == null) return;
            var agent = navigation.Agent;
            GUI.Label(new Rect(15, 15, 850, 30), $"Agent2D | {agent.State} | paused {agent.IsPaused} | queries {agent.QueryCount} | result {agent.LastResult?.Status}");
            if (GUI.Button(new Rect(15, 50, 180, 35), "Reset")) { marker.position = new(-3, 0); agent.SetDestination(goal.position); }
            if (GUI.Button(new Rect(15, 95, 180, 35), "Moving target")) movingTarget = !movingTarget;
            if (GUI.Button(new Rect(15, 140, 180, 35), "Pause / Resume")) { if (agent.IsPaused) agent.Resume(); else agent.Pause(); }
            if (GUI.Button(new Rect(15, 185, 180, 35), "Toggle obstacle")) { wall.SetActive(!wall.activeSelf); Physics2D.SyncTransforms(); agent.InvalidatePath(); }
        }
        /// <summary>显示剩余路径，供人工检查绕墙和转角。</summary>
        private void OnDrawGizmos()
        {
            if (navigation == null || marker == null) return;
            var agent = navigation.Agent; Vector3 previous = marker.position; Gizmos.color = Color.yellow;
            for (int i = agent.CurrentPathIndex; i < agent.CurrentPath.Count; i++)
            { Vector3 next = agent.CurrentPath[i]; Gizmos.DrawLine(previous, next); Gizmos.DrawWireSphere(next, .2f); previous = next; }
        }
        /// <summary>释放样例创建的图形资源。</summary>
        private void OnDestroy() { if (sprite != null) Destroy(sprite); }
    }
}
