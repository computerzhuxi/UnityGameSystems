using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>只读取 Lab 已提交的演示场景并完成独立 Windows 构建。</summary>
public static class PerceptionLabBuild
{
    /// <summary>构建独立演示，不修改使用者的既有场景。</summary>
    public static void Build()
    {
        const string scenePath = "Assets/PerceptionDemo.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            throw new FileNotFoundException("Perception Lab scene is missing.", scenePath);
        string outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Artifacts/Perception2D/Build"));
        Directory.CreateDirectory(outputDirectory);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{scenePath},locationPathName=Path.Combine(outputDirectory,"Perception2DLab.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
        if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Perception Lab build failed");
        Debug.Log("PERCEPTION_LAB_BUILD_PASS");
    }
}
