using System;
using UnityEngine;

namespace Computerzhuxi.Navigation2D.Samples
{
    /// <summary>只提供演示按钮、运行时形状和验收读数；不计算路径、不 Tick、不移动角色。</summary>
    public sealed class QuickStartControls : MonoBehaviour
    {
        [SerializeField] private NavigationNavigator2D navigator;
        [SerializeField] private Transform target;
        [SerializeField] private GameObject wall;
        private Texture2D texture;
        private Sprite sprite;
        private int frames;
        private bool smoke;
        /// <summary>创建显示用形状，实际导航/身体/目标引用均来自场景序列化。</summary>
        private void Start()
        {
            smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "--quickstart-smoke") >= 0;
            texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.white); texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            Visual(navigator.gameObject, new Vector2(.4f, .4f), new Color(.2f, .85f, 1));
            Visual(target.gameObject, new Vector2(.25f, .25f), Color.green);
            Visual(wall, new Vector2(.35f, 3), new Color(.7f, .45f, .3f));
        }
        /// <summary>创建无碰撞体的显示子物体，不改变身体净空或移动执行。</summary>
        private void Visual(GameObject owner, Vector2 size, Color color)
        {
            var visual = new GameObject("Visual"); visual.transform.SetParent(owner.transform, false);
            visual.transform.localScale = size;
            var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
        }
        /// <summary>仅检查正式组件自动运行结果，冒烟不替代人工视觉验收。</summary>
        private void FixedUpdate()
        {
            if (!smoke) return;
            frames++;
            if (frames < 400) return;
            bool passed = navigator.State == NavigationAgentState2D.Arrived && navigator.ConfigurationError == null;
            Debug.Log(passed ? "NAVIGATION_QUICKSTART_SMOKE_PASS" : "NAVIGATION_QUICKSTART_SMOKE_FAIL");
            Application.Quit(passed ? 0 : 2);
        }
        /// <summary>演示命令按钮只调用公共接口；修改目标物体的位置由导航自动发现。</summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 580, 230), GUI.skin.box);
            GUILayout.Label("Navigation 2D: Inspector Quick Start");
            GUILayout.Label("Blue: body | Green: target | Brown: obstacle");
            GUILayout.Label("State: " + navigator.State + " | Result: " + navigator.LastResult?.Status);
            if (GUILayout.Button(navigator.IsPaused ? "Resume" : "Pause")) { if (navigator.IsPaused) navigator.Resume(); else navigator.Pause(); }
            if (GUILayout.Button("Stop (clears target)")) navigator.Stop();
            if (GUILayout.Button("Follow target again")) navigator.SetTarget(target);
            if (GUILayout.Button("Move target")) target.position = new Vector3(3, target.position.y > 0 ? -2 : 2, 0);
            if (GUILayout.Button("Toggle obstacle")) wall.SetActive(!wall.activeSelf);
            GUILayout.EndArea();
        }
        /// <summary>释放本演示独占的显示资源，不销毁导航和身体资产。</summary>
        private void OnDestroy() { if (sprite != null) Destroy(sprite); if (texture != null) Destroy(texture); }
    }
}
