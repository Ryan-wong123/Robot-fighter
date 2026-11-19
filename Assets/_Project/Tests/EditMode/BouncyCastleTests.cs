using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Core;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public sealed class BouncyCastleTests
{
    [Test]
    public void HazardGeometry_RespectsWidthPadEdgesAndSymmetricRecovery()
    {
        for (int tick = 0; tick <= 1000; tick++)
        {
            float progress = tick / 1000f;
            float weight = FairgroundRules.Squeeze(progress, .25f);
            float width = FairgroundRules.HalfWidth(7f, .25f, weight);
            Assert.That(width, Is.InRange(5.25f, 7f));
            Assert.That(weight, Is.EqualTo(FairgroundRules.Squeeze(1f - progress, .25f)).Within(.00001f));
        }
        Assert.That(FairgroundRules.OnPad(-4f, .02f, -3f, 1f, .15f), Is.True);
        Assert.That(FairgroundRules.OnPad(-1.99f, .02f, -3f, 1f, .15f), Is.False);
        Assert.That(FairgroundRules.OnPad(-3f, 1f, -3f, 1f, .15f), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => FairgroundRules.Squeeze(.5f, 0f));
    }

    [TestCase(0f, 0f)]
    [TestCase(.125f, .5f)]
    [TestCase(.25f, 1f)]
    [TestCase(.5f, 1f)]
    [TestCase(.75f, 1f)]
    [TestCase(.875f, .5f)]
    [TestCase(1f, 0f)]
    public void Squeeze_EasesInHoldsAndReturns(float progress, float expected)
    {
        float weight = FairgroundRules.Squeeze(progress, .25f);
        Assert.That(weight, Is.EqualTo(expected).Within(.001f));
        Assert.That(FairgroundRules.HalfWidth(7f, .25f, weight),
            Is.EqualTo(7f * (1f - .25f * expected)).Within(.001f));
    }

    [Test]
    public void PadContact_RequiresFeetOnTheMarkedSection()
    {
        Assert.That(FairgroundRules.OnPad(-3f, .02f, -3f, 1f, .15f), Is.True);
        Assert.That(FairgroundRules.OnPad(-2f, .15f, -3f, 1f, .15f), Is.True);
        Assert.That(FairgroundRules.OnPad(-1.99f, 0f, -3f, 1f, .15f), Is.False);
        Assert.That(FairgroundRules.OnPad(-3f, .16f, -3f, 1f, .15f), Is.False);
        Assert.That(FairgroundRules.OnPad(-3f, -1f, -3f, 1f, .15f), Is.False);
    }

    [Test]
    public void InstalledStage_HasThumbnailAtlasBoundsAndOriginalCues()
    {
        EnsureContent();
        var entry = Resources.Load<StageDef>("Stages/BouncyCastleFair");
        Assert.That(entry.Id, Is.EqualTo("bouncy-castle-fair"));
        Assert.That(entry.Thumbnail, Is.Not.Null);
        var tuning = AssetDatabase.LoadAssetAtPath<FairgroundDef>(
            "Assets/_Project/Art/BouncyCastleFair/FairgroundTuning.asset");
        Assert.That(tuning.HalfWidth, Is.EqualTo(7f));
        Assert.That(tuning.WidthReduction, Is.EqualTo(.25f));
        Assert.That(tuning.JumpMultiplier, Is.EqualTo(1.8f));
        Assert.That(tuning.PadCenters, Is.EqualTo(new[] { -3f, 3f }));
        Assert.That(tuning.ItemPoints.Length, Is.EqualTo(3));
        var material = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/_Project/Art/BouncyCastleFair/Materials/CastleToon.mat");
        Assert.That(material.GetTexture("_BaseMap"), Is.Not.Null);
        foreach (string name in new[] { "FairMusic", "FairAmbience", "FairPadWarning", "FairPump", "FairBoing" })
        {
            Assert.That(Resources.Load<AudioClip>("Audio/" + name), Is.Not.Null);
        }
        Assert.That(Application.CanStreamedLevelBeLoaded("BouncyCastleFair"), Is.True);
    }

    [UnityTest]
    public IEnumerator StageFlow_BouncesSqueezesWithoutDamageAndResetsWhenDisabled()
    {
        EnsureContent();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = UnityEngine.Object.FindFirstObjectByType<AppStateMachine>();
        TestEconomy.Install(app);
        app.OpenSelection();
        app.SelectBlobby();
        int stage = -1;
        for (int index = 0; index < app.StageCount; index++)
        {
            if (app.GetStage(index).Scene == "BouncyCastleFair")
            {
                Assert.That(stage, Is.EqualTo(-1), "Stage discovery must not duplicate an existing entry.");
                stage = index;
            }
        }
        Assert.That(stage, Is.GreaterThanOrEqualTo(0));
        app.SelectStage(stage);
        app.SetItemsEnabled(false);
        app.StartMatch();
        float deadline = Time.realtimeSinceStartup + 30f;
        while (app.IsLoading && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
        Assert.That(app.IsLoading, Is.False);
        yield return null;
        var round = UnityEngine.Object.FindFirstObjectByType<RoundController>();
        var fairground = UnityEngine.Object.FindFirstObjectByType<FairgroundHazards>();
        var cycle = fairground.GetComponent<HazardController>();
        Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Idle));
        round.Opponent.GetComponent<AIInput>().TogglePause();
        int health = round.Player.Health;
        var body = round.Player.GetComponent<Rigidbody>();
        body.position = new Vector3(-3f, .02f, 0f);
        body.linearVelocity = Vector3.zero;
        var timing = ScriptableObject.CreateInstance<HazardDef>();
        timing.FirstWarningDelay = .04f;
        timing.WarningSeconds = HazardDef.MinimumWarningSeconds;
        timing.ActiveSeconds = .8f;
        timing.CooldownSeconds = .04f;
        timing.IdleSeconds = Vector2.one * .04f;
        cycle.EndRound();
        var properties = new SerializedObject(cycle);
        properties.FindProperty("definition").objectReferenceValue = timing;
        properties.ApplyModifiedPropertiesWithoutUndo();
        cycle.BeginRound(0);
        yield return WaitForPhase(cycle, HazardPhase.Active);
        for (int tick = 0; tick < 4; tick++)
        {
            yield return new WaitForFixedUpdate();
        }
        Assert.That(fairground.PadsActive, Is.True);
        Assert.That(body.linearVelocity.y, Is.GreaterThan(12f), "A pad must auto-launch a standing fighter.");
        Assert.That(round.Player.Health, Is.EqualTo(health));
        yield return WaitForPhase(cycle, HazardPhase.Cooldown);
        yield return WaitForPhase(cycle, HazardPhase.Telegraph);
        body.position = new Vector3(-6.4f, .02f, 0f);
        body.linearVelocity = Vector3.zero;
        var other = round.Opponent.GetComponent<Rigidbody>();
        other.position = new Vector3(6.4f, .02f, 0f);
        other.linearVelocity = Vector3.zero;
        yield return WaitForPhase(cycle, HazardPhase.Active);
        yield return new WaitForSeconds(.3f);
        Assert.That(fairground.CurrentHalfWidth, Is.EqualTo(5.25f).Within(.05f));
        Assert.That(body.position.x, Is.GreaterThan(-5.25f));
        Assert.That(other.position.x, Is.LessThan(5.25f));
        Assert.That(round.Player.Health, Is.EqualTo(health));
        cycle.BeginRound(1, new MatchRules(false, false));
        Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Inactive));
        Assert.That(fairground.CurrentHalfWidth, Is.EqualTo(7f));
        Assert.That(fairground.PadsActive, Is.False);
        cycle.BeginRound(2);
        yield return WaitForPhase(cycle, HazardPhase.Active);
        Assert.That(fairground.PadsActive, Is.True, "Every round starts with trampoline pads.");
        cycle.BeginSuddenDeath();
        Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Inactive));
        Assert.That(fairground.CurrentHalfWidth, Is.EqualTo(7f));
        UnityEngine.Object.Destroy(timing);
        yield return new ExitPlayMode();
    }

    private static IEnumerator WaitForPhase(HazardController cycle, HazardPhase phase)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (cycle.Phase != phase && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForFixedUpdate();
        }
        Assert.That(cycle.Phase, Is.EqualTo(phase));
    }

    private static void EnsureContent()
    {
        if (Resources.Load<StageDef>("Stages/BouncyCastleFair") == null)
        {
            Type.GetType("BouncyCastleContent, Assembly-CSharp-Editor", true).GetMethod("Build").Invoke(null, null);
        }
    }
}
