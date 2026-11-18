using UnityEditor;
using UnityEngine;
using WobbleBrawl.Data;

public static class SandboxAIProfiles
{
    [MenuItem("Wobble Brawl/Create Sandbox AI Profiles")]
    public static void Create()
    {
        const string root = "Assets/_Project/Data";
        if (!AssetDatabase.IsValidFolder(root + "/Resources"))
            AssetDatabase.CreateFolder(root, "Resources");
        if (!AssetDatabase.IsValidFolder(root + "/Resources/AI"))
            AssetDatabase.CreateFolder(root + "/Resources", "AI");
        Make(root, "Easy",   16, 42, .54f, .25f, .28f, .35f, .38f, 2.25f);
        Make(root, "Normal",  8, 28, .80f, .60f, .12f, .65f, .80f, 3.00f);
        Make(root, "Hard",    4, 19, .94f, .84f, .05f, .90f, .96f, 3.80f);
        AssetDatabase.SaveAssets();
    }

    private static void Make(string root, string tier, int reaction, int recovery, float aggression,
        float block, float mistake, float item, float hazard, float pickupRange)
    {
        string path = $"{root}/Resources/AI/{tier}.asset";
        if (AssetDatabase.LoadAssetAtPath<AIProfile>(path) != null)
            return;
        var profile = ScriptableObject.CreateInstance<AIProfile>();
        profile.ReactionFrames = reaction;
        profile.AttackIntervalFrames = recovery;
        profile.Aggression = aggression;
        profile.BlockChance = block;
        profile.MistakeChance = mistake;
        profile.ItemUseChance = item;
        profile.HazardAvoidance = hazard;
        profile.PickupRange = pickupRange;
        AssetDatabase.CreateAsset(profile, path);
    }
}
