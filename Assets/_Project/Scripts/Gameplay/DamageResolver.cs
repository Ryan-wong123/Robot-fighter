namespace WobbleBrawl.Gameplay
{
    public enum HurtZone { Head, Body, Legs }

    public readonly struct DamageResult
    {
        public readonly int Damage, Guard, StunFrames;
        public readonly bool Blocked, GuardBroken;

        public DamageResult(int damage, int guard, int stunFrames, bool blocked, bool guardBroken)
        {
            Damage = damage;
            Guard = guard;
            StunFrames = stunFrames;
            Blocked = blocked;
            GuardBroken = guardBroken;
        }
    }

    public static class DamageResolver
    {
        public const int GuardBreakFrames = 60;
        public const int MaximumComboStunFrames = 30;

        public static DamageResult Resolve(HurtZone zone, int bodyDamage, int legDamage, int guard,
            int guardCost, int stunFrames, int comboHits, int accumulatedStunFrames,
            bool blocking, bool special, bool invulnerable)
        {
            if (invulnerable)
                return new DamageResult(0, guard, 0, false, false);

            int rawDamage = zone == HurtZone.Legs ? legDamage : bodyDamage;
            int scale = 100 - System.Math.Min(5, System.Math.Max(0, comboHits)) * 10;
            int damage = (rawDamage * scale + 99) / 100;
            int remainingGuard = blocking ? System.Math.Max(0, guard - guardCost) : guard;
            bool broken = blocking && remainingGuard == 0;
            bool blocked = blocking && !broken;
            if (blocked)
                damage = (damage + (special ? 1 : 3)) / (special ? 2 : 4);

            int availableStun = System.Math.Max(0, MaximumComboStunFrames - accumulatedStunFrames);
            return new DamageResult(damage, remainingGuard,
                broken ? GuardBreakFrames : System.Math.Min(stunFrames, availableStun), blocked, broken);
        }
    }
}
