namespace WobbleBrawl.Gameplay
{
    public sealed class BlockRules
    {
        public const float MaximumGuard = 100f;
        public const float BreakDuration = 1f;
        public const float BlockDamageMultiplier = 0.25f;
        public const float SpecialDamageMultiplier = 0.5f;
        public const float RecoveryPerSecond = 20f;
        public float Guard { get; private set; } = MaximumGuard;
        public float StunRemaining { get; private set; }

        public void Advance(float seconds, bool held)
        {
            StunRemaining = System.Math.Max(0f, StunRemaining - seconds);
            if (!held && StunRemaining == 0f)
                Guard = System.Math.Min(MaximumGuard, Guard + RecoveryPerSecond * seconds);
        }

        public float Resolve(float damage, bool held, bool special, out bool blocked)
        {
            blocked = held && StunRemaining == 0f && Guard > damage;
            if (!held || StunRemaining > 0f)
                return damage;

            Guard = System.Math.Max(0f, Guard - damage);
            if (!blocked)
            {
                StunRemaining = BreakDuration;
                Guard = MaximumGuard;
                return damage;
            }
            return damage * (special ? SpecialDamageMultiplier : BlockDamageMultiplier);
        }
    }
}
