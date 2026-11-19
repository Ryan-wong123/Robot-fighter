using NUnit.Framework;
using WobbleBrawl.Core;

public sealed class ArcadeLadderTests
{
    [Test]
    public void LadderRetriesLossesAndClearsOnlyAfterWinningTheFinale()
    {
        var ladder = new ArcadeLadder();
        string[] expected = { "BathtubBrawl", "KitchenCounter", "BouncyCastleFair", "GiantSandbox", "ToyChestShowdown" };
        AIDifficulty[] tiers = { AIDifficulty.Easy, AIDifficulty.Normal, AIDifficulty.Hard, AIDifficulty.Hard, AIDifficulty.Hard };
        Assert.That(ArcadeLadder.Count, Is.EqualTo(expected.Length));
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(ladder.Scene, Is.EqualTo(expected[index]));
            Assert.That(ladder.Difficulty, Is.EqualTo(tiers[index]));
            ladder.RecordResult(false);
            ladder.ContinueAfter(false);
            Assert.That(ladder.Index, Is.EqualTo(index));
            Assert.That(ladder.IsCleared, Is.False);
            ladder.RecordResult(true);
            Assert.That(ladder.IsCleared, Is.EqualTo(index == expected.Length - 1));
            ladder.ContinueAfter(true);
        }
        Assert.That(ladder.Scene, Is.EqualTo(ArcadeLadder.FinaleScene));
        ladder.Reset();
        Assert.That(ladder.Index, Is.Zero);
        Assert.That(ladder.IsCleared, Is.False);
    }
}
