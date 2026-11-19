using System;

namespace WobbleBrawl.Core
{
    /// <summary>Integer health thresholds and the plush fighter's low-health bonus.</summary>
    public static class StuffingRules
    {
        public static int Stage(int health, int maximum)
        {
            if (maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            long value = Math.Max(0, health);
            return value * 4 <= maximum ? 3 : value * 2 <= maximum ? 2 :
                value * 4 <= (long)maximum * 3 ? 1 : 0;
        }

        public static int Damage(int damage, int health, int maximum, int bonusPercent)
        {
            return health > 0 && Stage(health, maximum) == 3
                ? (damage * (100 + bonusPercent) + 50) / 100 : damage;
        }
    }
}
