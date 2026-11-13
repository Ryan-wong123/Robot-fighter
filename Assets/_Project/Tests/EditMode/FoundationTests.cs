using NUnit.Framework;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

public class FoundationTests
{
    [TestCase(200f, 200f, false, false, RoundResult.Continue)]
    [TestCase(200f, 100f, true, false, RoundResult.PlayerWins)]
    [TestCase(0f, 100f, false, false, RoundResult.OpponentWins)]
    [TestCase(100f, 100f, true, false, RoundResult.SuddenDeath)]
    [TestCase(1f, 1f, true, true, RoundResult.Draw)]
    public void RoundResolvesByHealthAndTime(float player, float opponent, bool expired, bool suddenDeath, RoundResult expected)
        => Assert.That(RoundRules.Evaluate(player, opponent, expired, suddenDeath), Is.EqualTo(expected));

    [Test]
    public void BlockReducesDamageAndBreaksWhenGuardIsDepleted()
    {
        var guard = new BlockRules();
        Assert.That(guard.Resolve(20f, true, false, out bool blocked), Is.EqualTo(5f));
        Assert.That(blocked, Is.True);
        Assert.That(guard.Resolve(80f, true, false, out blocked), Is.EqualTo(80f));
        Assert.That(blocked, Is.False);
        Assert.That(guard.StunRemaining, Is.EqualTo(BlockRules.BreakDuration));
        guard.Advance(BlockRules.BreakDuration, false);
        Assert.That(guard.Resolve(10f, true, true, out blocked), Is.EqualTo(5f));
    }
}
