using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using WobbleBrawl.Core;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public sealed class SirNoodleTests
{
    private sealed class NeutralInput : IFighterInput
    {
        public FighterIntent ReadIntent() => default;
    }

    private const string Definition = "Assets/_Project/Resources/Characters/SirNoodle.asset";

    [Test]
    public void ExistingWhiteMaterialIsRepairedWithTheCorrectAtlas()
    {
        EnsureContent();
        var material = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/_Project/Art/Characters/SirNoodle/Materials/NoodleToon.mat");
        var expected = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/_Project/Art/Characters/SirNoodle/SirNoodle_Body_Albedo.png");
        var previous = material.GetTexture("_BaseMap");
        try
        {
            material.SetTexture("_BaseMap", null);
            Type.GetType("SirNoodleContent, Assembly-CSharp-Editor", true)
                .GetMethod("InstallIfMissing", System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static).Invoke(null, null);
            Assert.That(expected, Is.Not.Null);
            Assert.That(material.GetTexture("_BaseMap"), Is.EqualTo(expected));
        }
        finally
        {
            material.SetTexture("_BaseMap", previous != null ? previous : expected);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }
    }

    [Test]
    public void FrameScheduleHasFourSeparatedAlternatingPulses()
    {
        Assert.That(MoveHitSchedule.Pulse(11, 12, 24, 6), Is.EqualTo(-1));
        Assert.That(MoveHitSchedule.Pulse(36, 12, 24, 6), Is.EqualTo(-1));
        for (int frame = 12; frame < 36; frame++)
        {
            int pulse = MoveHitSchedule.Pulse(frame, 12, 24, 6);
            Assert.That(pulse, Is.EqualTo((frame - 12) / 6));
            Assert.That(MoveHitSchedule.Direction(1, pulse, true), Is.EqualTo(pulse % 2 == 0 ? 1 : -1));
            Assert.That(MoveHitSchedule.Direction(-1, pulse, true), Is.EqualTo(pulse % 2 == 0 ? -1 : 1));
            Assert.That(MoveHitSchedule.Pulse(frame, 12, 24, 0), Is.Zero, "Old moves remain single hit.");
        }
    }

    [Test]
    public void ImportedContentHasRequiredStatsRigMovesAndAnimations()
    {
        EnsureContent();
        var data = AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition);
        Assert.That(data.MaxHealth, Is.EqualTo(170));
        Assert.That(data.MaxGuard, Is.EqualTo(60));
        Assert.That(data.MoveSpeed, Is.EqualTo(3.2f));
        Assert.That(data.JumpSpeed, Is.EqualTo(10f));
        Assert.That(data.Prefab.GetComponent<Fighter>(), Is.Not.Null);
        Assert.That(data.VisualPrefab.GetComponent<FighterVisual>().ItemSocket, Is.Not.Null);
        Assert.That(data.VisualClips.Length, Is.EqualTo(22));
        foreach (var name in new[] { "Idle", "Walk", "Block", "JumpRise", "JumpFall", "JumpLand",
                     "Punch1", "Punch2", "Punch3", "Punch4", "Kick1", "Kick2", "Special", "Throw",
                     "GuardBreak", "HitLight", "HitHeavy", "Knockdown", "GetUp", "KO", "Win", "Taunt" })
        {
            var clip = data.VisualClips.Single(item => item.name == "SirNoodle_" + name);
            Assert.That(AnimationUtility.GetCurveBindings(clip), Is.Not.Empty, name + " must animate the rig.");
        }
        int punches = 0;
        for (var move = data.Punch; move != null; move = move.NextInCombo)
        {
            Assert.That(move.Clip, Is.Not.Null);
            Assert.That(move.HitboxOffset.x + move.HitboxSize.x * .5f, Is.EqualTo(2.03f).Within(.001f));
            Assert.That(move.BodyDamage, Is.EqualTo(9));
            punches++;
        }
        Assert.That(punches, Is.EqualTo(4));
        Assert.That(data.Kick.NextInCombo.NextInCombo, Is.Null);
        Assert.That(data.Special.Active / data.Special.HitIntervalFrames, Is.EqualTo(4));
        Assert.That(data.Special.AlternateSides, Is.True);
        Assert.That(data.Special.Recovery, Is.EqualTo(30));
        var skins = data.VisualPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
        Assert.That(skins.Length, Is.EqualTo(1));
        Assert.That(skins.Sum(skin => skin.sharedMesh.triangles.Length / 3), Is.LessThanOrEqualTo(12000));
        Assert.That(skins[0].sharedMaterials.Length, Is.EqualTo(2), "Toon surface plus outline.");
        Assert.That(skins[0].sharedMaterials[0].GetTexture("_BaseMap"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator SelectedFighterAndAIUseTheNewRigAndRetainItOnReset()
    {
        EnsureContent();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = UnityEngine.Object.FindFirstObjectByType<AppStateMachine>();
        TestEconomy.Install(app);
        app.OpenSelection();
        Assert.That(app.HasSirNoodle, Is.True);
        app.ToggleOpponent();
        app.SelectSirNoodle();
        app.SetItemsEnabled(false);
        app.SetHazardsEnabled(false);
        app.StartMatch();
        float deadline = Time.realtimeSinceStartup + 30f;
        while (app.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(app.IsLoading, Is.False);
        yield return null;
        var round = UnityEngine.Object.FindFirstObjectByType<RoundController>();
        Assert.That(round.Player.MaxHealth, Is.EqualTo(170));
        Assert.That(round.Opponent.MaxHealth, Is.EqualTo(170));
        Assert.That(round.Player.MaxGuard, Is.EqualTo(60));
        Assert.That(round.Opponent.GetComponent<AIInput>(), Is.Not.Null);
        var actors = new[] { round.Player, round.Opponent };
        var inputField = typeof(Fighter).GetField("input", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        var clipField = typeof(Fighter).GetField("playingClip", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        // This fixture checks both selected rigs. Prevent autonomous commands from consuming or
        // interrupting the single injected punch; decision behavior is covered by SandboxAITests.
        for (int index = 0; index < actors.Length; index++)
        {
            inputField.SetValue(actors[index], new NeutralInput());
            actors[index].ResetRound(new Vector3(index == 0 ? -3f : 3f, .02f, 0f), index == 0 ? 1 : -1);
        }
        Physics.SyncTransforms();
        foreach (var fighter in actors)
        {
            Assert.That(fighter.GetComponentInChildren<FighterVisual>(), Is.Not.Null);
            Assert.That(fighter.GetComponentsInChildren<Rigidbody>().Length, Is.EqualTo(13),
                "Twelve physical bones plus the gameplay motor; follow bones are visual only.");
            var visual = fighter.GetComponentInChildren<FighterVisual>();
            var spine = fighter.GetComponentsInChildren<Transform>().Single(bone => bone.name == "Spine");
            float readyDeadline = Time.realtimeSinceStartup + 2f;
            while (!Physics.Raycast(fighter.transform.position + Vector3.up * .15f, Vector3.down, .25f,
                       1 << 8, QueryTriggerInteraction.Ignore) && Time.realtimeSinceStartup < readyDeadline)
                yield return new WaitForFixedUpdate();
            Assert.That(Physics.Raycast(fighter.transform.position + Vector3.up * .15f, Vector3.down, .25f,
                1 << 8, QueryTriggerInteraction.Ignore), Is.True, "Rig probe needs a grounded fighter.");
            var rotation = spine.localRotation;
            fighter.SubmitTouchIntent(new FighterIntent { Punch = true });
            float attackDeadline = Time.realtimeSinceStartup + 1f;
            while (fighter.State != FighterState.Attacking && Time.realtimeSinceStartup < attackDeadline)
                yield return new WaitForFixedUpdate();
            Assert.That(fighter.State, Is.EqualTo(FighterState.Attacking),
                $"Punch was not accepted: team {fighter.Team}, clip {clipField.GetValue(fighter)}.");
            Assert.That(clipField.GetValue(fighter), Is.EqualTo("Punch1"));
            float greatestRotation = 0f;
            float motionDeadline = Time.realtimeSinceStartup + 1.5f;
            while (fighter.State == FighterState.Attacking && Time.realtimeSinceStartup < motionDeadline)
            {
                yield return null;
                greatestRotation = Mathf.Max(greatestRotation, Quaternion.Angle(rotation, spine.localRotation));
            }
            Assert.That(greatestRotation, Is.GreaterThan(.1f),
                $"Accepted Punch1 did not animate the spine: team {fighter.Team}, final clip {clipField.GetValue(fighter)}.");
            fighter.ResetRound(fighter.transform.position, fighter.Facing);
            Assert.That(fighter.Health, Is.EqualTo(170));
            Assert.That(fighter.GetComponentInChildren<FighterVisual>(), Is.SameAs(visual),
                "Reset must preserve the selected visual rig.");
        }
        yield return new ExitPlayMode();
    }

    private static void EnsureContent()
    {
        if (AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition) == null)
            Type.GetType("SirNoodleContent, Assembly-CSharp-Editor", true)
                .GetMethod("Build").Invoke(null, null);
    }
}
