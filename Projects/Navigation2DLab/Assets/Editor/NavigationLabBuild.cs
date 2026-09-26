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
    }
    /// <summary>创建独立 Agent 场景；既有直接查询场景和用户编辑保持原样。</summary>
    public static void PrepareAgent()
    {
        const string path = "Assets/NavigationAgentLab.unity";
        if (!File.Exists(path))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Navigation Agent Demo").AddComponent<AgentNavigationDemo>();
            var camera = new GameObject("Camera").AddComponent<Camera>(); camera.transform.position = new(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 5; camera.backgroundColor = new(.08f, .09f, .12f);
            camera.clearFlags = CameraClearFlags.SolidColor; EditorSceneManager.SaveScene(scene, path);
        }
    }
    /// <summary>单独构建 Agent 使用路线，防止两个样例运动执行器混在同一场景。</summary>
    public static void BuildAgent()
    {
        RequireScene("Assets/NavigationAgentLab.unity");
        string output = Path.GetFullPath("../../Artifacts/Logs/Navigation2D/Build/NavigationAgentLab.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/NavigationAgentLab.unity" }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Agent build failed");
        Debug.Log("NAVIGATION_AGENT_BUILD_PASS");
    }
    /// <summary>首次创建独立群体场景，不覆盖已经存在的用户场景编辑。</summary>
    public static void PrepareCrowd()
    {
        const string path = "Assets/NavigationCrowdLab.unity";
        if (File.Exists(path)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Navigation Crowd Demo").AddComponent<CrowdNavigationDemo>();
        var camera = new GameObject("Camera").AddComponent<Camera>(); camera.transform.position = new(0, 0, -10);
        camera.orthographic = true; camera.orthographicSize = 5; camera.backgroundColor = new(.08f, .09f, .12f);
        camera.clearFlags = CameraClearFlags.SolidColor; EditorSceneManager.SaveScene(scene, path);
    }
    /// <summary>构建具有运行时可见圆形身体和操作按钮的群体验收程序。</summary>
    public static void BuildCrowd()
    {
        RequireScene("Assets/NavigationCrowdLab.unity");
        string output = Path.GetFullPath("../../Artifacts/Logs/Navigation2D/Build/NavigationCrowdLab.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/NavigationCrowdLab.unity" }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Crowd build failed");
        Debug.Log("NAVIGATION_CROWD_BUILD_PASS");
    }
    /// <summary>构建场景并严格检查构建结果，产物放入被忽略的日志目录。</summary>
    public static void Build()
    {
        RequireScene("Assets/NavigationLab.unity");
        string output=Path.GetFullPath("../../Artifacts/Logs/Navigation2D/Build/Navigation2DLab.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{ scenes=new[]{"Assets/NavigationLab.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development });
        if(report.summary.result!=BuildResult.Succeeded) throw new InvalidOperationException("Navigation2D build failed");
        Debug.Log("NAVIGATION_BUILD_PASS");
    }
    /// <summary>构建只消费已跟踪场景；首次生成必须由维护者显式调用对应 Prepare 方法。</summary>
    private static void RequireScene(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("导航 Lab 场景不存在，请先显式生成并审查资产。", path);
    }
}
