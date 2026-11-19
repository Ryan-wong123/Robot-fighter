using System;
using System.Collections.Generic;

namespace WobbleBrawl.Core
{
    public sealed class EconomyService
    {
        private readonly SaveData data;
        private readonly SaveService saves;
        public int Balance => data.Coins;

        public EconomyService(SaveService saves, SaveData data)
        {
            this.saves = saves;
            this.data = data;
        }

        public static int FighterPrice(string id) => id switch
        {
            "SirNoodle" => 60,
            "PuffinPete" => 90,
            "MadamePlush" => 120,
            _ => 0
        };

        public static int ArenaPrice(string id) => id switch
        {
            "BouncyCastleFair" => 75,
            "GiantSandbox" => 100,
            _ => 0
        };

        public bool OwnsFighter(string id) => FighterPrice(id) == 0 ||
            data.UnlockedFighters.Contains(id) ||
            (id == "SirNoodle" && data.UnlockedFighters.Contains("Sir Noodle")) ||
            (id == "PuffinPete" && data.UnlockedFighters.Contains("Puffin Pete")) ||
            (id == "MadamePlush" && data.UnlockedFighters.Contains("Madame Plush"));

        public bool OwnsArena(string id) => ArenaPrice(id) == 0 || data.UnlockedArenas.Contains(id);

        public bool TryUnlockFighter(string id) => OwnsFighter(id) ||
            TryPurchase(id, FighterPrice(id), data.UnlockedFighters);

        public bool TryUnlockArena(string id) => OwnsArena(id) ||
            TryPurchase(id, ArenaPrice(id), data.UnlockedArenas);

        private bool TryPurchase(string id, int cost, List<string> owned)
        {
            if (cost <= 0 || Balance < cost) return false;
            data.Coins -= cost;
            owned.Add(id);
            if (saves.Save(data)) return true;
            owned.Remove(id);
            data.Coins += cost;
            return false;
        }

        public int AwardMatch(bool won, bool draw, bool arcadeCleared)
        {
            int oldBalance = Balance;
            int reward = (won ? 30 : draw ? 15 : 10) + (arcadeCleared && won ? 50 : 0);
            data.Coins = (int)Math.Min(int.MaxValue, (long)oldBalance + reward);
            if (saves.Save(data)) return Balance - oldBalance;
            data.Coins = oldBalance;
            return 0;
        }
    }
}
