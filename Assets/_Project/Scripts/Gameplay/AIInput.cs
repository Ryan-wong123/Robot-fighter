using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    public sealed class AIInput : MonoBehaviour, IFighterInput
    {
        [SerializeField] private Fighter opponent;
        [SerializeField] private AIProfile profile;
        [SerializeField, Min(0.1f)] private float attackRange = 1.25f;
        [SerializeField, Min(1)] private int attackIntervalFrames = 36;

        private Fighter self;
        private ItemSpawner items;
        private HazardController hazard;
        private System.Random random;
        private int attackRecovery;
        private int reaction;
        private bool kick;
        private FighterIntent sustainedIntent;
        public bool IsPaused { get; private set; }

        private void Awake()
        {
            self = GetComponent<Fighter>();
            if (profile == null)
                profile = Resources.Load<AIProfile>("AI/Normal");
            random = new System.Random(1);
        }

        public void ConfigureRound(ItemSpawner spawner, HazardController stageHazard, int roundIndex)
        {
            items = spawner;
            hazard = stageHazard;
            random = new System.Random(unchecked(17 + roundIndex * 31 + self.Team));
            attackRecovery = reaction = 0;
            kick = false;
            sustainedIntent = default;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            reaction = 0;
            sustainedIntent = default;
        }

        public void SelectDifficulty(string difficulty)
        {
            var selected = Resources.Load<AIProfile>("AI/" + difficulty);
            if (selected == null)
                return;
            profile = selected;
            reaction = attackRecovery = 0;
            sustainedIntent = default;
        }

        public FighterIntent ReadIntent()
        {
            if (IsPaused || opponent == null || !opponent.IsAlive)
                return default;

            if (attackRecovery > 0)
                attackRecovery--;
            if (reaction > 0)
            {
                reaction--;
                return sustainedIntent;
            }

            FighterIntent intent = ChooseIntent();
            reaction = Mathf.Max(0, (profile != null ? profile.ReactionFrames : 1) - 1);
            sustainedIntent = new FighterIntent { Move = intent.Move, Block = intent.Block };
            return intent;
        }

        private FighterIntent ChooseIntent()
        {
            Vector3 position = transform.position;
            float offset = opponent.transform.position.x - position.x;
            float distance = Mathf.Abs(offset);
            float towardOpponent = Mathf.Sign(offset);
            bool threatened = opponent.State == FighterState.Attacking && distance <= attackRange + .2f;

            if (hazard != null && hazard.TryGetAvoidance(position, out var avoidance) &&
                Roll(profile != null ? profile.HazardAvoidance : 0f))
                return avoidance;

            if (threatened && Roll(profile != null ? profile.BlockChance : 1f))
                return new FighterIntent { Block = true };

            if (profile != null && !Roll(profile.MistakeChance))
            {
                var holder = self.Items;
                if (holder != null && holder.HasItem && InItemRange(holder.HeldDefinition, distance) &&
                    Roll(profile.ItemUseChance))
                {
                    if (distance > .05f && self.Facing != towardOpponent)
                        return new FighterIntent { Move = towardOpponent };
                    return new FighterIntent { Throw = true };
                }
                if (holder != null && !holder.HasItem && distance > attackRange && items != null &&
                    items.TryGetPickup(position, profile.PickupRange, out var pickup) && Roll(profile.ItemUseChance))
                    return new FighterIntent
                    {
                        Move = Mathf.Sign(pickup.x - position.x),
                        Jump = pickup.y > position.y + 1.05f
                    };
            }
            else if (profile != null)
                return default;

            if (distance > attackRange)
                return new FighterIntent { Move = towardOpponent };
            if (attackRecovery > 0 || !Roll(profile != null ? profile.Aggression : 1f))
                return default;
            if (distance > .05f && self.Facing != towardOpponent)
                return new FighterIntent { Move = towardOpponent };

            attackRecovery = profile != null ? profile.AttackIntervalFrames : attackIntervalFrames;
            kick = !kick;
            return new FighterIntent { Kick = kick, Punch = !kick };
        }

        private bool Roll(float probability) => random.NextDouble() < probability;

        private static bool InItemRange(ItemDef item, float distance)
        {
            if (item.Mode == ItemUseMode.Thrown)
                return distance <= 4f;
            if (item.Mode == ItemUseMode.Placed)
                return distance <= item.PlacementOffset + .75f;
            foreach (var effect in item.Effects ?? System.Array.Empty<ItemEffect>())
                if (effect is CombatItemEffect combat && combat.Action == CombatItemAction.AreaStrike)
                    return distance <= combat.Radius;
            return distance >= 1.5f && distance <= 4.5f;
        }
    }
}
