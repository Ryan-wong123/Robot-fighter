using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WobbleBrawl.Data;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;
using WobbleBrawl.UI;

public sealed class MatchHudTests
{
    [UnityTest]
    public IEnumerator HudTracksVitalsTimerAndRoundWins()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatSandbox.unity");
        yield return new EnterPlayMode();
        yield return null;

        var hud = Object.FindFirstObjectByType<MatchHud>();
        Assert.That(hud, Is.Not.Null);
        var match = hud.GetComponent<MatchController>();
        var round = hud.GetComponent<RoundController>();
        var ai = round.Opponent.GetComponent<AIInput>();
        ai.TogglePause();
        var hudRoot = hud.transform.Find("Match HUD Canvas/Safe Area");
        var playerSpecial = (RectTransform)hudRoot.Find("Player/Special/Fill");
        var opponentHealth = hudRoot.Find("Opponent/Health/Health Value").GetComponent<Text>();
        var opponentGuard = (RectTransform)hudRoot.Find("Opponent/Guard/Fill");
        var time = hudRoot.Find("Round Clock/Timer").GetComponent<Text>();
        var phase = hudRoot.Find("Round Clock/Round Phase").GetComponent<Text>();
        var firstWin = hudRoot.Find("Player/Win One").GetComponent<Image>();
        Color emptyPip = firstWin.color;

        Assert.That(phase.text, Is.EqualTo("ROUND 1"));
        Assert.That(opponentHealth.text, Is.EqualTo($"{round.Opponent.MaxHealth} / {round.Opponent.MaxHealth}"));
        string firstTime = time.text;
        float deadline = Time.realtimeSinceStartup + 3f;
        while (time.text == firstTime && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(int.Parse(time.text), Is.LessThan(int.Parse(firstTime)));

        var move = ScriptableObject.CreateInstance<MoveDef>();
        move.HitStopFrames = 0;
        move.GaugeGain = 30;
        var hit = new HitInfo(round.Player, move, HurtZone.Body, round.Opponent.transform.position, Vector3.right);
        typeof(Fighter).GetProperty("State").SetValue(round.Opponent, FighterState.Blocking);
        Assert.That(round.Opponent.ReceiveHit(hit), Is.True);
        Assert.That(opponentGuard.anchorMin.x, Is.EqualTo(.1f).Within(.001f));
        Assert.That(playerSpecial.anchorMax.x, Is.EqualTo(.3f).Within(.001f));
        Assert.That(opponentHealth.text, Is.EqualTo($"{round.Opponent.Health} / {round.Opponent.MaxHealth}"));

        move.BodyDamage = 10000;
        typeof(Fighter).GetProperty("State").SetValue(round.Opponent, FighterState.Idle);
        Assert.That(round.Opponent.ReceiveHit(hit), Is.True);
        Assert.That(firstWin.color, Is.Not.EqualTo(emptyPip));
        Assert.That(opponentHealth.text, Is.EqualTo($"0 / {round.Opponent.MaxHealth}"));
        deadline = Time.realtimeSinceStartup + 4f;
        while (match.RoundNumber < 2 && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(phase.text, Is.EqualTo("ROUND 2"));
        Assert.That(opponentGuard.anchorMin.x, Is.EqualTo(0f).Within(.001f));
        Assert.That(playerSpecial.anchorMax.x, Is.EqualTo(0f).Within(.001f));
        Assert.That(firstWin.color, Is.Not.EqualTo(emptyPip));
        Object.Destroy(move);
        yield return new ExitPlayMode();
    }
}
