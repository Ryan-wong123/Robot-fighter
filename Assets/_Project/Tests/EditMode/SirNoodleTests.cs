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
    private const string Definition = "Assets/_Project/Resources/Characters/SirNoodle.asset";

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
        foreach (var fighter in new[] { round.Player, round.Opponent })
        {
            Assert.That(fighter.GetComponentInChildren<FighterVisual>(), Is.Not.Null);
            Assert.That(fighter.GetComponentsInChildren<Rigidbody>().Length, Is.EqualTo(13),
                "Twelve physical bones plus the gameplay motor; follow bones are visual only.");
            var spine = fighter.GetComponentsInChildren<Transform>().Single(bone => bone.name == "Spine");
            var rotation = spine.localRotation;
            fighter.SubmitTouchIntent(new FighterIntent { Punch = true });
            yield return new WaitForSeconds(.15f);
            Assert.That(Quaternion.Angle(rotation, spine.localRotation), Is.GreaterThan(.1f));
            fighter.ResetRound(fighter.transform.position, fighter.Facing);
            Assert.That(fighter.Health, Is.EqualTo(170));
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
