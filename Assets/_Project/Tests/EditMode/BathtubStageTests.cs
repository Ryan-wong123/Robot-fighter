using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Gameplay;

public sealed class BathtubStageTests
{
    [UnityTest]
    public IEnumerator StageAlternatesRisingWaterAndHarmlessDamageWave()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/BathtubBrawl.unity");
        yield return new EnterPlayMode();
        yield return null;

        var hazards = Object.FindFirstObjectByType<BathtubHazards>();
        Assert.That(hazards, Is.Not.Null);
        var cycle = hazards.GetComponent<HazardController>();
        var ai = Object.FindFirstObjectByType<AIInput>();
        ai.TogglePause();
        var fighter = ai.GetComponent<Fighter>();
        var body = ai.GetComponent<Rigidbody>();
        int health = fighter.Health;
        var water = hazards.transform.Find("Rising Water");
        var wave = hazards.transform.Find("Duck Wave");
        Assert.That(water.gameObject.activeSelf, Is.False);

        Time.timeScale = 8f;
        try
        {
            float deadline = Time.realtimeSinceStartup + 12f;
            while (water.localPosition.y < 1f && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Active));
            Assert.That(water.localPosition.y, Is.InRange(1f, 1.21f));
            Assert.That(fighter.Health, Is.EqualTo(health));

            while (!wave.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(wave.gameObject.activeSelf, Is.True);
            Assert.That(water.gameObject.activeSelf, Is.False);
            while (fighter.State != FighterState.HitStun && wave.gameObject.activeSelf &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(fighter.State, Is.EqualTo(FighterState.HitStun));
            Assert.That(body.linearVelocity.x, Is.LessThan(0f));
            Assert.That(fighter.Health, Is.EqualTo(health));
        }
        finally
        {
            Time.timeScale = 1f;
        }

        yield return new ExitPlayMode();
    }
}
