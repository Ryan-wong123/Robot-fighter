using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PrototypeSceneTests
{
    [TestCase("BattleScene 1")]
    [TestCase("BattleScene 2")]
    public void LegacySceneHasExplicitFighterReferences(string sceneName)
    {
        var scene = EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{sceneName}.unity", OpenSceneMode.Additive);
        try
        {
            var scripts = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(script => script != null).ToArray();
            var player = scripts.Single(script => script.GetType().Name == "PlayerBehaviour");
            var enemy = scripts.Single(script => script.GetType().Name == "EnemyBehaviour");
            var hand = scripts.Single(script => script.GetType().Name == "RightHandPickup");
            var playerData = new SerializedObject(player);
            var enemyData = new SerializedObject(enemy);
            Assert.That(playerData.FindProperty("rightHand").objectReferenceValue, Is.EqualTo(hand.transform));
            Assert.That(enemyData.FindProperty("TargetPlayer").objectReferenceValue, Is.EqualTo(player.transform));
            foreach (var fighter in new[] { playerData, enemyData })
            {
                Assert.That(fighter.FindProperty("Health").objectReferenceValue, Is.Not.Null);
                Assert.That(fighter.FindProperty("AttackGauge").objectReferenceValue, Is.Not.Null);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

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
