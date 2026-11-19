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
        TestEconomy.Install(app);
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
                if (app.GetStage(index).Scene == "BathtubBrawl")
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
            Assert.That(Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None), Is.Empty,
                "Real arenas wait for their first scheduled drop, regardless of Items toggle.");
            yield return new WaitForSeconds(.2f);
            Assert.That(hazard.Phase == HazardPhase.Inactive, Is.EqualTo(!hazards));
            if (!hazards)
                Assert.That(hazard.PhaseElapsed, Is.Zero, "Disabled timer must not advance.");

            // Shorten the authored interval to verify enabled/disabled scheduling without a long wait.
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
        TestEconomy.Install(app);
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

    [UnityTest]
    public IEnumerator ArenaDifficultyButtonsApplyProfilesAndKeepChoiceAcrossRounds()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = Object.FindFirstObjectByType<AppStateMachine>();
        TestEconomy.Install(app);
        foreach (var tier in new[] { AIDifficulty.Easy, AIDifficulty.Normal, AIDifficulty.Hard })
        {
            app.OpenSelection();
            Assert.That(app.Difficulty, Is.EqualTo(AIDifficulty.Normal));
            app.SetDifficulty(AIDifficulty.Hard);
            Assert.That(app.Difficulty, Is.EqualTo(AIDifficulty.Normal), "Only arena setup accepts changes.");
            app.SelectBlobby();
            var buttons = app.GetComponentsInChildren<Button>(true)
                .Where(button => button.name.StartsWith("Difficulty ")).ToArray();
            Assert.That(buttons.Length, Is.EqualTo(3));
            buttons.Single(button => button.name == "Difficulty " + tier).onClick.Invoke();
            Assert.That(app.Difficulty, Is.EqualTo(tier));
            app.SetDifficulty((AIDifficulty)999);
            Assert.That(app.Difficulty, Is.EqualTo(tier));
            app.SelectStage(Enumerable.Range(0, app.StageCount)
                .First(index => app.GetStage(index).Scene == "BathtubBrawl"));
            app.StartMatch();
            yield return WaitFor(() => !app.IsLoading && app.State == AppState.Match);
            yield return null;
            var ai = Object.FindFirstObjectByType<AIInput>();
            var expected = Resources.Load<WobbleBrawl.Data.AIProfile>("AI/" + tier);
            Assert.That(expected, Is.Not.Null);
            Assert.That(ai.Profile, Is.SameAs(expected));
            app.SetDifficulty(AIDifficulty.Normal);
            Assert.That(app.Difficulty, Is.EqualTo(tier), "In-match setup changes are ignored.");
            // Round configuration must retain the selected profile instead of restoring Normal.
            ai.ConfigureRound(null, null, 2);
            Assert.That(ai.Profile, Is.SameAs(expected));
            app.ReturnToMenu();
            yield return WaitFor(() => !app.IsLoading && app.State == AppState.MainMenu);
        }
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RealArenaIgnoresTestLineupAndSpawnsOneItemPerSeededInterval()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/BathtubBrawl.unity");
        yield return new EnterPlayMode();
        yield return null;
        var spawner = Object.FindFirstObjectByType<ItemSpawner>();
        var config = new SerializedObject(spawner);
        config.FindProperty("spawnAtRoundStart").boolValue = true;
        config.FindProperty("spawnOpeningLineup").boolValue = true;
        config.ApplyModifiedPropertiesWithoutUndo();
        var clock = typeof(ItemSpawner).GetField("remainingFrames",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var tick = typeof(ItemSpawner).GetMethod("FixedUpdate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        spawner.BeginRound(7);
        Assert.That(Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None), Is.Empty);
        int initial = (int)clock.GetValue(spawner);
        Assert.That(initial, Is.InRange(200, 400));
        for (int frame = 1; frame < initial; frame++) tick.Invoke(spawner, null);
        Assert.That(Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None), Is.Empty);
        tick.Invoke(spawner, null);
        var first = Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None);
        Assert.That(first.Length, Is.EqualTo(1));
        string selected = first[0].Definition.Id;
        Vector3 position = first[0].transform.position;
        int next = (int)clock.GetValue(spawner);
        Assert.That(next, Is.InRange(200, 400));
        spawner.BeginRound(7);
        Assert.That((int)clock.GetValue(spawner), Is.EqualTo(initial));
        for (int frame = 0; frame < initial; frame++) tick.Invoke(spawner, null);
        var repeated = Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None);
        Assert.That(repeated.Length, Is.EqualTo(1));
        Assert.That(repeated[0].Definition.Id, Is.EqualTo(selected));
        Assert.That(repeated[0].transform.position, Is.EqualTo(position));
        Assert.That((int)clock.GetValue(spawner), Is.EqualTo(next));
        spawner.BeginRound(8, new MatchRules(true, false));
        for (int frame = 0; frame < 401; frame++) tick.Invoke(spawner, null);
        Assert.That(Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None), Is.Empty);
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
