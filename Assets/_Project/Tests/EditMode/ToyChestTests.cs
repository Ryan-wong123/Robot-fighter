using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public sealed class ToyChestTests
{
    [UnityTest]
    public IEnumerator BossStageAlternatesHazardsAndCleansUp()
    {
        const string path = "Assets/_Project/Scenes/ToyChestShowdown.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            var assembly = Assembly.Load("Assembly-CSharp-Editor");
            var boss = assembly.GetType("MrJackContent", true);
            if (AssetDatabase.LoadAssetAtPath<WobbleBrawl.Data.CharacterDef>(
                "Assets/_Project/Resources/Characters/MrJack.asset") == null)
                boss.GetMethod("Build").Invoke(null, null);
            assembly.GetType("CombatSandboxBuilder", true).GetMethod("BuildToyChest").Invoke(null, null);
        }
        EditorSceneManager.OpenScene(path);
        yield return new EnterPlayMode();
        yield return null;

        var round = Object.FindFirstObjectByType<RoundController>();
        var cycle = Object.FindFirstObjectByType<HazardController>();
        var chest = cycle.GetComponent<ToyChestHazards>();
        Assert.That(round.Opponent.Boss, Is.Not.Null);
        Assert.That(round.IsTraining, Is.False);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var blocks = (Transform[])typeof(ToyChestHazards).GetField("blocks", flags).GetValue(chest);
        var shadows = (Transform[])typeof(ToyChestHazards).GetField("shadows", flags).GetValue(chest);
        var deck = (Transform)typeof(ToyChestHazards).GetField("deck", flags).GetValue(chest);
        var advance = typeof(HazardController).GetMethod("FixedUpdate", flags);
        var remaining = typeof(HazardController).GetField("remaining", flags);
        var tick = typeof(ToyChestHazards).GetMethod("FixedUpdate", flags);
        cycle.BeginRound(0);
        void NextPhase()
        {
            remaining.SetValue(cycle, 0f);
            advance.Invoke(cycle, null);
        }
        NextPhase();
        Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Telegraph));
        Assert.That(shadows[0].gameObject.activeSelf, Is.True);
        Assert.That(blocks[0].gameObject.activeSelf, Is.False);
        NextPhase();
        Assert.That(blocks[0].gameObject.activeSelf, Is.True);
        Assert.That(shadows[0].gameObject.activeSelf, Is.False);
        NextPhase(); // cooldown
        NextPhase(); // idle
        NextPhase(); // tilt warning
        Assert.That(shadows[0].gameObject.activeSelf, Is.False);
        NextPhase();
        remaining.SetValue(cycle, 2f);
        round.Player.ResetRound(new Vector3(-2f, .02f, 0), 1);
        round.Opponent.ResetRound(new Vector3(2f, .02f, 0), -1);
        Physics.SyncTransforms();
        var playerBody = round.Player.GetComponent<Rigidbody>();
        var opponentBody = round.Opponent.GetComponent<Rigidbody>();
        float playerX = playerBody.position.x;
        float opponentX = opponentBody.position.x;
        tick.Invoke(chest, null);
        var floor = (BoxCollider)typeof(ToyChestHazards).GetField("deckFloor", flags).GetValue(chest);
        Assert.That(floor.transform.up, Is.EqualTo(deck.up));
        foreach (var body in new[] { playerBody, opponentBody })
        {
            Assert.That(floor.Raycast(new Ray(body.position + Vector3.up, Vector3.down), out var ground, 2f), Is.True);
            Assert.That(body.position.y - ground.point.y, Is.EqualTo(.02f).Within(.03f));
        }
        Assert.That(playerBody.position.x, Is.GreaterThan(playerX));
        Assert.That(opponentBody.position.x, Is.GreaterThan(opponentX));
        Assert.That(round.Player.Health, Is.EqualTo(round.Player.MaxHealth));
        Assert.That(round.Opponent.Health, Is.EqualTo(round.Opponent.MaxHealth));
        Assert.That(Quaternion.Angle(deck.localRotation, Quaternion.identity), Is.GreaterThan(5f));
        Assert.That(blocks[0].gameObject.activeSelf, Is.False);
        cycle.BeginSuddenDeath();
        Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Inactive));
        Assert.That(deck.localRotation, Is.EqualTo(Quaternion.identity));
        yield return new ExitPlayMode();
    }
}
