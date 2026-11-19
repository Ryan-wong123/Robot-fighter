using System;
using System.Collections;
using System.Reflection;
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

public sealed class MrJackTests
{
    private const string Definition = "Assets/_Project/Resources/Characters/MrJack.asset";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private sealed class NeutralInput : IFighterInput { public FighterIntent ReadIntent() => default; }

    [TestCase(320, false)]
    [TestCase(160, false)]
    [TestCase(159, true)]
    [TestCase(1, true)]
    [TestCase(0, false)]
    public void PhaseRequiresLivingHealthStrictlyBelowHalf(int health, bool expected) =>
        Assert.That(BossPhaseRules.IsSecondPhase(health, 320), Is.EqualTo(expected));

    [Test]
    public void BossAssetsContainBothMoveSetsAndFortyFiveAnimatedClips()
    {
        EnsureContent();
        var data = AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition);
        Assert.That(data.Prefab.GetComponent<AIInput>(), Is.Not.Null);
        Assert.That(data.VisualClips.Count(clip => clip.isLooping), Is.EqualTo(8));
        Assert.That(data.Punch.NextInCombo.NextInCombo.NextInCombo.GroundShockwave, Is.True);
        Assert.That(data.MaxHealth, Is.EqualTo(320));
        Assert.That(data.MaxGuard, Is.EqualTo(120));
        Assert.That(data.MoveSpeed, Is.EqualTo(2.8f));
        Assert.That(data.Boss.SecondMoveSpeed, Is.EqualTo(4.2f));
        Assert.That(data.VisualClips.Length, Is.EqualTo(45));
        Assert.That(data.VisualPrefab.GetComponent<BossPresentation>(), Is.Not.Null);
        Assert.That(data.VisualPrefab.GetComponentsInChildren<SkinnedMeshRenderer>().Length, Is.EqualTo(1));
        Assert.That(data.Boss.Profile.ReactionFrames, Is.EqualTo(3));
        foreach (var clip in data.VisualClips) Assert.That(AnimationUtility.GetCurveBindings(clip), Is.Not.Empty, clip.name);
        foreach (var root in new[] { data.Punch, data.Boss.SecondPunch })
        {
            int count = 0;
            for (var move = root; move != null; move = move.NextInCombo) { count++; Assert.That(count, Is.LessThanOrEqualTo(4)); }
            Assert.That(count, Is.EqualTo(4));
        }
        Assert.That(data.Boss.SecondPunch.Duration, Is.LessThan(data.Punch.Duration));
        Assert.That(data.Special.Startup, Is.EqualTo(75));
        Assert.That(data.Special.Active, Is.EqualTo(5));
        Assert.That(data.Special.KnocksDown, Is.True);
        Assert.That(Resources.Load<AudioClip>("Audio/MrJackLaugh"), Is.Not.Null);
        Assert.That(Resources.Load<AudioClip>("Audio/MrJackMusicBox"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator BossSelectionPhaseSpecialMarkerAndResetUseSharedCombat()
    {
        EnsureContent();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Boot.unity");
        yield return new EnterPlayMode();
        yield return null;
        var app = UnityEngine.Object.FindFirstObjectByType<AppStateMachine>();
        TestEconomy.Install(app);
        app.OpenSelection();
        typeof(AppStateMachine).GetMethod("SelectFighter", Private).Invoke(app,
            new object[] { Resources.Load<CharacterDef>("Characters/MrJack") });
        Assert.That(app.Step, Is.EqualTo(SelectionStep.Fighter), "Boss cannot be selected as player.");
        for (int i = 0; i < 5 && app.OpponentName != "Mr. Jack"; i++) app.ToggleOpponent();
        Assert.That(app.OpponentName, Is.EqualTo("Mr. Jack"));
        app.SelectBlobby();
        Assert.That(app.RulesLocked, Is.True);
        app.SetDifficulty(AIDifficulty.Easy);
        Assert.That(app.Difficulty, Is.EqualTo(AIDifficulty.Normal), "Boss AI ignores ordinary tier selection.");
        app.SetItemsEnabled(false);
        Assert.That(app.Rules.ItemsEnabled, Is.True);
        app.StartMatch();
        float deadline = Time.realtimeSinceStartup + 30f;
        while (app.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        yield return null;
        Assert.That(app.IsLoading, Is.False);
        var round = UnityEngine.Object.FindFirstObjectByType<RoundController>();
        var boss = round.Opponent;
        Assert.That(boss.Boss, Is.Not.Null);
        var ai = boss.GetComponent<AIInput>();
        Assert.That(ai.Profile, Is.SameAs(boss.Boss.Profile));
        typeof(Fighter).GetField("input", Private).SetValue(boss, new NeutralInput());
        boss.ResetRound(new Vector3(-2f, .02f, 0f), 1);
        round.Player.ResetRound(new Vector3(2f, .02f, 0f), -1);
        var vitals = typeof(Fighter).GetMethod("SetVitals", Private);
        vitals.Invoke(boss, new object[] { 160, 120, 100, true });
        Assert.That(boss.IsBossSecondPhase, Is.False);
        vitals.Invoke(boss, new object[] { 159, 120, 100, true });
        Assert.That(boss.IsBossSecondPhase, Is.True);
        Assert.That(boss.Boss.SecondPunch.BodyDamage, Is.EqualTo(11), "Shared data stays unchanged.");
        boss.ResetRound(new Vector3(-2f, .02f, 0f), 1);
        Assert.That(boss.IsBossSecondPhase, Is.False);
        Physics.SyncTransforms();
        boss.SubmitTouchIntent(new FighterIntent { Special = true });
        var tick = typeof(Fighter).GetMethod("FixedUpdate", Private);
        tick.Invoke(boss, null);
        Assert.That(boss.IsBossSpecialWarning, Is.True);
        float marked = boss.BossTargetX;
        round.Player.GetComponent<Rigidbody>().position = new Vector3(-4f, .02f, 0f);
        for (int i = 0; i < 76; i++) tick.Invoke(boss, null);
        Assert.That(boss.transform.position.x, Is.EqualTo(marked).Within(.01f), "Target is fixed during the warning.");
        Assert.That(boss.IsBossSpecialWarning, Is.False);
        boss.ResetRound(new Vector3(-2f, .02f, 0f), 1);
        Assert.That(boss.Health, Is.EqualTo(320));
        Assert.That(boss.IsBossSecondPhase || boss.IsBossSpecialWarning, Is.False);
        boss.SubmitTouchIntent(new FighterIntent { Special = true });
        tick.Invoke(boss, null);
        Assert.That(boss.IsBossSpecialWarning, Is.True);
        var poke = ScriptableObject.CreateInstance<MoveDef>();
        poke.BodyDamage = 1; poke.GuardCost = 0; poke.HitStopFrames = 0; poke.Pushback = 0;
        Assert.That(boss.ReceiveHit(new HitInfo(round.Player, poke, HurtZone.Body, boss.transform.position, Vector3.right)), Is.True);
        Assert.That(boss.IsBossSpecialWarning, Is.False, "Unblocked hits interrupt the telegraph.");
        UnityEngine.Object.Destroy(poke);
        yield return new ExitPlayMode();
    }

    private static void EnsureContent()
    {
        if (AssetDatabase.LoadAssetAtPath<CharacterDef>(Definition) == null)
            Type.GetType("MrJackContent, Assembly-CSharp-Editor", true).GetMethod("Build").Invoke(null, null);
    }
}
