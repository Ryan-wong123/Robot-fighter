using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;

public sealed class SandboxAITests
{
    [Test]
    public void DifficultyAssetsTuneBehaviorInOrder()
    {
        var easy = Resources.Load<AIProfile>("AI/Easy");
        var normal = Resources.Load<AIProfile>("AI/Normal");
        var hard = Resources.Load<AIProfile>("AI/Hard");
        Assert.That(easy, Is.Not.Null);
        Assert.That(normal, Is.Not.Null);
        Assert.That(hard, Is.Not.Null);
        Assert.That(easy.ReactionFrames, Is.GreaterThan(normal.ReactionFrames));
        Assert.That(normal.ReactionFrames, Is.GreaterThan(hard.ReactionFrames));
        Assert.That(easy.BlockChance, Is.LessThan(hard.BlockChance));
        Assert.That(easy.MistakeChance, Is.GreaterThan(hard.MistakeChance));
        Assert.That(easy.ItemUseChance, Is.LessThan(hard.ItemUseChance));
        Assert.That(easy.HazardAvoidance, Is.LessThan(hard.HazardAvoidance));
    }

    [UnityTest]
    public IEnumerator OpponentSeeksAndUsesPickupThenAvoidsTelegraph()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatSandbox.unity");
        yield return new EnterPlayMode();
        yield return null;

        var ai = Object.FindFirstObjectByType<AIInput>();
        var self = ai.GetComponent<Fighter>();
        var opponent = self.ItemOpponent;
        var body = ai.GetComponent<Rigidbody>();
        var otherBody = opponent.GetComponent<Rigidbody>();
        var spawner = Object.FindFirstObjectByType<ItemSpawner>();
        var hazard = Object.FindFirstObjectByType<HazardController>();
        var rocket = Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None)
            .Single(item => item.Definition.Id == "Rocket Plunger");
        var profile = ScriptableObject.CreateInstance<AIProfile>();
        profile.ReactionFrames = 1;
        profile.MistakeChance = 0f;
        profile.ItemUseChance = profile.HazardAvoidance = 1f;
        typeof(AIInput).GetField("profile", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ai, profile);

        self.enabled = false;
        body.linearVelocity = otherBody.linearVelocity = Vector3.zero;
        otherBody.position = new Vector3(6f, .02f, 0f);
        body.position = new Vector3(2f, .02f, 0f);
        yield return new WaitForFixedUpdate();
        yield return null;
        ai.ConfigureRound(spawner, hazard, 0);
        Assert.That(ai.ReadIntent().Move, Is.LessThan(0f));

        body.position = new Vector3(rocket.transform.position.x, .02f, 0f);
        otherBody.position = new Vector3(-2.2f, .02f, 0f);
        var holder = self.GetComponent<ItemHolder>();
        float deadline = Time.realtimeSinceStartup + 3f;
        while (!holder.HasItem && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(holder.HasItem, Is.True);
        Assert.That(ai.ReadIntent().Throw, Is.True);

        Time.timeScale = 8f;
        try
        {
            body.position = new Vector3(.45f, .02f, 0f);
            body.linearVelocity = Vector3.zero;
            deadline = Time.realtimeSinceStartup + 6f;
            while (hazard.Phase != HazardPhase.Telegraph && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(hazard.Phase, Is.EqualTo(HazardPhase.Telegraph));
            Assert.That(ai.ReadIntent().Move, Is.GreaterThan(0f), $"AI at {self.transform.position}");
        }
        finally
        {
            Time.timeScale = 1f;
            Object.Destroy(profile);
        }
        yield return new ExitPlayMode();
    }
}
