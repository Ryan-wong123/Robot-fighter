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

public sealed class PuffinPeteTests
{
    private const string Definition = "Assets/_Project/Resources/Characters/PuffinPete.asset";

    [Test]
    public void ArmorRequiresFullGuardEntryAndLatchesUntilRelease()
    {
        var state = new InflationState(100, 25, 75, 30);
        state.SetBlocking(false, true);
        Assert.That(state.Inflated, Is.False);
        state.SetBlocking(true, false);
        Assert.That(state.Inflated, Is.False);
        state.SetBlocking(true, true);
        Assert.That(state.Inflated, Is.True);
        Assert.That(state.ReduceDamage(10), Is.EqualTo(7));
        Assert.That(state.ReduceDamage(3), Is.EqualTo(2));
        Assert.That(state.ReduceDamage(0), Is.Zero);
        Assert.That(state.RegisterHit(), Is.False);
        state.SetBlocking(true, false);
        Assert.That(state.Inflated, Is.True, "Guard drain does not cancel an established inflate.");
        state.SetBlocking(false, false);
        Assert.That(state.Inflated, Is.False);
        Assert.That(state.ReduceDamage(10), Is.EqualTo(10));
        state.Advance();
        Assert.That(state.Meter, Is.EqualTo(24));
    }

    [Test]
    public void FourthHitPopsAndRecoveryCannotBeBypassedByBlocking()
    {
        var state = new InflationState(100, 25, 75, 30);
        state.SetBlocking(true, true);
        for (int i = 0; i < 3; i++) Assert.That(state.RegisterHit(), Is.False);
        Assert.That(state.RegisterHit(), Is.True);
        Assert.That(state.Meter, Is.EqualTo(100));
        Assert.That(state.Inflated, Is.False);
        Assert.That(state.RemainingFrames, Is.EqualTo(75));
        Assert.That(state.RegisterHit(), Is.False, "Further hits do not restart recovery.");
        state.SetBlocking(true, true);
        Assert.That(state.Inflated, Is.False);
        for (int i = 0; i < 74; i++) state.Advance();
        Assert.That(state.Deflated, Is.True);
        state.Advance();
        Assert.That(state.Deflated, Is.False);
        Assert.That(state.Meter, Is.Zero);
        state.SetBlocking(true, true);
        Assert.That(state.Inflated, Is.True);
        state.Reset();
        Assert.That(state.Inflated || state.Deflated, Is.False);
    }

    [Test]
    public void ImportedPenguinHasMorphsCompleteActionsAndBumperData()
    {
        EnsureContent();
        var data = AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition);
        Assert.That(data.MaxHealth, Is.EqualTo(220));
        Assert.That(data.MaxGuard, Is.EqualTo(100));
        Assert.That(data.MoveSpeed, Is.EqualTo(2.6f));
        Assert.That(data.JumpSpeed, Is.EqualTo(8f));
        Assert.That(data.HasInflation, Is.True);
        Assert.That(data.VisualClips.Length, Is.EqualTo(24));
        Assert.That(data.VisualPrefab.GetComponent<InflationVisual>(), Is.Not.Null);
        var skin = data.VisualPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.That(skin.sharedMesh.blendShapeCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(skin.sharedMesh.triangles.Length / 3, Is.LessThanOrEqualTo(12000));
        Assert.That(skin.sharedMaterials[0].GetTexture("_BaseMap"), Is.EqualTo(
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Characters/PuffinPete/PuffinPete_Body_Albedo.png")));
        foreach (var clip in data.VisualClips)
            Assert.That(AnimationUtility.GetCurveBindings(clip), Is.Not.Empty, clip.name);
        Assert.That(data.Punch.HitboxOffset.x + data.Punch.HitboxSize.x * .5f, Is.EqualTo(1.305f).Within(.001f));
        Assert.That(data.Special.Startup, Is.EqualTo(18));
        Assert.That(data.Special.ForwardSpeed, Is.EqualTo(2.2f));
        Assert.That(data.Special.HopSpeed, Is.EqualTo(2.5f));
        Assert.That(data.Special.AlternateSides, Is.False);
        Assert.That(data.Prefab.GetComponent<Fighter>(), Is.Not.Null);
        Assert.That(Resources.Load<AudioClip>("Audio/PetePop"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator BlockingResistsPushThenPopsZipsRefillsAndResets()
    {
        EnsureContent();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = UnityEngine.Object.FindFirstObjectByType<AppStateMachine>();
        TestEconomy.Install(app);
        app.OpenSelection();
        Assert.That(app.HasPuffinPete, Is.True);
        app.SelectPuffinPete();
        app.SetItemsEnabled(false);
        app.SetHazardsEnabled(false);
        app.StartMatch();
        float deadline = Time.realtimeSinceStartup + 30f;
        while (app.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(app.IsLoading, Is.False);
        yield return null;
        var round = UnityEngine.Object.FindFirstObjectByType<RoundController>();
        var fighter = round.Player;
        round.Opponent.GetComponent<AIInput>().TogglePause();
        var move = ScriptableObject.CreateInstance<MoveDef>();
        move.BodyDamage = 20;
        move.GuardCost = 10;
        move.HitStopFrames = move.StunFrames = 0;
        move.Pushback = 4f;
        fighter.SubmitTouchIntent(new FighterIntent { Block = true });
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(fighter.IsInflated, Is.True);
        int health = fighter.Health;
        var hit = new HitInfo(round.Opponent, move, HurtZone.Body, fighter.transform.position, Vector3.left);
        Assert.That(fighter.ReceiveHit(hit), Is.True);
        Assert.That(health - fighter.Health, Is.EqualTo(3), "Block's 5 damage is reduced by inflated armor.");
        Assert.That(fighter.GetComponent<Rigidbody>().linearVelocity.x, Is.EqualTo(0f).Within(.01f));
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForFixedUpdate();
            fighter.SubmitTouchIntent(new FighterIntent { Block = true });
            Assert.That(fighter.ReceiveHit(hit), Is.True);
        }
        Assert.That(fighter.IsDeflated, Is.True);
        fighter.SubmitTouchIntent(new FighterIntent { Jump = true, Punch = true });
        yield return new WaitForFixedUpdate();
        Assert.That(fighter.State, Is.EqualTo(FighterState.Deflated));
        Assert.That(Mathf.Abs(fighter.GetComponent<Rigidbody>().linearVelocity.x), Is.GreaterThan(0f));
        for (int i = 0; i < 76; i++) yield return new WaitForFixedUpdate();
        Assert.That(fighter.IsDeflated, Is.False);
        Assert.That(fighter.PopMeter, Is.Zero);
        fighter.ResetRound(new Vector3(-2f, .02f, 0), 1);
        Assert.That(fighter.Health, Is.EqualTo(220));
        Assert.That(fighter.IsInflated || fighter.IsDeflated, Is.False);
        Assert.That(fighter.PopMeter, Is.Zero);
        fighter.SubmitTouchIntent(new FighterIntent { Block = true });
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        move.GuardCost = 100;
        int beforeBreak = fighter.Health;
        Assert.That(fighter.ReceiveHit(hit), Is.True);
        Assert.That(beforeBreak - fighter.Health, Is.EqualTo(20), "A guard-breaking hit deals full damage.");
        Assert.That(fighter.IsInflated, Is.False);
        UnityEngine.Object.Destroy(move);
        yield return new ExitPlayMode();
    }

    private static void EnsureContent()
    {
        if (AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition) == null)
            Type.GetType("PuffinPeteContent, Assembly-CSharp-Editor", true).GetMethod("Build").Invoke(null, null);
    }
}
