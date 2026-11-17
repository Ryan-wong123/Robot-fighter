using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public sealed class CombatSandboxTests
{
    [UnityTest]
    public IEnumerator CombatSandboxStartsRoundAndAiApproaches()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatSandbox.unity");
        yield return new EnterPlayMode();

        var match = Object.FindFirstObjectByType<MatchController>();
        var ai = Object.FindFirstObjectByType<AIInput>();
        Assert.That(match, Is.Not.Null);
        Assert.That(ai, Is.Not.Null);
        float deadline = Time.realtimeSinceStartup + 3f;
        while (match.RoundNumber == 0 && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(match.RoundNumber, Is.EqualTo(1));
        Assert.That(ai.GetComponent<Fighter>().Health, Is.GreaterThan(0));
        float start = ai.transform.position.x;
        deadline = Time.realtimeSinceStartup + 3f;
        while (ai.transform.position.x >= start && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(ai.transform.position.x, Is.LessThan(start));

        yield return new ExitPlayMode();
    }
}
