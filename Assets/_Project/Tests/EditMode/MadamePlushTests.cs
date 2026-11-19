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

public sealed class MadamePlushTests
{
    private const string Definition = "Assets/_Project/Resources/Characters/MadamePlush.asset";

    [TestCase(240, 0)]
    [TestCase(181, 0)]
    [TestCase(180, 1)]
    [TestCase(121, 1)]
    [TestCase(120, 2)]
    [TestCase(61, 2)]
    [TestCase(60, 3)]
    [TestCase(0, 3)]
    public void SeamsUseInclusiveIntegerHealthThresholds(int health, int expected)
    {
        Assert.That(StuffingRules.Stage(health, 240), Is.EqualTo(expected));
    }

    [Test]
    public void ToughBonusUsesCurrentHealthAndNearestIntegerRounding()
    {
        Assert.That(StuffingRules.Damage(12, 61, 240, 10), Is.EqualTo(12));
        Assert.That(StuffingRules.Damage(12, 60, 240, 10), Is.EqualTo(13));
        Assert.That(StuffingRules.Damage(24, 60, 240, 10), Is.EqualTo(26));
        Assert.That(StuffingRules.Damage(100, 1, 240, 10), Is.EqualTo(110));
        Assert.That(StuffingRules.Damage(12, 0, 240, 10), Is.EqualTo(12));
        Assert.That(StuffingRules.Damage(0, 60, 240, 10), Is.Zero);
        Assert.That(StuffingRules.Stage(75, 99), Is.Zero);
        Assert.That(StuffingRules.Stage(74, 99), Is.EqualTo(1));
    }

    [Test]
    public void ImportedBearHasAllClipsStuffingShapesAndHeavyMoveData()
    {
        EnsureContent();
        var data = AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition);
        Assert.That(data.MaxHealth, Is.EqualTo(240));
        Assert.That(data.MaxGuard, Is.EqualTo(100));
        Assert.That(data.MoveSpeed, Is.EqualTo(2.4f));
        Assert.That(data.JumpSpeed, Is.EqualTo(9f));
        Assert.That(data.HasStuffingLoss, Is.True);
        Assert.That(data.HasInflation, Is.False);
        Assert.That(data.VisualClips.Length, Is.EqualTo(22));
        Assert.That(data.VisualPrefab.GetComponent<StuffingVisual>(), Is.Not.Null);
        var skin = data.VisualPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.That(skin.sharedMesh.blendShapeCount, Is.GreaterThanOrEqualTo(3));
        Assert.That(skin.sharedMesh.triangles.Length / 3, Is.LessThanOrEqualTo(12000));
        Assert.That(skin.sharedMaterials[0].GetTexture("_BaseMap"), Is.EqualTo(
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Characters/MadamePlush/MadamePlush_Body_Albedo.png")));
        foreach (var clip in data.VisualClips)
        {
            Assert.That(AnimationUtility.GetCurveBindings(clip), Is.Not.Empty, clip.name);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                Assert.That(binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal), Is.False,
                    "Health-driven stuffing must not be overwritten by sampled clips.");
        }
        Assert.That(data.Punch.BodyDamage, Is.EqualTo(12));
        Assert.That(data.Kick.BodyDamage, Is.EqualTo(14));
        Assert.That(data.Punch.HitboxOffset.x + data.Punch.HitboxSize.x * .5f, Is.EqualTo(1.305f).Within(.001f));
        Assert.That(data.Special.Startup, Is.EqualTo(24));
        Assert.That(data.Special.LaunchSpeed, Is.EqualTo(7f));
        Assert.That(data.Special.Pushback, Is.EqualTo(8f));
        Assert.That(Resources.Load<AudioClip>("Audio/PlushCreak"), Is.Not.Null);
        Assert.That(Resources.Load<AudioClip>("Audio/PlushWinBell"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator ThresholdDamageLaunchAndRoundResetUseTheSharedFighter()
    {
        EnsureContent();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = UnityEngine.Object.FindFirstObjectByType<AppStateMachine>();
        app.OpenSelection();
        Assert.That(app.HasMadamePlush, Is.True);
        app.SelectMadamePlush();
        app.SetItemsEnabled(false);
        app.SetHazardsEnabled(false);
        app.StartMatch();
        float deadline = Time.realtimeSinceStartup + 30f;
        while (app.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(app.IsLoading, Is.False);
        yield return null;
        var round = UnityEngine.Object.FindFirstObjectByType<RoundController>();
        var fighter = round.Player;
        var target = round.Opponent;
        target.GetComponent<AIInput>().TogglePause();
        var incoming = ScriptableObject.CreateInstance<MoveDef>();
        incoming.BodyDamage = 180;
        incoming.StunFrames = incoming.HitStopFrames = 0;
        incoming.Pushback = 0;
        Assert.That(fighter.ReceiveHit(new HitInfo(target, incoming, HurtZone.Body,
            fighter.transform.position, Vector3.left)), Is.True);
        Assert.That(fighter.Health, Is.EqualTo(60));
        Assert.That(fighter.StuffingStage, Is.EqualTo(3));
        Assert.That(fighter.IsTough, Is.True);
        Assert.That(fighter.AttackDamage(24), Is.EqualTo(26));
        yield return new WaitForSeconds(.25f);
        var skin = fighter.GetComponentInChildren<StuffingVisual>().GetComponentInChildren<SkinnedMeshRenderer>();
        for (int shape = 0; shape < skin.sharedMesh.blendShapeCount; shape++)
            Assert.That(skin.GetBlendShapeWeight(shape), Is.GreaterThan(90f));
        var data = AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition);
        var uppercut = UnityEngine.Object.Instantiate(data.Special);
        uppercut.HitStopFrames = 0;
        target.GetComponent<Rigidbody>().position = new Vector3(6.1f, .02f, 0);
        Assert.That(target.ReceiveHit(new HitInfo(fighter, uppercut, HurtZone.Body,
            target.transform.position, Vector3.right)), Is.True);
        Assert.That(target.GetComponent<Rigidbody>().linearVelocity.y, Is.GreaterThanOrEqualTo(7f));
        Assert.That(target.Health, Is.EqualTo(target.MaxHealth - 26));
        for (int tick = 0; tick < 10; tick++) yield return new WaitForFixedUpdate();
        Assert.That(target.transform.position.x, Is.LessThanOrEqualTo(6.21f));
        fighter.ResetRound(new Vector3(-2f, .02f, 0), 1);
        Assert.That(fighter.Health, Is.EqualTo(240));
        Assert.That(fighter.StuffingStage, Is.Zero);
        Assert.That(fighter.IsTough, Is.False);
        for (int shape = 0; shape < skin.sharedMesh.blendShapeCount; shape++)
            Assert.That(skin.GetBlendShapeWeight(shape), Is.Zero);
        UnityEngine.Object.Destroy(incoming);
        UnityEngine.Object.Destroy(uppercut);
        yield return new ExitPlayMode();
    }

    private static void EnsureContent()
    {
        if (AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition) == null)
            Type.GetType("MadamePlushContent, Assembly-CSharp-Editor", true).GetMethod("Build").Invoke(null, null);
    }
}
