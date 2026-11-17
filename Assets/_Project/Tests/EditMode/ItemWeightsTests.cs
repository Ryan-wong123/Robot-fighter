using System;
using NUnit.Framework;
using WobbleBrawl.Gameplay;

public sealed class ItemWeightsTests
{
    [Test]
    public void SameSeedProducesSameSelections()
    {
        var first = new Random(42);
        var second = new Random(42);
        int[] weights = { 1, 0, 5, 3 };
        var counts = new int[weights.Length];
        for (int roll = 0; roll < 900; roll++)
        {
            int selected = ItemWeights.Select(weights, first);
            Assert.That(selected, Is.EqualTo(ItemWeights.Select(weights, second)));
            counts[selected]++;
        }

        Assert.That(counts[0], Is.InRange(65, 135));
        Assert.That(counts[1], Is.Zero);
        Assert.That(counts[2], Is.InRange(450, 550));
        Assert.That(counts[3], Is.InRange(255, 345));
    }

    [Test]
    public void NoAvailableWeightsReturnsNoSelection()
    {
        Assert.That(ItemWeights.Select(new[] { 0, -1 }, new Random(42)), Is.EqualTo(-1));
    }
}
