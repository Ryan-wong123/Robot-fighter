using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WobbleBrawl.Core;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public sealed class MatchRulesTests
{
    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ClassicOptionsAreIndependent(bool hazards, bool items)
    {
        foreach (var mode in new[] { MatchMode.QuickFight, MatchMode.Versus })
        {
            var rules = new MatchRules(hazards, items).Resolve(mode, false);
            Assert.That(rules.HazardsEnabled, Is.EqualTo(hazards));
            Assert.That(rules.ItemsEnabled, Is.EqualTo(items));
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ArcadeAndBossForceBothSystemsOn(bool hazards, bool items)
    {
        var requested = new MatchRules(hazards, items);
        foreach (var rules in new[] { requested.Resolve(MatchMode.Arcade, false),
                                     requested.Resolve(MatchMode.QuickFight, true),
                                     requested.Resolve(MatchMode.Versus, true) })
        {
            Assert.That(rules.HazardsEnabled, Is.True);
            Assert.That(rules.ItemsEnabled, Is.True);
        }
        Assert.That(MatchRules.Default.HazardsEnabled, Is.True);
        Assert.That(MatchRules.Default.ItemsEnabled, Is.True);
    }

    [UnityTest]
    public IEnumerator SetupTogglesControlSpawningAndHazardTimers()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = Object.FindFirstObjectByType<AppStateMachine>();
        foreach (bool hazards in new[] { false, true })
        foreach (bool items in new[] { false, true })
        {
            app.OpenSelection();
            app.SelectBlobby();
            Assert.That(app.Rules.HazardsEnabled && app.Rules.ItemsEnabled, Is.True,
                "Fresh setup must default to both on.");
            var toggles = app.GetComponentsInChildren<Toggle>(true);
            var hazardToggle = toggles.Single(toggle => toggle.name == "Hazards Toggle");
            var itemToggle = toggles.Single(toggle => toggle.name == "Items Toggle");
            hazardToggle.isOn = hazards;
            itemToggle.isOn = items;
            for (int index = 0; index < app.StageCount; index++)
                if (app.GetStage(index).Scene == "CombatSandbox")
                    app.SelectStage(index);
            app.StartMatch();
            yield return WaitFor(() => !app.IsLoading && app.State == AppState.Match);
            yield return null;
            var match = Object.FindFirstObjectByType<MatchController>();
            var hazard = Object.FindFirstObjectByType<HazardController>();
            var spawner = Object.FindFirstObjectByType<ItemSpawner>();
            Object.FindFirstObjectByType<AIInput>().TogglePause();
            Assert.That(match.Rules.HazardsEnabled, Is.EqualTo(hazards));
            Assert.That(match.Rules.ItemsEnabled, Is.EqualTo(items));
            Assert.That(Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None).Length > 0,
                Is.EqualTo(items), "Opening lineup obeys item rules.");
            yield return new WaitForSeconds(.2f);
            Assert.That(hazard.Phase == HazardPhase.Inactive, Is.EqualTo(!hazards));
            if (!hazards)
                Assert.That(hazard.PhaseElapsed, Is.Zero, "Disabled timer must not advance.");

            // Exercise the scheduled path separately from the sandbox opening lineup.
            var config = new SerializedObject(spawner);
            config.FindProperty("spawnOpeningLineup").boolValue = false;
            config.FindProperty("spawnAtRoundStart").boolValue = false;
            config.FindProperty("intervalFrames").intValue = 1;
            config.ApplyModifiedPropertiesWithoutUndo();
            spawner.BeginRound(1, MatchRules.Default);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None), Is.Not.Empty,
                "Enabled periodic spawning must run even without an opening lineup.");
            // Starting a disabled round clears content and prevents subsequent scheduled spawns.
            spawner.BeginRound(1, new MatchRules(false, false));
            hazard.BeginRound(1, new MatchRules(false, false));
            yield return new WaitForSeconds(.2f);
            Assert.That(Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(hazard.Phase, Is.EqualTo(HazardPhase.Inactive));
            Assert.That(hazard.PhaseElapsed, Is.Zero);
            app.ReturnToMenu();
            yield return WaitFor(() => !app.IsLoading && app.State == AppState.MainMenu);
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ForcedRulesAreVisibleAndCannotBeToggledOff()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = Object.FindFirstObjectByType<AppStateMachine>();
        app.OpenSelection(MatchMode.Arcade);
        app.SelectBlobby();
        AssertLocked(app);
        app.ReturnToMenu();
        app.ReturnToMenu();
        app.OpenSelection();
        app.SelectBlobby();
        app.SetHazardsEnabled(false);
        app.SetItemsEnabled(false);
        var stage = app.GetStage(app.StageIndex);
        stage.IsBossStage = true;
        app.SelectStage(app.StageIndex);
        AssertLocked(app);
        stage.IsBossStage = false;
        app.SelectStage(app.StageIndex);
        Assert.That(app.Rules.HazardsEnabled || app.Rules.ItemsEnabled, Is.False,
            "Leaving a boss entry restores the requested Classic options.");
        yield return new ExitPlayMode();
    }

    private static void AssertLocked(AppStateMachine app)
    {
        app.SetHazardsEnabled(false);
        app.SetItemsEnabled(false);
        Assert.That(app.Rules.HazardsEnabled && app.Rules.ItemsEnabled, Is.True);
        foreach (var toggle in app.GetComponentsInChildren<Toggle>(true)
                     .Where(toggle => toggle.name == "Hazards Toggle" || toggle.name == "Items Toggle"))
        {
            Assert.That(toggle.isOn, Is.True);
            Assert.That(toggle.interactable, Is.False);
        }
    }

    private static IEnumerator WaitFor(System.Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 30f;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(condition(), Is.True, "Match flow timed out.");
    }
}
