using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>验证 Lab 中的示例镜像并只构建已有场景，不改写源资产。</summary>
public static class HealthLabBuild
{
    private const string ScenePath = "Assets/BasicHealthDemo/BasicHealthDemo.unity";
    private static readonly string[] MirrorFiles =
    {
        "BasicHealthDemo.cs", "BasicHealthDemo.cs.meta",
        "BasicHealthDemo.unity", "BasicHealthDemo.unity.meta",
        "Computerzhuxi.Health.Samples.asmdef", "Computerzhuxi.Health.Samples.asmdef.meta",
        "HealthDemo.prefab", "HealthDemo.prefab.meta"
    };

    /// <summary>比较 Sample 和 Lab 的脚本、资产与 GUID，阻止镜像在后续维护中漂移。</summary>
    public static void ValidateSampleMirror()
    {
        string labRoot = Path.GetDirectoryName(Application.dataPath);
        string labSample = Path.Combine(labRoot, "Assets/BasicHealthDemo");
        string packageSample = Path.GetFullPath(Path.Combine(labRoot,
            "../../Packages/com.computerzhuxi.health/Samples~/BasicHealthDemo"));
        foreach (string name in MirrorFiles)
        {
            string source = Path.Combine(packageSample, name);
            string mirror = Path.Combine(labSample, name);
            if (!File.Exists(source) || !File.Exists(mirror))
                throw new FileNotFoundException("Health Sample 镜像缺少文件：" + name);
            // Unity 可能只调整换行符；其余内容包括序列化引用与 GUID 必须相同。
            if (File.ReadAllText(source).Replace("\r\n", "\n") !=
                File.ReadAllText(mirror).Replace("\r\n", "\n"))
                throw new InvalidOperationException("Health Sample 镜像与包内容不同：" + name);
        }
        Debug.Log("HEALTH_SAMPLE_MIRROR_PASS");
    }

    /// <summary>使用已跟踪场景构建独立验证程序，不重新生成场景、Prefab 或构建设置。</summary>
    public static void Build()
    {
        ValidateSampleMirror();
        string labRoot = Path.GetDirectoryName(Application.dataPath);
        if (!File.Exists(Path.Combine(labRoot, ScenePath)))
            throw new FileNotFoundException("HealthLab 场景不存在。", ScenePath);
        string buildFolder = Path.GetFullPath(Path.Combine(labRoot, "../../Artifacts/Build"));
        Directory.CreateDirectory(buildFolder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = Path.Combine(buildFolder, "HealthLab.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("HealthLab 构建失败。");
        Debug.Log("HEALTHLAB_BUILD_PASS");
    }
}
