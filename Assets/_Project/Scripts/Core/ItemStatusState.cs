using System;

namespace WobbleBrawl.Core
{
    /// <summary>Frame-based item slowdown and breakable trap timers.</summary>
    public sealed class ItemStatusState
    {
        public int SlowFrames { get; private set; }
        public int SlowPercent { get; private set; } = 100;
        public int TrapFrames { get; private set; }

        public void Slow(int frames, int percent)
        {
            if (frames <= 0) return;
            SlowFrames = Math.Max(SlowFrames, frames);
            SlowPercent = Math.Min(SlowPercent, Math.Clamp(percent, 1, 100));
        }

        public void Trap(int frames) => TrapFrames = Math.Max(1, frames);
        public void BreakTrap() => TrapFrames = 0;

        public void Advance()
        {
            if (SlowFrames > 0 && --SlowFrames == 0) SlowPercent = 100;
            if (TrapFrames > 0) TrapFrames--;
        }

        public void Reset()
        {
            SlowFrames = TrapFrames = 0;
            SlowPercent = 100;
        }
    }
}
