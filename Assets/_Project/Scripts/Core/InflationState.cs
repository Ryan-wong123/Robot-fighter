using System;

namespace WobbleBrawl.Core
{
    /// <summary>Frame-based inflatable armor and vulnerable pop recovery.</summary>
    public sealed class InflationState
    {
        private readonly int capacity, perHit, recoveryFrames, damageReduction;
        public bool Inflated { get; private set; }
        public int Meter { get; private set; }
        public int RemainingFrames { get; private set; }
        public bool Deflated => RemainingFrames > 0;

        public InflationState(int capacity, int perHit, int recoveryFrames, int damageReduction)
        {
            if (capacity <= 0 || perHit <= 0 || recoveryFrames <= 0 || damageReduction < 0 || damageReduction > 100)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            this.capacity = capacity;
            this.perHit = perHit;
            this.recoveryFrames = recoveryFrames;
            this.damageReduction = damageReduction;
        }

        public void SetBlocking(bool blocking, bool fullGuard)
        {
            Inflated = !Deflated && blocking && (Inflated || fullGuard);
        }

        public void Advance()
        {
            if (Deflated)
            {
                if (--RemainingFrames == 0)
                    Meter = 0;
            }
            else if (!Inflated && Meter > 0)
                Meter--;
        }

        public int ReduceDamage(int damage) => !Inflated ? damage : damage <= 0 || damageReduction == 100
            ? 0 : Math.Max(1, damage * (100 - damageReduction) / 100);

        public bool RegisterHit()
        {
            if (!Inflated)
                return false;
            Meter = Math.Min(capacity, Meter + perHit);
            if (Meter < capacity)
                return false;
            Inflated = false;
            RemainingFrames = recoveryFrames;
            return true;
        }

        public void Reset()
        {
            Inflated = false;
            Meter = RemainingFrames = 0;
        }
    }
}
