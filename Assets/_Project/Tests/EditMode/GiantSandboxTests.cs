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
using WobbleBrawl.UI;

public sealed class GiantSandboxTests
{
    [Test]
    public void SweptContact_CatchesFastAndReversedPassesButAllowsJumpAndBlock()
    {
        Assert.That(SandboxRules.PawContact(-2, 2, 0, .95f, 0, .85f, false), Is.True);
        Assert.That(SandboxRules.PawContact(2, -2, 0, .95f, 0, .85f, false), Is.True);
        Assert.That(SandboxRules.PawContact(-2, 2, 0, .95f, .85f, .85f, false), Is.False);
        Assert.That(SandboxRules.PawContact(-2, 2, 0, .95f, 0, .85f, true), Is.False);
        Assert.That(SandboxRules.PawContact(-2, -1, 2, .95f, 0, .85f, false), Is.False);
        Assert.That(SandboxRules.PawPosition(0, 8.5f, true), Is.EqualTo(-8.5f));
        Assert.That(SandboxRules.PawPosition(1, 8.5f, true), Is.EqualTo(8.5f));
        Assert.That(SandboxRules.PawPosition(0, 8.5f, false), Is.EqualTo(8.5f));
        Assert.That(SandboxRules.InSand(-4.35f, -3, 1.35f, .02f, .15f), Is.True);
        Assert.That(SandboxRules.InSand(-3, -3, 1.35f, 1, .15f), Is.False);
        Assert.That(SandboxRules.InSand(0, -3, 1.35f, .02f, .15f), Is.False);
    }

    [Test]
    public void InstalledStage_HasOriginalArtMetadataAudioAndNonDamagingPaw()
    {
        EnsureContent();
        var data = Resources.Load<StageDef>("Stages/GiantSandbox");
        Assert.That(data.Id, Is.EqualTo("giant-sandbox"));
        Assert.That(data.Thumbnail, Is.Not.Null);
        Assert.That(Application.CanStreamedLevelBeLoaded(data.SceneName), Is.True);
        var tuning = AssetDatabase.LoadAssetAtPath<GiantSandboxDef>(Art + "/SandboxTuning.asset");
        Assert.That(tuning.MovementMultiplier, Is.EqualTo(.5f));
        Assert.That(tuning.ItemPoints.Length, Is.EqualTo(3));
        var hit = AssetDatabase.LoadAssetAtPath<MoveDef>(Art + "/PawSweepHit.asset");
        Assert.That(hit.BodyDamage + hit.LegDamage + hit.GuardCost + hit.GaugeGain, Is.Zero);
        Assert.That(hit.KnocksDown, Is.True);
        foreach (string name in new[] { "SandMusic", "SandAmbience", "SandPurr", "SandSwirl", "SandThump" })
        {
            Assert.That(Resources.Load<AudioClip>("Audio/" + name), Is.Not.Null);
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/GiantSandbox.fbx");
        Assert.That(model.GetComponentsInChildren<MeshFilter>().Length, Is.EqualTo(5));
    }

    [UnityTest]
    public IEnumerator StageFlow_PawKnocksDownWithoutDamageBlockAvoidsAndSandResets()
    {
        EnsureContent();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = UnityEngine.Object.FindFirstObjectByType<AppStateMachine>();
        TestEconomy.Install(app);
        app.OpenSelection();
        app.SelectBlobby();
        int found = -1;
        for (int index = 0; index < app.StageCount; index++)
        {
            if (app.GetStage(index).Scene == "GiantSandbox")
            {
                Assert.That(found, Is.EqualTo(-1));
                found = index;
            }
        }
        Assert.That(found, Is.GreaterThanOrEqualTo(0));
        app.SelectStage(found);
        app.SetItemsEnabled(false);
        app.StartMatch();
        float deadline = Time.realtimeSinceStartup + 30;
        while (app.IsLoading && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
        Assert.That(app.IsLoading, Is.False);
        yield return null;
        var round = UnityEngine.Object.FindFirstObjectByType<RoundController>();
        var sandbox = UnityEngine.Object.FindFirstObjectByType<GiantSandboxHazards>();
        var cycle = sandbox.GetComponent<HazardController>();
        round.Opponent.GetComponent<AIInput>().TogglePause();
        var controls = UnityEngine.Object.FindFirstObjectByType<MatchTouchControls>();
        if (controls != null)
        {
            controls.enabled = false;
        }
        var body = round.Player.GetComponent<Rigidbody>();
        var target = round.Opponent.GetComponent<Rigidbody>();
        body.position = new Vector3(0f, .02f, 0f);
        target.position = new Vector3(3f, .02f, 0f);
        body.linearVelocity = target.linearVelocity = Vector3.zero;
        round.Opponent.SubmitTouchIntent(new FighterIntent { Block = true });
        int health = round.Player.Health;
        int guard = round.Opponent.Guard;
        var timing = ScriptableObject.CreateInstance<HazardDef>();
        timing.FirstWarningDelay = .04f;
        timing.WarningSeconds = HazardDef.MinimumWarningSeconds;
        timing.ActiveSeconds = 4f;
        timing.CooldownSeconds = .04f;
        timing.IdleSeconds = Vector2.one * .04f;
        cycle.EndRound();
        var properties = new SerializedObject(cycle);
        properties.FindProperty("definition").objectReferenceValue = timing;
        properties.ApplyModifiedPropertiesWithoutUndo();
        cycle.BeginRound(0);
        yield return WaitForPhase(cycle, HazardPhase.Active);
        yield return new WaitForSeconds(1.55f);
        Assert.That(round.Player.State, Is.EqualTo(FighterState.KnockedDown));
        Assert.That(round.Player.Health, Is.EqualTo(health));
        yield return new WaitForSeconds(.9f);
        Assert.That(round.Player.State, Is.Not.EqualTo(FighterState.KnockedDown));
        Assert.That(round.Opponent.State, Is.EqualTo(FighterState.Blocking));
        Assert.That(round.Opponent.Guard, Is.EqualTo(guard));
        yield return WaitForPhase(cycle, HazardPhase.Cooldown);
        yield return WaitForPhase(cycle, HazardPhase.Active);
        Assert.That(sandbox.IsSandCycle, Is.True);
        body.position = new Vector3(sandbox.SandCenter, .02f, 0f);
        body.linearVelocity = Vector3.zero;
        round.Player.SubmitTouchIntent(new FighterIntent { Move = .1f });
        for (int tick = 0; tick < 5; tick++)
        {
            yield return new WaitForFixedUpdate();
        }
        float slowSpeed = body.linearVelocity.x;
        body.position = new Vector3(5f, .02f, 0f);
        for (int tick = 0; tick < 5; tick++)
        {
            yield return new WaitForFixedUpdate();
        }
        Assert.That(body.linearVelocity.x, Is.EqualTo(slowSpeed * 2f).Within(.02f));
        Assert.That(round.Player.Health, Is.EqualTo(health));
        cycle.BeginRound(1, new MatchRules(false, false));
        Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Inactive));
        cycle.BeginRound(2);
        yield return WaitForPhase(cycle, HazardPhase.Active);
        Assert.That(sandbox.IsSandCycle, Is.False);
        cycle.BeginSuddenDeath();
        Assert.That(cycle.Phase, Is.EqualTo(HazardPhase.Inactive));
        UnityEngine.Object.Destroy(timing);
        yield return new ExitPlayMode();
    }

    private const string Art = "Assets/_Project/Art/GiantSandbox";

    private static IEnumerator WaitForPhase(HazardController cycle, HazardPhase phase)
    {
        float deadline = Time.realtimeSinceStartup + 6f;
        while (cycle.Phase != phase && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForFixedUpdate();
        }
        Assert.That(cycle.Phase, Is.EqualTo(phase));
    }

    private static void EnsureContent()
    {
        if (Resources.Load<StageDef>("Stages/GiantSandbox") == null)
        {
            Type.GetType("GiantSandboxContent, Assembly-CSharp-Editor", true).GetMethod("Build").Invoke(null, null);
        }
    }
}
