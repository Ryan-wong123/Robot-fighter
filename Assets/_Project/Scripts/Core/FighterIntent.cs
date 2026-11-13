namespace WobbleBrawl.Core
{
    public struct FighterIntent
    {
        public float Move;
        public bool Jump;
        public bool Block;
        public bool Punch;
        public bool Kick;
        public bool Special;
        public bool Throw;
    }

    public interface IFighterInput
    {
        FighterIntent ReadIntent();
    }
}
