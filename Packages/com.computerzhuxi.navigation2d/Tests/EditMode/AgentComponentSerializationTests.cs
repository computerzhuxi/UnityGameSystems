using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Computerzhuxi.Navigation2D;

/// <summary>验证便利组件真实 Prefab 持久化，运行期路径不参与序列化。</summary>
public sealed class AgentComponentSerializationTests
{
    /// <summary>保存并恢复非默认半径、掩码和重算配置，只清理本测试独占目录。</summary>
    [Test] public void Prefab_RestoresConfigurationWithoutRuntimeState()
    {
        string folder = "AgentSerialization-" + Guid.NewGuid().ToString("N");
        string guid = AssetDatabase.CreateFolder("Assets", folder); string path = "Assets/" + folder + "/Agent.prefab";
        var go = new GameObject("Agent config");
        try
        {
            var component = go.AddComponent<NavigationAgent2DComponent>(); var serialized = new SerializedObject(component);
            serialized.FindProperty("radius").floatValue = .37f; serialized.FindProperty("obstacleMask").intValue = 256;
            serialized.FindProperty("repathInterval").floatValue = .42f; serialized.ApplyModifiedPropertiesWithoutUndo();
            component.Agent.SetDestination(Vector2.right); PrefabUtility.SaveAsPrefabAsset(go, path);
            var restored = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NavigationAgent2DComponent>();
            var data = new SerializedObject(restored); Assert.That(data.FindProperty("radius").floatValue, Is.EqualTo(.37f));
            Assert.That(data.FindProperty("obstacleMask").intValue, Is.EqualTo(256)); Assert.That(data.FindProperty("repathInterval").floatValue, Is.EqualTo(.42f));
            Assert.That(data.FindProperty("agent"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            if (AssetDatabase.AssetPathToGUID("Assets/" + folder) == guid) AssetDatabase.DeleteAsset("Assets/" + folder);
        }
    }
}
