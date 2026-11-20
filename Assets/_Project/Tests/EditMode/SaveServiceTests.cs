using System;
using System.IO;
using NUnit.Framework;
using WobbleBrawl.Core;

public sealed class SaveServiceTests
{
    private string directory;
    private string path;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "WobbleSaveTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "save.json");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(directory, true);

    [Test]
    public void SaveServiceRoundTripsSettingsAndProgress()
    {
        var data = new SaveData { Coins = 123 };
        data.Settings.Voice = .37f;
        data.UnlockedFighters.Add("Sir Noodle");
        data.Settings.Master = .27f;
        data.Settings.Haptics = false;
        data.Settings.Layout = ControlLayout.Mirrored;
        Assert.That(new SaveService(path).Save(data), Is.True);
        var loaded = new SaveService(path).Load();
        Assert.That(loaded.Version, Is.EqualTo(SaveService.CurrentVersion));
        Assert.That(loaded.Settings.Master, Is.EqualTo(.27f));
        Assert.That(loaded.Settings.Haptics, Is.False);
        Assert.That(loaded.Settings.Layout, Is.EqualTo(ControlLayout.Mirrored));
        Assert.That(loaded.Coins, Is.EqualTo(123));
        Assert.That(loaded.Settings.Voice, Is.EqualTo(.37f).Within(.001f));
        Assert.That(loaded.UnlockedFighters, Does.Contain("Sir Noodle"));
        Assert.That(File.Exists(path + ".tmp"), Is.False);
    }

    [Test]
    public void SaveServiceMigratesVersionOne()
    {
        File.WriteAllText(path, "{\"Version\":1,\"Master\":0.35,\"Haptics\":false,\"Coins\":45}");
        var loaded = new SaveService(path).Load();
        Assert.That(loaded.Version, Is.EqualTo(SaveService.CurrentVersion));
        Assert.That(loaded.Settings.Master, Is.EqualTo(.35f));
        Assert.That(loaded.Settings.Haptics, Is.False);
        Assert.That(loaded.Coins, Is.EqualTo(45));
        Assert.That(loaded.UnlockedFighters, Does.Contain("Blobby"));
    }

    [Test]
    public void SaveServiceUsesBackupAndPreservesItWhileRecovering()
    {
        var service = new SaveService(path);
        Assert.That(service.Save(new SaveData { Coins = 20 }), Is.True);
        Assert.That(service.Save(new SaveData { Coins = 40 }), Is.True);
        File.WriteAllText(path, "{truncated");
        var restored = service.Load();
        Assert.That(restored.Coins, Is.EqualTo(20));
        Assert.That(service.Save(restored), Is.True);
        File.WriteAllText(path, "bad again");
        Assert.That(service.Load().Coins, Is.EqualTo(20));
    }

    [Test]
    public void SaveServiceDefaultsOnMissingOrCorruptedFiles()
    {
        var service = new SaveService(path);
        Assert.That(service.Load().Settings.Master, Is.EqualTo(1f));
        File.WriteAllText(path, "broken");
        Assert.That(service.Load().Coins, Is.Zero);
        Assert.That(service.Load().UnlockedFighters, Does.Contain("Blobby"));
    }

    [Test]
    public void SaveServiceProtectsNewerVersions()
    {
        const string future = "{\"Version\":99,\"Coins\":700}";
        File.WriteAllText(path, future);
        var service = new SaveService(path);
        service.Load();
        Assert.That(service.IsReadOnly, Is.True);
        Assert.That(service.Save(new SaveData()), Is.False);
        Assert.That(File.ReadAllText(path), Is.EqualTo(future));
    }

    [Test]
    public void SaveServiceClampsInvalidTuning()
    {
        File.WriteAllText(path, "{\"Version\":2,\"Coins\":-4,\"Settings\":{\"Master\":8,\"Sfx\":-2,\"Layout\":99}}");
        var data = new SaveService(path).Load();
        Assert.That(data.Coins, Is.Zero);
        Assert.That(data.Settings.Master, Is.EqualTo(1f));
        Assert.That(data.Settings.Sfx, Is.Zero);
        Assert.That(data.Settings.Layout, Is.EqualTo(ControlLayout.Standard));
    }
}
