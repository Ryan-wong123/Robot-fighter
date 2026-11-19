namespace WobbleBrawl.Core
{
    /// <summary>Stable scene identities separating development controls from playable arenas.</summary>
    public static class TrainingSandboxRules
    {
        public const string Scene = "CombatSandbox";
        public const string RetiredScene = "NewItemSandbox";
        public static bool AllowsShortcuts(string scene) => scene == Scene;
        public static bool IsSelectableArena(string scene) => !string.IsNullOrEmpty(scene) && scene != Scene && scene != RetiredScene;
    }
}
