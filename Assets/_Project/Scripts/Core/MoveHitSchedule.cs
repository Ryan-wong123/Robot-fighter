namespace WobbleBrawl.Core
{
    /// <summary>Frame-based hit windows; zero interval preserves single-hit moves.</summary>
    public static class MoveHitSchedule
    {
        public static int Pulse(int frame, int startup, int active, int interval)
        {
            int elapsed = frame - startup;
            return elapsed < 0 || elapsed >= active ? -1 : interval > 0 ? elapsed / interval : 0;
        }

        public static int Direction(int facing, int pulse, bool alternateSides) =>
            alternateSides && pulse % 2 != 0 ? -facing : facing;
    }
}
