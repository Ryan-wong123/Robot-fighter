using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PrototypeSceneTests
{
    [Test]
    public void BattleSceneHasRoundReferencesAndInput()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/BattleScene 1.unity", OpenSceneMode.Additive);
        try
        {
            var scripts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            var manager = new SerializedObject(scripts.Single(script => script != null && script.GetType().Name == "GameManager"));
            foreach (var field in new[] { "playerBehaviour", "enemyBehaviour", "TimerCountDown", "Crate", "SpawnEffect", "restart" })
                Assert.That(manager.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            var input = new SerializedObject(scripts.Single(script => script != null && script.GetType().Name == "TouchInput"));
            Assert.That(input.FindProperty("playerBehaviour").objectReferenceValue, Is.Not.Null);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void PrototypeMaterialsReferenceUrpShaders()
    {
        Assert.That(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline, Is.Not.Null);
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Art" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".mat"))
                continue;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.That(material.shader.name, Does.StartWith("Universal Render Pipeline/"), path);
        }
    }
}
