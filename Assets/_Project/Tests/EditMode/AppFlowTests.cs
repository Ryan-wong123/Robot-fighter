using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public sealed class AppFlowTests
{
    [Test]
    public void BootIsFirstAndMatchSceneIsEnabled()
    {
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
        Assert.That(scenes[0].path, Does.EndWith("/Boot.unity"));
        Assert.That(scenes.Any(scene => scene.path.EndsWith("/CombatSandbox.unity")), Is.True);
    }

    [UnityTest]
    public IEnumerator BootToResultsAndRematchThenReturnsToMenu()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        yield return ExerciseFlow();
        yield return new ExitPlayMode();
    }

    private static IEnumerator ExerciseFlow()
    {
        var app = Object.FindFirstObjectByType<AppStateMachine>();
        Assert.That(app, Is.Not.Null);
        yield return WaitFor(() => app.State == AppState.MainMenu, "initial menu");
        var panel = app.transform.Find("App Canvas/App Screen");
        Assert.That(panel, Is.Not.Null, "Boot must create its navigation panel.");
        var primary = panel.Find("Primary Action").GetComponent<Button>();
        var secondary = panel.Find("Secondary Action").GetComponent<Button>();
        primary.onClick.Invoke();
        Assert.That(app.State, Is.EqualTo(AppState.Selection));
        secondary.onClick.Invoke();
        Assert.That(app.State, Is.EqualTo(AppState.MainMenu));
        primary.onClick.Invoke();
        primary.onClick.Invoke();
        yield return WaitFor(() => !app.IsLoading && SceneManager.GetActiveScene().name == "CombatSandbox" &&
            Object.FindFirstObjectByType<MatchController>()?.RoundNumber == 1, "first match");
        Assert.That(panel.gameObject.activeSelf, Is.False);

        var match = Object.FindFirstObjectByType<MatchController>();
        var ai = Object.FindFirstObjectByType<AIInput>();
        ai.TogglePause();
        var defender = ai.GetComponent<Fighter>();
        var move = ScriptableObject.CreateInstance<MoveDef>();
        move.BodyDamage = 10000;
        move.HitStopFrames = 0;
        var hit = new HitInfo(defender.ItemOpponent, move, HurtZone.Body, defender.transform.position, Vector3.right);
        Assert.That(defender.ReceiveHit(hit), Is.True);
        yield return WaitFor(() => match.RoundNumber == 2, "second round");
        Assert.That(defender.ReceiveHit(hit), Is.True);
        yield return WaitFor(() => app.State == AppState.Results, "results");
        Assert.That(app.Result, Is.EqualTo(RoundResult.PlayerWins));
        Assert.That(panel.gameObject.activeSelf, Is.True);

        primary.onClick.Invoke();
        yield return WaitFor(() => !app.IsLoading && app.State == AppState.Match, "rematch");
        Assert.That(Object.FindFirstObjectByType<MatchController>(), Is.Not.SameAs(match));
        Assert.That(Object.FindFirstObjectByType<MatchController>().RoundNumber, Is.EqualTo(1));
        app.ReturnToMenu();
        yield return WaitFor(() => app.State == AppState.MainMenu && !app.IsLoading, "menu return");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Boot"));
        Assert.That(Object.FindObjectsByType<AppStateMachine>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Object.Destroy(move);

    }

    private static IEnumerator WaitFor(System.Func<bool> condition, string transition)
    {
        float deadline = Time.realtimeSinceStartup + 8f;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
        var app = Object.FindFirstObjectByType<AppStateMachine>();
        var match = Object.FindFirstObjectByType<MatchController>();
        Assert.That(condition(), Is.True, $"{transition} timed out. Scene {SceneManager.GetActiveScene().name}, " +
            $"state {app?.State}, loading {app?.IsLoading}, round {match?.RoundNumber}, wins {match?.PlayerWins}.");
    }
}
