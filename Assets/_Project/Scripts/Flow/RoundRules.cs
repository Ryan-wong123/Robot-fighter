namespace WobbleBrawl.Flow
{
    public enum RoundResult { Continue, PlayerWins, OpponentWins, SuddenDeath, Draw }

    public static class RoundRules
    {
        public const int RoundSeconds = 60;
        public const int SuddenDeathSeconds = 10;
        public const int SuddenDeathHealth = 1;

        public static RoundResult Evaluate(float player, float opponent, bool expired, bool suddenDeath)
        {
            if (!expired && player > 0f && opponent > 0f)
                return RoundResult.Continue;
            if (player > opponent)
                return RoundResult.PlayerWins;
            if (opponent > player)
                return RoundResult.OpponentWins;
            return suddenDeath ? RoundResult.Draw : RoundResult.SuddenDeath;
        }
    }
}
