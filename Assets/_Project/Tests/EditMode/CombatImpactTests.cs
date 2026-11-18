using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;
using WobbleBrawl.UI;

public sealed class CombatImpactTests
{
    [Test]
    public void PaletteDistinguishesCombatOutcomes()
    {
        var palette = JsonUtility.FromJson<ImpactPalette>(Resources.Load<TextAsset>("CombatImpactPalette").text);
        Assert.That(palette.styles.Length, Is.EqualTo(6));
        Assert.That(palette.styles.Select(style => style.text).Distinct().Count(), Is.EqualTo(6));
        var light = new DamageResult(5, 10, 3, false, false);
        Assert.That(palette.Classify(light, false, false), Is.EqualTo(ImpactKind.Light));
        Assert.That(palette.Classify(new DamageResult(12, 10, 3, false, false), false, false),
            Is.EqualTo(ImpactKind.Heavy));
        Assert.That(palette.Classify(light, false, true), Is.EqualTo(ImpactKind.Special));
        Assert.That(palette.Classify(new DamageResult(2, 5, 3, true, false), false, true),
            Is.EqualTo(ImpactKind.Block));
        Assert.That(palette.Classify(new DamageResult(10, 0, 3, false, true), false, false),
            Is.EqualTo(ImpactKind.GuardBreak));
        Assert.That(palette.Classify(light, true, false), Is.EqualTo(ImpactKind.Knockout));
    }

    [UnityTest]
    public IEnumerator HeavyAndKnockoutUseTheSameImpactGraphic()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatSandbox.unity");
        yield return new EnterPlayMode();
        yield return null;

        var ai = Object.FindFirstObjectByType<AIInput>();
        ai.TogglePause();
        var opponent = ai.GetComponent<Fighter>();
        var player = opponent.ItemOpponent;
        var popup = Object.FindObjectsByType<ComicImpactPopup>(FindObjectsSortMode.None)
            .Single(component => component.name == "Opponent Impact");
        var label = popup.GetComponent<Text>();
        var burst = popup.transform.parent.GetChild(popup.transform.GetSiblingIndex() - 1)
            .GetComponent<ComicImpactShape>();

        var strike = ScriptableObject.CreateInstance<MoveDef>();
        strike.HitStopFrames = 0;
        strike.BodyDamage = 14;
        var hit = new HitInfo(player, strike, HurtZone.Body, opponent.transform.position, Vector3.right);
        Assert.That(opponent.ReceiveHit(hit), Is.True);
        Assert.That(label.text, Is.EqualTo("POW!"));
        Assert.That(label.enabled && burst.enabled, Is.True);

        strike.BodyDamage = 1000;
        Assert.That(opponent.ReceiveHit(hit), Is.True);
        Assert.That(label.text, Is.EqualTo("KO!"));
        Assert.That(popup.transform.parent.GetChild(popup.transform.GetSiblingIndex() - 1)
            .GetComponent<ComicImpactShape>(), Is.SameAs(burst));
        Object.Destroy(strike);

        yield return new ExitPlayMode();
    }
}
