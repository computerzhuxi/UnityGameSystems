using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Computerzhuxi.Navigation2D.Samples
{
    /// <summary>演示路径期望、批量避让与外部运动的组合；所有图形和位置都由样例拥有。</summary>
    public sealed class CrowdNavigationDemo : MonoBehaviour
    {
        private sealed class Actor
        {
            internal Transform View;
            internal NavigationAgent2D Agent;
            internal Vector2 Position, Velocity, Goal;
            internal float Radius;
            internal bool Locked;
        }
        private readonly List<Actor> actors = new();
        private readonly List<AvoidanceAgent2D> snapshots = new(16);
        private readonly List<AvoidanceResult2D> results = new(16);
        private readonly List<GameObject> visuals = new();
        private readonly AvoidanceWorld2D world = new(16, 15, 6, 1, .1f);
        private PhysicsTraversalSource2D source;
        private Texture2D circleTexture;
        private Sprite circle, square;
        private int scenario, steps;
        private bool paused, smoke;
        public int CompletedCount { get; private set; }
        public float MinimumGap { get; private set; } = float.PositiveInfinity;
        public int BlockedSteps { get; private set; }

        /// <summary>建立运行时图形，默认打开两角色迎面场景。</summary>
        private void Start()
        {
            circleTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                circleTexture.SetPixel(x, y, new Vector2(x - 31.5f, y - 31.5f).sqrMagnitude <= 31 * 31 ? Color.white : Color.clear);
            circleTexture.Apply();
            circle = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), new(.5f, .5f), 64);
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new(.5f, .5f), Texture2D.whiteTexture.width);
            source = new PhysicsTraversalSource2D(gameObject.scene.GetPhysicsScene2D(), 1 << 8);
            smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "--crowd-smoke") >= 0;
            SetScenario(0);
        }

        /// <summary>选择迎面、停止障碍、八人交叉或窄道场景；重置运动与测量结果。</summary>
        public void SetScenario(int value)
        {
            if (circle == null) return;
            foreach (var visual in visuals) { visual.SetActive(false); Destroy(visual); }
            visuals.Clear(); actors.Clear(); scenario = value; steps = 0; CompletedCount = 0;
            MinimumGap = float.PositiveInfinity; BlockedSteps = 0;
            if (scenario == 2)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4;
                    Vector2 start = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 3;
                    AddActor(start, -start, .22f, false, i % 2 == 0 ? Color.cyan : new Color(1, .55f, .1f));
                }
            }
            else
            {
                AddActor(new(-3, 0), new(3, 0), .3f, false, Color.cyan);
                AddActor(scenario == 1 ? Vector2.zero : new(3, 0), new(-3, 0), .3f,
                    scenario == 1, scenario == 1 ? Color.gray : new Color(1, .55f, .1f));
                if (scenario == 3)
                {
                    AddWall(new(0, .55f), new(8, .2f)); AddWall(new(0, -.55f), new(8, .2f));
                }
            }
            Physics2D.SyncTransforms();
        }

        /// <summary>创建导航圆形身体和目标标记，样例运动不依赖任何游戏组件。</summary>
        private void AddActor(Vector2 position, Vector2 goal, float radius, bool locked, Color color)
        {
            var view = Visual("Agent", position, Vector2.one * radius * 2, color, circle);
            var actor = new Actor { View = view.transform, Position = position, Goal = goal, Radius = radius, Locked = locked,
                Agent = new NavigationAgent2D(new GridSettings2D(Vector2.zero, .25f), new PathOptions2D(radius),
                    new AgentSettings2D(.08f, .1f, .15f, .25f, 3)) };
            if (!locked) { actor.Agent.SetDestination(goal); Visual("Goal", goal, Vector2.one * .13f, color, square); }
            actors.Add(actor);
        }

        /// <summary>创建可见墙体与同场景 Physics2D 障碍。</summary>
        private void AddWall(Vector2 position, Vector2 size)
        {
            var go = Visual("Wall", position, size, new Color(.45f, .45f, .5f), square);
            go.layer = 8; go.AddComponent<BoxCollider2D>();
        }

        /// <summary>创建属于样例所在场景的图形对象，避免多场景示例串域。</summary>
        private GameObject Visual(string label, Vector2 position, Vector2 scale, Color color, Sprite sprite)
        {
            var go = new GameObject(label); SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            go.transform.SetParent(transform); go.transform.position = position; go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            visuals.Add(go); return go;
        }

        /// <summary>同一步先采集完整快照，再求解并统一移动；地图限制后反馈真实执行速度。</summary>
        private void FixedUpdate()
        {
            if (source == null || paused) return;
            float dt = Time.fixedDeltaTime; snapshots.Clear();
            for (int i = 0; i < actors.Count; i++)
            {
                var a = actors[i]; a.Agent.Tick(a.Position, dt, source);
                bool locked = a.Locked || a.Agent.State == NavigationAgentState2D.Arrived;
                Vector2 preferred = locked ? Vector2.zero : a.Agent.DesiredDirection * Mathf.Min(2, a.Agent.RemainingWaypointDistance / dt);
                snapshots.Add(new AvoidanceAgent2D(i, 0, a.Position, a.Velocity, preferred, a.Radius, 2, locked));
            }
            world.Solve(snapshots, dt, results); CompletedCount = 0;
            for (int i = 0; i < actors.Count; i++)
            {
                var a = actors[i];
                Vector2 velocity = snapshots[i].Locked || results[i].Infeasible || results[i].TruncatedNeighbors ? Vector2.zero : results[i].Velocity;
                velocity = Vector2.ClampMagnitude(velocity, a.Agent.RemainingWaypointDistance / dt);
                if (!source.IsSegmentClear(a.Position, a.Position + velocity * dt, a.Radius)) { velocity = Vector2.zero; BlockedSteps++; }
                a.Velocity = velocity; a.Position += velocity * dt; a.View.position = a.Position;
                if (!a.Locked && Vector2.Distance(a.Position, a.Goal) <= .09f) CompletedCount++;
            }
            for (int i = 0; i < actors.Count; i++) for (int j = i + 1; j < actors.Count; j++)
                MinimumGap = Mathf.Min(MinimumGap, Vector2.Distance(actors[i].Position, actors[j].Position) - actors[i].Radius - actors[j].Radius);
            if (smoke && ++steps >= 500)
            {
                bool pass = CompletedCount == 2 && MinimumGap >= -.005f;
                Debug.Log(pass ? "NAVIGATION_CROWD_SMOKE_PASS" : "NAVIGATION_CROWD_SMOKE_FAIL"); Application.Quit(pass ? 0 : 1);
            }
        }

        /// <summary>在构建程序中也显示模式、圆间距和可重复操作，不依赖编辑器 Gizmo。</summary>
        private void OnGUI()
        {
            GUI.Label(new Rect(15, 15, 1000, 30), $"Crowd Navigation2D | scenario {scenario} | arrived {CompletedCount} | min gap {MinimumGap:F3} | blocked steps {BlockedSteps}");
            string[] labels = { "Head-on", "Stopped actor", "8-way crossing", "Narrow corridor" };
            for (int i = 0; i < labels.Length; i++) if (GUI.Button(new Rect(15 + i * 155, 50, 145, 32), labels[i])) SetScenario(i);
            if (GUI.Button(new Rect(15, 90, 145, 32), "Pause / Resume")) paused = !paused;
            GUI.Label(new Rect(15, 130, 1000, 30), "Circles show occupied radius. Narrow corridors may wait: no forced escape or combat slots.");
        }

        /// <summary>释放运行时生成的纹理和精灵。</summary>
        private void OnDestroy() { if (circle != null) Destroy(circle); if (square != null) Destroy(square); if (circleTexture != null) Destroy(circleTexture); }
    }
}
