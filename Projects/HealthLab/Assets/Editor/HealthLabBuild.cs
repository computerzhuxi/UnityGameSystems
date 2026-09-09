using System.IO;
using Computerzhuxi.Health;
using Computerzhuxi.Health.Samples;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>创建真实示例场景、持久化事件绑定并构建独立验证程序。</summary>
public static class HealthLabBuild
{
    /// <summary>通过 Unity API 生成可运行场景并验证 Standalone 构建。</summary>
    public static void Build()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var host=new GameObject("Health Demo"); var health=host.AddComponent<HealthComponent>(); var demo=host.AddComponent<BasicHealthDemo>();
        var data=new SerializedObject(demo); data.FindProperty("target").objectReferenceValue=health; data.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(health.OnChanged,demo.RecordUnityEvent);
        var camera=new GameObject("Camera").AddComponent<Camera>(); camera.transform.position=new Vector3(0,0,-10);
        Directory.CreateDirectory("Assets/BasicHealthDemo");
        EditorSceneManager.SaveScene(scene,"Assets/BasicHealthDemo/BasicHealthDemo.unity");
        PrefabUtility.SaveAsPrefabAsset(host,"Assets/BasicHealthDemo/HealthDemo.prefab");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/BasicHealthDemo/BasicHealthDemo.unity",true)};
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("../../Artifacts/Build");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/BasicHealthDemo/BasicHealthDemo.unity"},locationPathName="../../Artifacts/Build/HealthLab.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        if(report.summary.result!=BuildResult.Succeeded) throw new System.InvalidOperationException("HealthLab 构建失败。");
        Debug.Log("HEALTHLAB_BUILD_PASS");
    }
}
