using System;
using System.IO;
using Computerzhuxi.Stats;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>建立独立 Lab 的实际定义资产、Prefab 和场景，并提供候选构建入口。</summary>
public static class StatsLabBuild
{
    private const string DemoRoot = "Assets/BasicStatsDemo";
    private const string ScenePath = DemoRoot + "/BasicStatsDemo.unity";

    /// <summary>生成完整双入口示例资源，可在批处理或编辑器菜单运行。</summary>
    [MenuItem("StatsLab/Create Demo Assets")]
    public static void CreateDemoAssets()
    {
        EnsureFolder(DemoRoot);
        EnsureFolder(DemoRoot + "/Definitions");
        StatDefinitionAsset health = CreateDefinition("MaxHealth", "maxHealth", 1, true, StatRounding.Nearest);
        StatDefinitionAsset attack = CreateDefinition("Attack", "attack", 0, true, StatRounding.Nearest);
        StatDefinitionAsset speed = CreateDefinition("MoveSpeed", "moveSpeed", 0, true, StatRounding.None);

        var prefabRoot = new GameObject("ComponentRole");
        SampleCharacterStats domain = prefabRoot.AddComponent<SampleCharacterStats>();
        StatCollectionComponent collection = prefabRoot.AddComponent<StatCollectionComponent>();
        var serialized = new SerializedObject(collection);
        serialized.FindProperty("bindingProvider").objectReferenceValue = domain;
        SerializedProperty definitions = serialized.FindProperty("definitions");
        definitions.arraySize = 3;
        definitions.GetArrayElementAtIndex(0).objectReferenceValue = health;
        definitions.GetArrayElementAtIndex(1).objectReferenceValue = attack;
        definitions.GetArrayElementAtIndex(2).objectReferenceValue = speed;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, DemoRoot + "/ComponentRole.prefab");
        UnityEngine.Object.DestroyImmediate(prefabRoot);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var controllerObject = new GameObject("DemoController");
        BasicStatsDemo controller = controllerObject.AddComponent<BasicStatsDemo>();
        var controllerSerialized = new SerializedObject(controller);
        controllerSerialized.FindProperty("componentRole").objectReferenceValue = instance.GetComponent<StatCollectionComponent>();
        controllerSerialized.FindProperty("componentDomain").objectReferenceValue = instance.GetComponent<SampleCharacterStats>();
        controllerSerialized.ApplyModifiedPropertiesWithoutUndo();
        var cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.09f, 0.12f, 0.16f);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("STATSLAB_ASSETS_PASS");
    }

    /// <summary>构建独立 Lab 的 Standalone 播放器，供发布候选冒烟使用。</summary>
    public static void Build()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string repoRoot = Directory.GetParent(Directory.GetParent(projectRoot).FullName).FullName;
        string output = Path.Combine(repoRoot, "Artifacts", "Build", "StatsLab.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new[] { ScenePath }, output,
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("StatsLab 构建失败：" + report.summary.result);
    }

    /// <summary>在 Unity 资产数据库中按层级建立缺少的文件夹。</summary>
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    /// <summary>创建或更新样例属性定义资产。</summary>
    private static StatDefinitionAsset CreateDefinition(string fileName, string id,
        double minimum, bool useMinimum, StatRounding rounding)
    {
        string path = DemoRoot + "/Definitions/" + fileName + ".asset";
        StatDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<StatDefinitionAsset>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<StatDefinitionAsset>();
            AssetDatabase.CreateAsset(asset, path);
        }
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("statId").stringValue = id;
        serialized.FindProperty("useMinimum").boolValue = useMinimum;
        serialized.FindProperty("minimum").doubleValue = minimum;
        serialized.FindProperty("useMaximum").boolValue = false;
        serialized.FindProperty("rounding").enumValueIndex = (int)rounding;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }
}
