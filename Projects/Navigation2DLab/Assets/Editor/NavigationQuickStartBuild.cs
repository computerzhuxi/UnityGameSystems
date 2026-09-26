using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Computerzhuxi.Navigation2D;
using Computerzhuxi.Navigation2D.Samples;

/// <summary>创建由导航组件与示例适配器序列化装配的快速使用场景，并独立构建。</summary>
public static class NavigationQuickStartBuild
{
    private const string ScenePath = "Assets/Samples/QuickStartNavigation2D/QuickStartNavigation2D.unity";
    /// <summary>仅首次生成真实场景；之后只使用正式 Sample 的镜像，不覆盖人工编辑。</summary>
    public static void Prepare()
    {
        if (File.Exists(ScenePath)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var actor = new GameObject("Navigator (select to edit configuration)"); actor.transform.position = new(-3, 0, 0);
        var body = actor.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        var shape = actor.AddComponent<CircleCollider2D>(); shape.radius = .2f;
        var target = new GameObject("Target"); target.transform.position = new(3, 0, 0);
        var wall = new GameObject("Obstacle (layer 8)"); wall.layer = 8;
        wall.AddComponent<BoxCollider2D>().size = new(.35f, 3);
        var navigator = actor.AddComponent<NavigationNavigator2D>();
        var data = new SerializedObject(navigator);
        data.FindProperty("bodyCollider").objectReferenceValue = shape;
        data.FindProperty("target").objectReferenceValue = target.transform;
        data.FindProperty("obstacleMask").intValue = 1 << 8;
        data.FindProperty("cellSize").floatValue = .25f;
        data.ApplyModifiedPropertiesWithoutUndo();
        actor.AddComponent<QuickStartExampleMover2D>();
        var controls = new GameObject("Optional display and buttons").AddComponent<QuickStartControls>();
        data = new SerializedObject(controls); data.FindProperty("navigator").objectReferenceValue = navigator;
        data.FindProperty("target").objectReferenceValue = target.transform; data.FindProperty("wall").objectReferenceValue = wall;
        data.ApplyModifiedPropertiesWithoutUndo();
        var camera = new GameObject("Camera").AddComponent<Camera>(); camera.transform.position = new(0, 0, -10);
        camera.orthographic = true; camera.orthographicSize = 5; camera.backgroundColor = new(.08f, .09f, .12f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.Refresh();
        string source = Path.GetFullPath("../../Packages/com.computerzhuxi.navigation2d/Samples~/QuickStartNavigation2D/QuickStartNavigation2D.unity");
        if (File.Exists(source)) throw new InvalidOperationException("正式 Sample 场景已存在，禁止覆盖。");
        File.Copy(ScenePath, source); File.Copy(ScenePath + ".meta", source + ".meta");
        Debug.Log("QUICKSTART_SERIALIZED_SCENE_CREATED");
    }
    /// <summary>构建真实 Inspector 配置场景，不在运行时生成导航或示例运动组件。</summary>
    public static void Build()
    {
        if (!File.Exists(ScenePath)) throw new FileNotFoundException("QuickStart 场景不存在，请先显式生成并审查资产。", ScenePath);
        string output = Path.GetFullPath("../../Artifacts/Logs/Navigation2D/Build/NavigationQuickStartLab.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Quick start build failed");
        Debug.Log("NAVIGATION_QUICKSTART_BUILD_PASS");
    }
    /// <summary>通过 Unity 加载保留 GUID 的示例脚本迁移，并把重新保存的真实场景同步到正式 Sample。</summary>
    public static void MigrateExampleMover()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0) throw new InvalidOperationException("Missing script after sample migration");
            count += root.GetComponentsInChildren<QuickStartExampleMover2D>(true).Length;
        }
        if (count != 1) throw new InvalidOperationException("Expected exactly one sample mover");
        EditorSceneManager.SaveScene(scene, ScenePath);
        string source = Path.GetFullPath("../../Packages/com.computerzhuxi.navigation2d/Samples~/QuickStartNavigation2D/QuickStartNavigation2D.unity");
        File.Copy(ScenePath, source, true);
        Debug.Log("QUICKSTART_EXAMPLE_MIGRATION_PASS");
    }
}
