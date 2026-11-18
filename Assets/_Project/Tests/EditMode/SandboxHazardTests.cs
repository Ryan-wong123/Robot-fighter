using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Gameplay;

public sealed class SandboxHazardTests
{
    [UnityTest]
    public IEnumerator WarningIsSafeAndActiveZoneHitsEachFighterOnce()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatSandbox.unity");
        yield return new EnterPlayMode();
        yield return null;

        var hazard = Object.FindFirstObjectByType<HazardController>();
        Assert.That(hazard, Is.Not.Null);
        var player = Object.FindObjectsByType<Fighter>(FindObjectsSortMode.None).Single(fighter => fighter.Team == 0);
        var ai = Object.FindFirstObjectByType<AIInput>();
        ai.TogglePause();
        var body = player.GetComponent<Rigidbody>();
        body.position = new Vector3(0f, .02f, 0f);
        int health = player.Health;
        int opponentHealth = ai.GetComponent<Fighter>().Health;

        Time.timeScale = 8f;
        try
        {
            yield return WaitFor(hazard, HazardPhase.Telegraph);
            float warningStart = Time.fixedTime;
            Assert.That(player.Health, Is.EqualTo(health));
            yield return WaitFor(hazard, HazardPhase.Active);
            Assert.That(Time.fixedTime - warningStart, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(player.Health, Is.EqualTo(health - 12));
            Assert.That(ai.GetComponent<Fighter>().Health, Is.EqualTo(opponentHealth));

            body.position = new Vector3(0f, .02f, 0f);
            yield return WaitFor(hazard, HazardPhase.Cooldown);
            Assert.That(player.Health, Is.EqualTo(health - 12));
            Assert.That(hazard.transform.GetChild(0).gameObject.activeSelf, Is.False);
            Assert.That(hazard.transform.GetChild(1).gameObject.activeSelf, Is.False);
        }
        finally
        {
            Time.timeScale = 1f;
        }

        yield return new ExitPlayMode();
    }

    private static IEnumerator WaitFor(HazardController hazard, HazardPhase phase)
    {
        float deadline = Time.realtimeSinceStartup + 8f;
        while (hazard.Phase != phase && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(hazard.Phase, Is.EqualTo(phase));
    }
}
