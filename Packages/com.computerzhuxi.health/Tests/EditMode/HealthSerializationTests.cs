using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Computerzhuxi.Health.Tests
{
    public sealed class HealthSerializationTests
    {
        private string temporaryFolder;
        private string temporaryFolderGuid;

        /// <summary>为每个测试创建独占目录，避免触碰使用者已有资源。</summary>
        [SetUp] public void CreateTemporaryFolder()
        {
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/HealthTests_" + Guid.NewGuid().ToString("N"));
            temporaryFolderGuid = AssetDatabase.CreateFolder("Assets", Path.GetFileName(path));
            Assert.That(temporaryFolderGuid, Is.Not.Empty);
            temporaryFolder = AssetDatabase.GUIDToAssetPath(temporaryFolderGuid);
        }

        /// <summary>仅清理本次测试创建且 GUID 仍匹配的独占目录。</summary>
        [TearDown] public void DeleteTemporaryFolder()
        {
            // 创建失败或目录身份改变时不删除，宁可留下临时内容也不冒险删除外部资源。
            if (!string.IsNullOrEmpty(temporaryFolderGuid) &&
                AssetDatabase.AssetPathToGUID(temporaryFolder) == temporaryFolderGuid)
                AssetDatabase.DeleteAsset(temporaryFolder);
        }

        /// <summary>通过实际 Prefab 保存与加载验证 Inspector 初值不会被运行时写回。</summary>
        [Test] public void PrefabInitialValues()
        {
            ExercisePrefabRoundTrip(false);
        }

        /// <summary>验证同名已有资源在正常执行和异常清理后均保持内容及 GUID 不变。</summary>
        [TestCase(false)] [TestCase(true)]
        public void ExistingResourceSurvivesCleanup(bool failAfterSave)
        {
            string path = temporaryFolder + "/HealthSerializationTest.prefab";
            var sentinel = new GameObject("Existing resource must survive");
            try { Assert.That(PrefabUtility.SaveAsPrefabAsset(sentinel, path), Is.Not.Null); }
            finally { Object.DestroyImmediate(sentinel); }
            string guid = AssetDatabase.AssetPathToGUID(path);
            byte[] bytes = File.ReadAllBytes(path);
            string originalName = AssetDatabase.LoadAssetAtPath<GameObject>(path).name;
            if (failAfterSave)
                Assert.Throws<InvalidOperationException>(() => ExercisePrefabRoundTrip(true));
            else
                ExercisePrefabRoundTrip(false);
            Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path).name, Is.EqualTo(originalName));
        }

        /// <summary>在独占目录中选择未占用路径执行序列化，并只清理本次成功创建的资源。</summary>
        private void ExercisePrefabRoundTrip(bool failAfterSave)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath(temporaryFolder + "/HealthSerializationTest.prefab");
            string createdGuid = null;
            var host = new GameObject("Serialized Health");
            host.SetActive(false);
            GameObject instance = null;
            try
            {
                var component = host.AddComponent<HealthComponent>();
                Configure(component, 25, false, 7);
                Assert.That(PrefabUtility.SaveAsPrefabAsset(host, path), Is.Not.Null);
                createdGuid = AssetDatabase.AssetPathToGUID(path);
                if (failAfterSave) throw new InvalidOperationException("模拟保存后失败，检查异常清理。");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                var loaded = instance.GetComponent<HealthComponent>();
                loaded.Initialize();
                Assert.That(loaded.State.Current, Is.EqualTo(7));
                Assert.That(loaded.State.Maximum, Is.EqualTo(25));
                loaded.Damage(3);
                Assert.That(new SerializedObject(loaded).FindProperty("startingHealth").intValue, Is.EqualTo(7));
            }
            finally
            {
                if (instance != null) Object.DestroyImmediate(instance);
                Object.DestroyImmediate(host);
                if (!string.IsNullOrEmpty(createdGuid) && AssetDatabase.AssetPathToGUID(path) == createdGuid)
                    AssetDatabase.DeleteAsset(path);
            }
        }

        /// <summary>验证满血开关不能掩盖非法初值，失败初始化不发布事件且允许修正后重试。</summary>
        [TestCase(0, 0, false)] [TestCase(0, 0, true)]
        [TestCase(-1, 0, false)] [TestCase(-1, 0, true)]
        [TestCase(10, -1, false)] [TestCase(10, -1, true)]
        [TestCase(10, 11, false)] [TestCase(10, 11, true)]
        public void InvalidSerializedValuesRejectInitialization(int maximum, int current, bool full)
        {
            var host = new GameObject("Invalid serialized health"); host.SetActive(false);
            try
            {
                var component = host.AddComponent<HealthComponent>();
                Configure(component, maximum, full, current);
                int events = 0;
                component.OnChanged.AddListener(() => events++);
                Assert.Throws<ArgumentOutOfRangeException>(() => component.Initialize());
                Assert.That(component.IsInitialized, Is.False);
                Assert.That(events, Is.Zero);
                Configure(component, 10, false, 5);
                component.Initialize(); component.Damage(1);
                Assert.That(component.State.Current, Is.EqualTo(4));
                Assert.That(events, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(host); }
        }

        /// <summary>验证序列化初值的合法端点以及满血开关的选择语义。</summary>
        [TestCase(0, false, 0)] [TestCase(10, false, 10)]
        [TestCase(0, true, 10)] [TestCase(10, true, 10)]
        public void ValidSerializedBoundariesInitialize(int current, bool full, int expected)
        {
            var host = new GameObject("Valid serialized health"); host.SetActive(false);
            try
            {
                var component = host.AddComponent<HealthComponent>();
                Configure(component, 10, full, current);
                component.Initialize();
                Assert.That(component.State.Current, Is.EqualTo(expected));
            }
            finally { Object.DestroyImmediate(host); }
        }

        /// <summary>通过 Unity 序列化接口设置初值，覆盖 Inspector 属性约束无法阻止的旧资产数据。</summary>
        private static void Configure(HealthComponent component, int maximum, bool full, int current)
        {
            var data = new SerializedObject(component);
            data.FindProperty("maximum").intValue = maximum;
            data.FindProperty("startWithFullHealth").boolValue = full;
            data.FindProperty("startingHealth").intValue = current;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
