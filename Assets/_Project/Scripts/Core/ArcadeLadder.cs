namespace WobbleBrawl.Core
{
    public sealed class ArcadeLadder
    {
        public const string FinaleScene = "ToyChestShowdown";
        private static readonly string[] Scenes =
        {
            "BathtubBrawl", "KitchenCounter", "BouncyCastleFair", "GiantSandbox", FinaleScene
        };
        private static readonly string[] Names =
        {
            "Bathtub Brawl", "Kitchen Counter", "Bouncy Castle Fair", "Giant Sandbox", "Toy Chest Showdown"
        };

        public static int Count => Scenes.Length;
        public static string SceneAt(int index) => Scenes[index];
        public int Index { get; private set; }
        public string Scene => SceneAt(Index);
        public string Arena => Names[Index];
        public bool IsFinale => Index == Count - 1;
        public bool IsCleared { get; private set; }
        public AIDifficulty Difficulty => Index == 0 ? AIDifficulty.Easy :
            Index == 1 ? AIDifficulty.Normal : AIDifficulty.Hard;

        public void Reset()
        {
            Index = 0;
            IsCleared = false;
        }

        public void RecordResult(bool playerWon)
        {
            IsCleared = IsFinale && playerWon;
        }

        public void ContinueAfter(bool playerWon)
        {
            if (playerWon && !IsFinale)
            {
                Index++;
            }
        }
    }
}
