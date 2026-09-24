using UnityEditor;
using UnityEngine;

namespace Computerzhuxi.Navigation2D.Editor
{
    /// <summary>在 Inspector 显示实时配置错误和导航只读状态，命令按钮调用正式公共入口。</summary>
    [CustomEditor(typeof(NavigationNavigator2D)), CanEditMultipleObjects]
    public sealed class NavigationInspector2D : UnityEditor.Editor
    {
        /// <summary>保留标准序列化编辑和 Undo，运行态追加只读诊断与命令。</summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("只计算路径，不写入位置。请由项目运动系统读取建议；QuickStart 中的运动适配器仅供示例。所有配置运行时修改均在下个固定帧生效。", MessageType.Info);
            if (!Application.isPlaying || targets.Length != 1) return;
            var navigator = (NavigationNavigator2D)target;
            EditorGUILayout.LabelField("导航状态", navigator.State.ToString());
            EditorGUILayout.LabelField("查询结果", navigator.LastResult?.Status.ToString() ?? "尚未查询");
            EditorGUILayout.LabelField("身体净空半径", navigator.EffectiveRadius.ToString("F3"));
            EditorGUILayout.LabelField("剩余路径点", (navigator.CurrentPath.Count - navigator.CurrentPathIndex).ToString());
            if (navigator.ConfigurationError != null) EditorGUILayout.HelpBox(navigator.ConfigurationError, MessageType.Error);
            if (GUILayout.Button(navigator.IsPaused ? "恢复" : "暂停")) { if (navigator.IsPaused) navigator.Resume(); else navigator.Pause(); }
            if (GUILayout.Button("停止并清除目标")) navigator.Stop();
            Repaint();
        }
    }
}
