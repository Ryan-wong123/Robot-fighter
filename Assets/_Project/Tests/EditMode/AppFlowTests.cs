using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using WobbleBrawl.Core;
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
        Assert.That(scenes.Any(scene => scene.path.EndsWith("/BathtubBrawl.unity")), Is.True);
        Assert.That(scenes.Any(scene => scene.path.EndsWith("/KitchenCounter.unity")), Is.True);
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
        TestEconomy.Install(app);
        Assert.That(app, Is.Not.Null);
        yield return WaitFor(() => app.State == AppState.MainMenu, "initial menu");
        var panel = app.transform.Find("App Canvas/App Screen");
        Assert.That(panel, Is.Not.Null, "Boot must create its navigation panel.");
        var artwork = panel.Find("Bathtub Brawl Illustration").GetComponent<RawImage>();
        Assert.That(artwork.texture, Is.Not.Null);
        Assert.That(artwork.gameObject.activeSelf, Is.True);
        var primary = panel.Find("Safe Content/Primary Action").GetComponent<Button>();
        var secondary = panel.Find("Safe Content/Secondary Action").GetComponent<Button>();
        var settings = panel.Find("Safe Content/Settings Action").GetComponent<Button>();
        settings.onClick.Invoke();
        var settingsPanel = panel.Find("Safe Content/Settings Controls");
        Assert.That(settingsPanel.gameObject.activeSelf, Is.True);
        settingsPanel.Find("Master Slider").GetComponent<Slider>().value = .4f;
        Assert.That(AudioListener.volume, Is.EqualTo(.4f).Within(.01f));
        settingsPanel.Find("Controls").GetComponent<Button>().onClick.Invoke();
        Assert.That(GameSettings.Layout, Is.EqualTo(ControlLayout.Mirrored));
        secondary.onClick.Invoke();
        Assert.That(app.State, Is.EqualTo(AppState.MainMenu));
        Assert.That(settingsPanel.gameObject.activeSelf, Is.False);
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(primary.gameObject));
        primary.onClick.Invoke();
        Assert.That(app.State, Is.EqualTo(AppState.Selection));
        Assert.That(app.Step, Is.EqualTo(SelectionStep.Fighter));
        Assert.That(artwork.gameObject.activeSelf, Is.False);
        secondary.onClick.Invoke();
        Assert.That(app.State, Is.EqualTo(AppState.MainMenu));
        primary.onClick.Invoke();
        primary.onClick.Invoke();
        Assert.That(app.Step, Is.EqualTo(SelectionStep.Stage));
        secondary.onClick.Invoke();
        Assert.That(app.Step, Is.EqualTo(SelectionStep.Fighter));
        primary.onClick.Invoke();
        var kitchen = panel.Find("Safe Content/Stage 1").GetComponent<Button>();
        kitchen.onClick.Invoke();
        Assert.That(app.GetStage(app.StageIndex).Scene, Is.EqualTo("KitchenCounter"));
        primary.onClick.Invoke();
        yield return WaitFor(() => !app.IsLoading && SceneManager.GetActiveScene().name == "KitchenCounter" &&
            Object.FindFirstObjectByType<MatchController>()?.RoundNumber == 1, "first match");
        Assert.That(panel.gameObject.activeSelf, Is.False);

        var match = Object.FindFirstObjectByType<MatchController>();
        Assert.That(Object.FindFirstObjectByType<WobbleBrawl.UI.MatchTouchControls>(), Is.Not.Null);
        var pauseButton = app.transform.Find("App Canvas/Pause Match").GetComponent<Button>();
        Assert.That(pauseButton.gameObject.activeSelf, Is.True, "Pause must appear when round one starts.");
        var round = match.GetComponent<RoundController>();
        var hazard = Object.FindFirstObjectByType<HazardController>();
        int seconds = round.SecondsRemaining;
        float hazardTime = hazard.PhaseElapsed;
        pauseButton.onClick.Invoke();
        Assert.That(app.IsPaused, Is.True);
        Assert.That(panel.gameObject.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        settings.onClick.Invoke();
        Assert.That(settingsPanel.gameObject.activeSelf, Is.True);
        settingsPanel.Find("Controls").GetComponent<Button>().onClick.Invoke();
        Assert.That(GameSettings.Layout, Is.EqualTo(ControlLayout.Standard));
        secondary.onClick.Invoke();
        Assert.That(app.IsPaused, Is.True);
        yield return new WaitForSecondsRealtime(1.2f);
        Assert.That(round.SecondsRemaining, Is.EqualTo(seconds));
        Assert.That(hazard.PhaseElapsed, Is.EqualTo(hazardTime));
        primary.onClick.Invoke();
        Assert.That(app.IsPaused, Is.False);
        Assert.That(pauseButton.gameObject.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        var ai = Object.FindFirstObjectByType<AIInput>();
        ai.TogglePause();
        var defender = ai.GetComponent<Fighter>();
        var move = ScriptableObject.CreateInstance<MoveDef>();
        move.BodyDamage = 10000;
        move.HitStopFrames = 0;
        var hit = new HitInfo(defender.ItemOpponent, move, HurtZone.Body, defender.transform.position, Vector3.right);
        Assert.That(defender.ReceiveHit(hit), Is.True);
        Assert.That(pauseButton.gameObject.activeSelf, Is.True, "Pause stays visible between rounds.");
        Assert.That(pauseButton.interactable, Is.False);
        yield return WaitFor(() => match.RoundNumber == 2, "second round");
        Assert.That(pauseButton.interactable, Is.True);
        Assert.That(defender.ReceiveHit(hit), Is.True);
        yield return WaitFor(() => app.State == AppState.Results, "results");
        Assert.That(app.Result, Is.EqualTo(RoundResult.PlayerWins));
        Assert.That(app.MatchSummary, Is.EqualTo("2 – 0"));
        Assert.That(panel.gameObject.activeSelf, Is.True);

        primary.onClick.Invoke();
        yield return WaitFor(() => !app.IsLoading && app.State == AppState.Match, "rematch");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("KitchenCounter"));
        Assert.That(Object.FindFirstObjectByType<MatchController>(), Is.Not.SameAs(match));
        Assert.That(Object.FindFirstObjectByType<MatchController>().RoundNumber, Is.EqualTo(1));
        app.ReturnToMenu();
        yield return WaitFor(() => app.State == AppState.MainMenu && !app.IsLoading, "menu return");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Boot"));
        Assert.That(Object.FindObjectsByType<AppStateMachine>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        Assert.That(artwork.gameObject.activeSelf, Is.True);
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(primary.gameObject));
        Object.Destroy(move);

    }

    private static IEnumerator WaitFor(System.Func<bool> condition, string transition)
    {
        float deadline = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
        var app = Object.FindFirstObjectByType<AppStateMachine>();
        TestEconomy.Install(app);
        var match = Object.FindFirstObjectByType<MatchController>();
        Assert.That(condition(), Is.True, $"{transition} timed out. Scene {SceneManager.GetActiveScene().name}, " +
            $"state {app?.State}, loading {app?.IsLoading}, round {match?.RoundNumber}, wins {match?.PlayerWins}.");
    }
}
