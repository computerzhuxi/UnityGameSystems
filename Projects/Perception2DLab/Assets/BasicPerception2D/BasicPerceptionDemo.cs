using System;
using System.Collections;
using UnityEngine;

namespace Computerzhuxi.Perception2D.Samples
{
    /// <summary>运行时建立无项目依赖的演示，可切换感官并模拟有来源及无来源声音。</summary>
    public sealed class BasicPerceptionDemo : MonoBehaviour
    {
        private PerceptionWorld2D world;
        private PerceptionObserver2D observer;
        private PerceptionTarget2D target;
        private bool sight = true, hearing = true;
        private string latest = "Ready";
        /// <summary>建立明确绑定的观察者、目标与遮挡物。</summary>
        private void Start()
        {
            var host = new GameObject("Demo World"); host.transform.SetParent(transform); world = host.AddComponent<PerceptionWorld2D>();
            var eye = CreateBox("Observer", new Vector2(-3, 0), Color.cyan); eye.SetActive(false);
            observer = eye.AddComponent<PerceptionObserver2D>(); observer.Configure(world, new PerceptionSettings2D { ViewAngle = 100, ObstacleLayers = 1 << 8 }); eye.SetActive(true); observer.SetFacingDirection(Vector2.right);
            var actor = CreateBox("Target", new Vector2(1, 0), Color.yellow); actor.AddComponent<BoxCollider2D>(); target = actor.AddComponent<PerceptionTarget2D>(); target.Bind(world);
            var wall = CreateBox("Wall", new Vector2(0, 2), Color.gray); wall.layer = 8; wall.transform.localScale = new Vector3(0.3f, 2, 1); wall.AddComponent<BoxCollider2D>();
            observer.SenseUpdated += OnSense;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--perception-smoke") >= 0) StartCoroutine(Smoke());
        }
        /// <summary>在独立程序中验证真实视觉、声音、关闭听觉和记忆查询并返回退出码。</summary>
        private IEnumerator Smoke()
        {
            yield return new WaitForSeconds(0.2f);
            if (!observer.TryGetObservation(target,out var info) || !info.IsVisible) { Debug.LogError("PERCEPTION_SMOKE_SIGHT_FAIL"); Application.Quit(1); yield break; }
            world.ReportNoise(new NoiseEvent2D(target.transform.position,source:target)); yield return null;
            if (!observer.TryGetObservation(target,out info) || !info.Hearing.HasValue) { Debug.LogError("PERCEPTION_SMOKE_HEARING_FAIL"); Application.Quit(1); yield break; }
            observer.SetSenseEnabled(PerceptionSense.Hearing,false);
            if (world.ReportNoise(new NoiseEvent2D(Vector2.zero)) != 0) { Debug.LogError("PERCEPTION_SMOKE_GATE_FAIL"); Application.Quit(1); yield break; }
            Debug.Log("PERCEPTION_SMOKE_PASS"); Application.Quit(0);
        }
        /// <summary>将最近通知显示在演示面板上。</summary>
        private void OnSense(PerceptionChange change) => latest = change.Sense + " / " + change.Reason;
        /// <summary>创建简单可见方块，纹理由此示例自行持有。</summary>
        private GameObject CreateBox(string label, Vector2 position, Color color)
        {
            var go = new GameObject(label); go.transform.SetParent(transform); go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>(); var texture = Texture2D.whiteTexture;
            renderer.sprite = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), new Vector2(0.5f,0.5f), texture.width); renderer.color = color; return go;
        }
        /// <summary>用按钮验证纯视觉、纯听觉、遮挡、丢失与声音位置。</summary>
        private void OnGUI()
        {
            if (observer == null) return;
            GUILayout.BeginArea(new Rect(15,15,430,320), GUI.skin.box);
            GUILayout.Label("Perception 2D — Sight / Hearing / Memory");
            if (GUILayout.Button("Sight: " + sight)) { sight = !sight; observer.SetSenseEnabled(PerceptionSense.Sight,sight); }
            if (GUILayout.Button("Hearing: " + hearing)) { hearing = !hearing; observer.SetSenseEnabled(PerceptionSense.Hearing,hearing); }
            if (GUILayout.Button("Move target behind wall / restore")) target.transform.position = target.transform.position.y == 0 ? new Vector3(2,3,0) : new Vector3(1,0,0);
            if (GUILayout.Button("Report target footstep")) world.ReportNoise(new NoiseEvent2D(target.transform.position,source:target,tag:"Footstep"));
            if (GUILayout.Button("Report anonymous sound")) world.ReportNoise(new NoiseEvent2D(new Vector2(-2,2),tag:"Unknown"));
            if (GUILayout.Button("Clear memory")) observer.ClearMemory();
            GUILayout.Label(latest);
            foreach (var item in observer.Observations) GUILayout.Label(item.Target.name + " visible=" + item.IsVisible + " hearing=" + item.Hearing.HasValue);
            GUILayout.Label("Anonymous memories: " + observer.HeardEvents.Count);
            GUILayout.EndArea();
        }
        /// <summary>释放示例创建的 Sprite，避免重复装卸时残留原生对象。</summary>
        private void OnDestroy()
        { foreach (var renderer in GetComponentsInChildren<SpriteRenderer>()) if (renderer.sprite != null) Destroy(renderer.sprite); }
    }
}
