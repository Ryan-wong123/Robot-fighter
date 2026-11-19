using System;
using System.IO;
using System.Reflection;
using WobbleBrawl.Core;
using WobbleBrawl.Flow;

internal static class TestEconomy
{
    public static void Install(AppStateMachine app)
    {
        var data = new SaveData();
        data.UnlockedFighters.AddRange(new[] { "SirNoodle", "PuffinPete", "MadamePlush" });
        data.UnlockedArenas.AddRange(new[] { "BouncyCastleFair", "GiantSandbox" });
        string path = Path.Combine(UnityEngine.Application.temporaryCachePath, "WobbleBrawlTests", Guid.NewGuid() + ".json");
        typeof(AppStateMachine).GetField("wallet", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(app, new EconomyService(new SaveService(path), data));
    }
}
