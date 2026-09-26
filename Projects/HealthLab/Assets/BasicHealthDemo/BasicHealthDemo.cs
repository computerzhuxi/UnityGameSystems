using System;
using UnityEngine;

namespace Computerzhuxi.Health.Samples
{
    /// <summary>以最小界面展示全部生命命令及真实 UnityEvent 回调。</summary>
    public sealed class BasicHealthDemo : MonoBehaviour
    {
        [SerializeField] private HealthComponent target;
        private string maximum="15";
        private int unityEvents;
        private string lastEvent="Waiting";
        private Health standalone;
        private int coreEvents;
        private string lastCoreEvent="Core waiting";
        /// <summary>创建一个由示例代码独立持有的核心实例，演示直接组合入口。</summary>
        private void Awake()
        {
            standalone = new Health(6,8);
            standalone.Changed += RecordCoreEvent;
        }
        /// <summary>解除示例自己建立的核心订阅。</summary>
        private void OnDestroy()
        {
            if (standalone != null) standalone.Changed -= RecordCoreEvent;
        }
        /// <summary>记录独立核心的强类型状态事件。</summary>
        private void RecordCoreEvent(HealthChange change)
        {
            coreEvents++;
            lastCoreEvent="Core #"+coreEvents+" "+change.Reason+" "+change.After.Current+"/"+change.After.Maximum;
        }
        /// <summary>记录 Inspector 持久化绑定的实际调用。</summary>
        public void RecordUnityEvent() { unityEvents++; lastEvent="UnityEvent #"+unityEvents; }
        /// <summary>构建运行时自动执行完整生命操作并输出可检测的成功标记。</summary>
        private void Start()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--health-smoke")<0) return;
            target.Damage(3); target.Heal(1); target.Kill(); target.Revive(4);
            target.ChangeMaximum(20,MaximumHealthPolicy.Refill);
            target.Restore(6,12);
            standalone.Damage(2); standalone.Restore(6,8);
            bool passed=target.State.Current==6 && target.State.Maximum==12 && unityEvents==6
                && standalone.Current==6 && standalone.Maximum==8 && coreEvents==2;
            Debug.Log(passed ? "HEALTH_SMOKE_PASS" : "HEALTH_SMOKE_FAIL");
            Application.Quit(passed?0:1);
        }
        /// <summary>绘制数值、生死状态与命令按钮，界面仅属于示例。</summary>
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(30,30,420,720));
            GUILayout.Label("Independent Health System");
            GUILayout.Label($"Health: {target.State.Current} / {target.State.Maximum}   {(target.State.IsAlive ? "Alive" : "Dead")}");
            if(GUILayout.Button("Damage 3")) target.Damage(3);
            if(GUILayout.Button("Heal 3")) target.Heal(3);
            if(GUILayout.Button("Kill")) target.Kill();
            if(GUILayout.Button("Revive full")) target.Revive(target.State.Maximum);
            maximum=GUILayout.TextField(maximum);
            if(int.TryParse(maximum,out int value) && value>0)
            {
                if(GUILayout.Button("Change Maximum / Preserve")) target.ChangeMaximum(value,MaximumHealthPolicy.PreserveCurrent);
                if(GUILayout.Button("Change Maximum / Refill")) target.ChangeMaximum(value,MaximumHealthPolicy.Refill);
            }
            if(GUILayout.Button("Restore 5 / 10")) target.Restore(5,10);
            GUILayout.Label(lastEvent);
            GUILayout.Space(12);
            GUILayout.Label($"Programmatic Core: {standalone.Current} / {standalone.Maximum}");
            if(GUILayout.Button("Core Damage 2")) standalone.Damage(2);
            if(GUILayout.Button("Core Restore 6 / 8")) standalone.Restore(6,8);
            GUILayout.Label(lastCoreEvent);
            GUILayout.EndArea();
        }
    }
}
