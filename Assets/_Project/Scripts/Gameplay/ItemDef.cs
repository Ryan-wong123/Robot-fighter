using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    public enum ItemUseMode { OnPickup, Thrown, Placed, Held }

    [CreateAssetMenu(menuName = "Wobble Brawl/Item")]
    public sealed class ItemDef : ScriptableObject
    {
        public string Id;
        public PooledItem Prefab;
        public ItemUseMode Mode;
        public ItemEffect[] Effects;
        [Min(0)] public float ThrowSpeed = 8f;
        [Min(0)] public float PlacementOffset = 0.75f;
        [Min(0)] public float ThrowLift;
        [Min(0)] public int GroundBounces;
        [Min(0)] public float BounceSpeed;
        [Min(0)] public float GroundClearance = 0.18f;
        [Min(0)] public int TelegraphFrames;
        [Min(0)] public float DropHeight;
        [Min(1)] public int LifetimeFrames = 300;

        internal void Apply(Fighter user, Fighter target, Vector3 position, int facing)
        {
            if (Effects == null)
                return;
            var context = new ItemContext(user, target, position, facing);
            foreach (var effect in Effects)
                if (effect != null)
                    effect.Apply(context);
        }
    }
}
