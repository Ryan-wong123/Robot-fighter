namespace WobbleBrawl.Core
{
    public static class BossPhaseRules
    {
        public static bool IsSecondPhase(int health, int maximum) => health > 0 && (long)health * 2 < maximum;
    }
}
