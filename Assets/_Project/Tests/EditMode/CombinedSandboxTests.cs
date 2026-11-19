using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Core;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;
using Object = UnityEngine.Object;

public sealed class CombinedSandboxTests
{
    [TestCase("CombatSandbox", true, false)]
    [TestCase("NewItemSandbox", false, false)]
    [TestCase("BathtubBrawl", false, true)]
    [TestCase("KitchenCounter", false, true)]
    [TestCase("BouncyCastleFair", false, true)]
    [TestCase("GiantSandbox", false, true)]
    public void DevelopmentPolicy_SeparatesShortcutsFromPlayableArenas(string scene, bool shortcuts, bool selectable)
    {
        Assert.That(TrainingSandboxRules.AllowsShortcuts(scene), Is.EqualTo(shortcuts));
        Assert.That(TrainingSandboxRules.IsSelectableArena(scene), Is.EqualTo(selectable));
    }

    [UnityTest]
    public IEnumerator CombinedRack_PersistsDispensesReusesAndIgnoresTrainingTimeout()
    {
        Type.GetType("CombinedSandboxContent, Assembly-CSharp-Editor", true).GetMethod("Build").Invoke(null, null);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatSandbox.unity");
        yield return new EnterPlayMode();
        yield return null;
        var round = Object.FindFirstObjectByType<RoundController>();
        var spawner = Object.FindFirstObjectByType<ItemSpawner>();
        Assert.That(round.IsTraining, Is.True);
        Assert.That(spawner.HasPersistentRack, Is.True);
        var points = (Transform[])Field(spawner, "spawnPoints");
        Assert.That(points.Select(point => point.position).Distinct().Count(), Is.EqualTo(12));
        Assert.That(points.Max(point => point.position.x) - points.Min(point => point.position.x), Is.EqualTo(22f));
        round.Opponent.GetComponent<AIInput>().TogglePause();
        var displays = Object.FindObjectsByType<PooledItem>(FindObjectsSortMode.None)
            .Where(item => Field(item, "dispenser") != null).ToArray();
        Assert.That(displays.Length, Is.EqualTo(12));
        Assert.That(displays.Select(item => item.Definition.Id).Distinct().Count(), Is.EqualTo(12));
        Assert.That(displays.Max(item => item.transform.position.x) - displays.Min(item => item.transform.position.x),
            Is.EqualTo(22f).Within(.01f));
        foreach (var display in displays)
            display.GetType().GetField("remainingFrames", Private).SetValue(display, 1);
        round.GetType().GetField("deadline", Private).SetValue(round, Time.time - 1f);
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.That(round.Running, Is.True);
        Assert.That(displays.All(item => item.gameObject.activeSelf), Is.True);
        int index = Array.FindIndex((ItemSpawner.Entry[])Field(spawner, "table"),
            entry => entry.Item.Id == "wind-up-car");
        int before = spawner.GetComponentsInChildren<PooledItem>(true).Length;
        var holder = round.Player.GetComponent<ItemHolder>();
        for (int copy = 0; copy < 6; copy++)
        {
            ((int[])Field(spawner, "dispenseCooldown"))[index] = 0;
            Call(spawner, "TryDispense", index, round.Player);
            Assert.That(holder.HasItem, Is.True);
            Assert.That(displays.All(item => item.gameObject.activeSelf), Is.True);
            Call(holder, "TryUse");
        }
        Assert.That(spawner.GetComponentsInChildren<PooledItem>(true).Length, Is.EqualTo(before));
        spawner.EndRound();
        Assert.That(displays.All(item => item.gameObject.activeSelf), Is.True);
        spawner.BeginRound(1);
        Assert.That(displays.All(item => item.gameObject.activeSelf), Is.True);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator ArenaSelection_ExcludesBothDevelopmentScenes()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = Object.FindFirstObjectByType<AppStateMachine>();
        Assert.That(app.StageCount, Is.GreaterThanOrEqualTo(2));
        for (int index = 0; index < app.StageCount; index++)
            Assert.That(TrainingSandboxRules.IsSelectableArena(app.GetStage(index).Scene), Is.True);
        yield return new ExitPlayMode();
    }

    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object Field(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private).Invoke(target, args);
}
