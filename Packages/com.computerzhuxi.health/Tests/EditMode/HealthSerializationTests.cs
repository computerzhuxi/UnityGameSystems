using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Computerzhuxi.Health.Tests
{
    public sealed class HealthSerializationTests
    {
        /// <summary>通过实际 Prefab 保存与加载验证 Inspector 初值不会被运行时写回。</summary>
        [Test] public void PrefabInitialValues()
        {
            const string path="Assets/HealthSerializationTest.prefab";
            var host=new GameObject("Serialized Health"); host.SetActive(false);
            GameObject instance=null;
            try
            {
                var component=host.AddComponent<HealthComponent>(); var data=new SerializedObject(component);
                data.FindProperty("maximum").intValue=25; data.FindProperty("startWithFullHealth").boolValue=false;
                data.FindProperty("startingHealth").intValue=7; data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(host,path); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                var loaded=instance.GetComponent<HealthComponent>(); loaded.Initialize(); Assert.That(loaded.State.Current,Is.EqualTo(7)); Assert.That(loaded.State.Maximum,Is.EqualTo(25));
                loaded.Damage(3); var serialized=new SerializedObject(loaded); Assert.That(serialized.FindProperty("startingHealth").intValue,Is.EqualTo(7));
            }
            finally { if(instance!=null) Object.DestroyImmediate(instance); Object.DestroyImmediate(host); AssetDatabase.DeleteAsset(path); }
        }
    }
}
