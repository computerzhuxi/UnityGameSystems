using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Computerzhuxi.Navigation2D.Samples;

/// <summary>维护可独立打开的导航 Lab 场景并构建验证程序。</summary>
public static class NavigationLabBuild
{
    /// <summary>首次建立场景；存在时直接使用，不覆盖用户编辑。</summary>
    public static void Prepare()
    {
        const string path="Assets/NavigationLab.unity";
        if(!File.Exists(path))
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Navigation Demo").AddComponent<NavigationDemo>();
            var camera=new GameObject("Camera").AddComponent<Camera>(); camera.transform.position=new(0,0,-10); camera.orthographic=true; camera.orthographicSize=5; camera.backgroundColor=new(.08f,.09f,.12f); camera.clearFlags=CameraClearFlags.SolidColor;
            EditorSceneManager.SaveScene(scene,path);
        }
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
    }
    /// <summary>构建场景并严格检查构建结果，产物放入被忽略的日志目录。</summary>
    public static void Build()
    {
        Prepare();
        string output=Path.GetFullPath("../../Artifacts/Logs/Navigation2D/Build/Navigation2DLab.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{ scenes=new[]{"Assets/NavigationLab.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
        if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Navigation2D build failed");
        Debug.Log("NAVIGATION_BUILD_PASS");
    }
}
