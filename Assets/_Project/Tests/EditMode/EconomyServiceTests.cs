using System;
using System.IO;
using NUnit.Framework;
using WobbleBrawl.Core;

public sealed class EconomyServiceTests
{
    private string folder;

    [SetUp]
    public void Setup()
    {
        folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
    }

    [TearDown]
    public void Cleanup() => Directory.Delete(folder, true);

    [Test]
    public void EarnSpendAndReloadWithoutChargingTwiceOrLosingSettings()
    {
        var saves = new SaveService(Path.Combine(folder, "save.json"));
        var data = saves.Load();
        data.Settings.Music = .43f;
        var wallet = new EconomyService(saves, data);
        Assert.That(wallet.OwnsFighter("Blobby"), Is.True);
        Assert.That(wallet.TryUnlockFighter("SirNoodle"), Is.False);
        Assert.That(wallet.AwardMatch(false, false, false), Is.EqualTo(10));
        Assert.That(wallet.AwardMatch(false, true, false), Is.EqualTo(15));
        Assert.That(wallet.AwardMatch(true, false, true), Is.EqualTo(80));
        Assert.That(wallet.TryUnlockArena("BouncyCastleFair"), Is.True);
        Assert.That(wallet.TryUnlockArena("BouncyCastleFair"), Is.True);
        Assert.That(wallet.Balance, Is.EqualTo(30));
        Assert.That(wallet.AwardMatch(true, false, false), Is.EqualTo(30));
        Assert.That(wallet.TryUnlockFighter("SirNoodle"), Is.True);
        var loaded = saves.Load();
        var restored = new EconomyService(saves, loaded);
        Assert.That(restored.Balance, Is.Zero);
        Assert.That(restored.OwnsFighter("SirNoodle"), Is.True);
        Assert.That(restored.OwnsArena("BouncyCastleFair"), Is.True);
        Assert.That(loaded.Settings.Music, Is.EqualTo(.43f).Within(.001f));
    }

    [Test]
    public void FutureSaveRejectsRewardsAndRollsBackPurchases()
    {
        string path = Path.Combine(folder, "save.json");
        const string future = "{\"Version\":99,\"Coins\":700}";
        File.WriteAllText(path, future);
        var saves = new SaveService(path);
        var data = saves.Load();
        data.Coins = 100;
        var wallet = new EconomyService(saves, data);
        Assert.That(wallet.TryUnlockFighter("SirNoodle"), Is.False);
        Assert.That(wallet.OwnsFighter("SirNoodle"), Is.False);
        Assert.That(wallet.AwardMatch(true, false, false), Is.Zero);
        Assert.That(wallet.Balance, Is.EqualTo(100));
        Assert.That(File.ReadAllText(path), Is.EqualTo(future));
    }
}
