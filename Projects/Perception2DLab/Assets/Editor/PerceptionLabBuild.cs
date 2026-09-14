using System;
using System.IO;
using Computerzhuxi.Perception2D.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>生成 Lab 专属示例场景并完成独立 Windows 构建。</summary>
public static class PerceptionLabBuild
{
    /// <summary>构建独立演示，不修改使用者的既有场景。</summary>
    public static void Build()
    {
        const string scenePath = "Assets/PerceptionDemo.unity";
        if (!File.Exists(scenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Perception Demo").AddComponent<BasicPerceptionDemo>();
            var camera = new GameObject("Camera").AddComponent<Camera>(); camera.transform.position = new Vector3(0,0,-10); camera.orthographic = true; camera.orthographicSize = 6;
            EditorSceneManager.SaveScene(scene,scenePath);
        }
        EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene(scenePath,true)};
        Directory.CreateDirectory("../../Artifacts/Perception2D/Build");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{scenePath},locationPathName="../../Artifacts/Perception2D/Build/Perception2DLab.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
        if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Perception Lab build failed");
        Debug.Log("PERCEPTION_LAB_BUILD_PASS");
    }
}
