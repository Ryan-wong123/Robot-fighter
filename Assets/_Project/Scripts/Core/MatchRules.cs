namespace WobbleBrawl.Core
{
    /// <summary>Match setup shared by local flow and future synchronized configuration.</summary>
    public enum MatchMode { QuickFight, Versus, Arcade }

    /// <summary>Independent Classic options; default matches enable both systems.</summary>
    public readonly struct MatchRules
    {
        public static MatchRules Default => new MatchRules(true, true);
        public bool HazardsEnabled { get; }
        public bool ItemsEnabled { get; }

        public MatchRules(bool hazardsEnabled, bool itemsEnabled)
        {
            HazardsEnabled = hazardsEnabled;
            ItemsEnabled = itemsEnabled;
        }

        /// <summary>Arcade and boss encounters always include their designed chaos.</summary>
        public MatchRules Resolve(MatchMode mode, bool isBossStage) =>
            mode == MatchMode.Arcade || isBossStage ? Default : this;
    }
}
