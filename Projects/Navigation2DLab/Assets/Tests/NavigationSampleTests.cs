using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Computerzhuxi.Navigation2D.Samples;

/// <summary>验证导入 Sample 的实际 Prefab 序列化，不依赖包内部实现。</summary>
public sealed class NavigationSampleTests
{
    /// <summary>Sample 参数保存后能恢复，测试只清理自己创建且 GUID 未变化的独占资源。</summary>
    [Test] public void SamplePrefab_RestoresSerializedRadius()
    {
        string directory="NavigationSampleTest-"+Guid.NewGuid().ToString("N");
        string folderGuid=AssetDatabase.CreateFolder("Assets",directory); string path=AssetDatabase.GenerateUniqueAssetPath("Assets/"+directory+"/Demo.prefab"); string assetGuid=null;
        var go=new GameObject("Sample serialization");
        try
        {
            var demo=go.AddComponent<NavigationDemo>(); var serialized=new SerializedObject(demo); serialized.FindProperty("radius").floatValue=.37f; serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(go,path); assetGuid=AssetDatabase.AssetPathToGUID(path);
            var restored=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NavigationDemo>();
            Assert.That(new SerializedObject(restored).FindProperty("radius").floatValue,Is.EqualTo(.37f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            if(assetGuid!=null && AssetDatabase.AssetPathToGUID(path)==assetGuid) AssetDatabase.DeleteAsset(path);
            // 目录独占且 GUID 未变时才清理，避免固定 Assets 路径覆盖既有内容。
            string folder="Assets/"+directory;
            if(AssetDatabase.AssetPathToGUID(folder)==folderGuid && AssetDatabase.FindAssets("",new[]{folder}).Length==0) AssetDatabase.DeleteAsset(folder);
        }
    }
}
