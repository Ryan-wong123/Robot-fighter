using System;
using System.Collections.Generic;

namespace WobbleBrawl.Gameplay
{
    public static class ItemWeights
    {
        public static int Select(IReadOnlyList<int> weights, Random random)
        {
            long total = 0;
            for (int index = 0; index < weights.Count; index++)
                total += Math.Max(0, weights[index]);
            if (total == 0)
                return -1;

            long roll = (long)(random.NextDouble() * total);
            for (int index = 0; index < weights.Count; index++)
            {
                roll -= Math.Max(0, weights[index]);
                if (roll < 0)
                    return index;
            }
            return -1;
        }
    }
}
