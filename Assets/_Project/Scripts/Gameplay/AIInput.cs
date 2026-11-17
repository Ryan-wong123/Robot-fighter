using UnityEngine;
using WobbleBrawl.Core;

namespace WobbleBrawl.Gameplay
{
    public sealed class AIInput : MonoBehaviour, IFighterInput
    {
        [SerializeField] private Fighter opponent;
        [SerializeField, Min(0.1f)] private float attackRange = 1.25f;
        [SerializeField, Min(1)] private int attackIntervalFrames = 36;
        private int remainingFrames;
        private bool kick;

        public FighterIntent ReadIntent()
        {
            if (opponent == null || !opponent.IsAlive)
                return default;

            float distance = opponent.transform.position.x - transform.position.x;
            if (remainingFrames > 0)
                remainingFrames--;
            if (Mathf.Abs(distance) > attackRange)
                return new FighterIntent { Move = Mathf.Sign(distance) };
            if (opponent.State == FighterState.Attacking)
                return new FighterIntent { Block = true };
            if (remainingFrames > 0)
                return default;

            remainingFrames = attackIntervalFrames;
            kick = !kick;
            return new FighterIntent { Kick = kick, Punch = !kick };
        }
    }
}
