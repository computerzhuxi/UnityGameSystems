using System;
using UnityEngine;

namespace Computerzhuxi.Health.Samples
{
    /// <summary>以最小界面展示全部生命命令及真实 UnityEvent 回调。</summary>
    public sealed class BasicHealthDemo : MonoBehaviour
    {
        [SerializeField] private HealthComponent target;
        private string maximum = "15";
        private int unityEvents;
        private string lastEvent = "Waiting";
        private Health standalone;
        private int coreEvents;
        private string lastCoreEvent = "Core waiting";

        /// <summary>创建一个由示例代码独立持有的核心实例，演示直接组合入口。</summary>
        private void Awake()
        {
            // 两种接入分别使用自己的对象，避免同一角色拥有两份生命状态。
            standalone = new Health(6, 8);
            standalone.Changed += RecordCoreEvent;
        }

        /// <summary>解除示例自己建立的核心订阅。</summary>
        private void OnDestroy()
        {
            if (standalone != null)
                standalone.Changed -= RecordCoreEvent;
        }

        /// <summary>记录独立核心的强类型状态事件。</summary>
        private void RecordCoreEvent(HealthChange change)
        {
            coreEvents++;
            lastCoreEvent = "Core #" + coreEvents + " " + change.Reason + " "
                + change.After.Current + "/" + change.After.Maximum;
        }

        /// <summary>记录 Inspector 持久化绑定的实际调用。</summary>
        public void RecordUnityEvent()
        {
            unityEvents++;
            lastEvent = "UnityEvent #" + unityEvents;
        }

        /// <summary>仅在指定启动参数存在时执行构建冒烟。</summary>
        private void Start()
        {
            // 普通演示等待用户操作；自动验证由独立程序的启动参数触发。
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--health-smoke") < 0)
                return;
            RunSmokeCheck();
        }

        /// <summary>验证两种接入的命令链与事件计数，并退出独立程序。</summary>
        private void RunSmokeCheck()
        {
            target.Damage(3);
            target.Heal(1);
            target.Kill();
            target.Revive(4);
            target.ChangeMaximum(20, MaximumHealthPolicy.Refill);
            target.Restore(6, 12);
            standalone.Damage(2);
            standalone.Restore(6, 8);

            // 组件事件计数同时验证场景中的持久化 UnityEvent 接线。
            bool passed = target.State.Current == 6 && target.State.Maximum == 12
                && unityEvents == 6 && standalone.Current == 6 && standalone.Maximum == 8
                && coreEvents == 2;
            Debug.Log(passed ? "HEALTH_SMOKE_PASS" : "HEALTH_SMOKE_FAIL");
            Application.Quit(passed ? 0 : 1);
        }

        /// <summary>绘制两种接入的演示区域，界面仅属于示例。</summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(30, 30, 420, 720));
            GUILayout.Label("Independent Health System");
            DrawComponentDemo();
            GUILayout.Space(12);
            DrawCoreDemo();
            GUILayout.EndArea();
        }

        /// <summary>通过组件入口绘制状态并提交用户选择的生命命令。</summary>
        private void DrawComponentDemo()
        {
            GUILayout.Label($"Health: {target.State.Current} / {target.State.Maximum}   {(target.State.IsAlive ? "Alive" : "Dead")}");
            if (GUILayout.Button("Damage 3"))
                target.Damage(3);
            if (GUILayout.Button("Heal 3"))
                target.Heal(3);
            if (GUILayout.Button("Kill"))
                target.Kill();
            if (GUILayout.Button("Revive full"))
                target.Revive(target.State.Maximum);

            maximum = GUILayout.TextField(maximum);
            // 只为合法正整数显示上限操作，避免演示输入触发参数异常。
            if (int.TryParse(maximum, out int value) && value > 0)
            {
                if (GUILayout.Button("Change Maximum / Preserve"))
                    target.ChangeMaximum(value, MaximumHealthPolicy.PreserveCurrent);
                if (GUILayout.Button("Change Maximum / Refill"))
                    target.ChangeMaximum(value, MaximumHealthPolicy.Refill);
            }
            if (GUILayout.Button("Restore 5 / 10"))
                target.Restore(5, 10);
            GUILayout.Label(lastEvent);
        }

        /// <summary>通过独立核心入口绘制状态、命令与强类型事件结果。</summary>
        private void DrawCoreDemo()
        {
            GUILayout.Label($"Programmatic Core: {standalone.Current} / {standalone.Maximum}");
            if (GUILayout.Button("Core Damage 2"))
                standalone.Damage(2);
            if (GUILayout.Button("Core Restore 6 / 8"))
                standalone.Restore(6, 8);
            GUILayout.Label(lastCoreEvent);
        }
    }
}
