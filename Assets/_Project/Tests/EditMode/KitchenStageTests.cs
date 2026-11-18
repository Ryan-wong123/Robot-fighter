using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Gameplay;

public sealed class KitchenStageTests
{
    [UnityTest]
    public IEnumerator ToastDamagesAndBeltReversesWithoutDamage()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/KitchenCounter.unity");
        yield return new EnterPlayMode();
        yield return null;

        var hazards = Object.FindFirstObjectByType<KitchenHazards>();
        Assert.That(hazards, Is.Not.Null);
        var cycle = hazards.GetComponent<HazardController>();
        var ai = Object.FindFirstObjectByType<AIInput>();
        ai.TogglePause();
        var fighter = Object.FindObjectsByType<Fighter>(FindObjectsSortMode.None).Single(f => f.Team == 0);
        var body = fighter.GetComponent<Rigidbody>();
        ai.GetComponent<Rigidbody>().position = new Vector3(5f, .02f, 0f);
        var landing = hazards.transform.Find("Toast Landing 1");
        var belt = hazards.transform.Find("Mixer Belt");
        var arrows = belt.Find("Belt Direction");
        body.position = landing.position;
        int startHealth = fighter.Health;

        Time.timeScale = 6f;
        try
        {
            yield return WaitFor(cycle, HazardPhase.Telegraph);
            Assert.That(landing.gameObject.activeSelf, Is.True);
            Assert.That(fighter.Health, Is.EqualTo(startHealth));
            yield return WaitFor(cycle, HazardPhase.Active);
            yield return WaitFor(cycle, HazardPhase.Cooldown);
            Assert.That(fighter.Health, Is.EqualTo(startHealth - 10));
            Assert.That(landing.gameObject.activeSelf, Is.False);

            body.position = belt.position;
            yield return WaitFor(cycle, HazardPhase.Telegraph);
            Assert.That(arrows.gameObject.activeSelf, Is.True);
            Assert.That(body.linearVelocity.x, Is.EqualTo(0f).Within(.05f));
            yield return WaitFor(cycle, HazardPhase.Active);
            while (body.linearVelocity.x <= 0f && cycle.Phase == HazardPhase.Active &&
                   cycle.PhaseElapsed < .8f)
                yield return null;
            Assert.That(body.linearVelocity.x, Is.GreaterThan(0f), $"Fighter at {body.position} on belt {belt.position}");

            while (cycle.Phase == HazardPhase.Active && arrows.localRotation == Quaternion.identity)
                yield return null;
            Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Active));
            body.position = belt.position;
            while (body.linearVelocity.x >= 0f && cycle.Phase == HazardPhase.Active &&
                   cycle.PhaseElapsed < 2.7f)
                yield return null;
            Assert.That(body.linearVelocity.x, Is.LessThan(0f));
            yield return WaitFor(cycle, HazardPhase.Cooldown);
            Assert.That(arrows.gameObject.activeSelf, Is.False);
            Assert.That(fighter.Health, Is.EqualTo(startHealth - 10));
        }
        finally
        {
            Time.timeScale = 1f;
        }
        yield return new ExitPlayMode();
    }

    private static IEnumerator WaitFor(HazardController cycle, HazardPhase phase)
    {
        float deadline = Time.realtimeSinceStartup + 8f;
        while (cycle.Phase != phase && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(cycle.Phase, Is.EqualTo(phase));
    }
}
