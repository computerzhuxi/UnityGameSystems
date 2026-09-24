using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Computerzhuxi.Navigation2D;

/// <summary>验证直接使用组件的完整 Inspector 配置及 Prefab 引用恢复。</summary>
public sealed class NavigatorSerializationTests
{
    /// <summary>真实保存与加载身体、目标及参数，禁止把运行时代理或路径序列化进资产。</summary>
    [Test] public void Prefab_RestoresInspectorSettingsAndReferences()
    {
        string folder = "NavigatorSerialization-" + Guid.NewGuid().ToString("N");
        string guid = AssetDatabase.CreateFolder("Assets", folder);
        var go = new GameObject("Navigator config");
        try
        {
            var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
            var shape = go.AddComponent<CircleCollider2D>(); shape.radius = .3f;
            var target = new GameObject("Target"); target.transform.SetParent(go.transform); target.transform.localPosition = Vector3.right * 3;
            var navigator = go.AddComponent<NavigationNavigator2D>();
            var data = new SerializedObject(navigator);
            data.FindProperty("bodyCollider").objectReferenceValue = shape;
            data.FindProperty("target").objectReferenceValue = target.transform;
            data.FindProperty("obstacleMask").intValue = 256;
            data.FindProperty("cellSize").floatValue = .37f;
            data.FindProperty("gridOrigin").vector2Value = new(1, 2);
            data.FindProperty("maxExpandedNodes").intValue = 8192;
            data.FindProperty("repathInterval").floatValue = .42f;
            data.ApplyModifiedPropertiesWithoutUndo();
            string path = "Assets/" + folder + "/Navigator.prefab"; PrefabUtility.SaveAsPrefabAsset(go, path);
            var restored = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            data = new SerializedObject(restored.GetComponent<NavigationNavigator2D>());
            Assert.That(data.FindProperty("bodyCollider").objectReferenceValue, Is.EqualTo(restored.GetComponent<CircleCollider2D>()));
            Assert.That(data.FindProperty("target").objectReferenceValue, Is.EqualTo(restored.transform.GetChild(0)));
            Assert.That(data.FindProperty("cellSize").floatValue, Is.EqualTo(.37f));
            Assert.That(data.FindProperty("gridOrigin").vector2Value, Is.EqualTo(new Vector2(1, 2)));
            Assert.That(data.FindProperty("maxExpandedNodes").intValue, Is.EqualTo(8192));
            Assert.That(data.FindProperty("repathInterval").floatValue, Is.EqualTo(.42f));
            Assert.That(data.FindProperty("agent"), Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            if (AssetDatabase.AssetPathToGUID("Assets/" + folder) == guid) AssetDatabase.DeleteAsset("Assets/" + folder);
        }
    }
}
