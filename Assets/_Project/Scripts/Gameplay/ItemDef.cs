using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    public enum NoveltyItemKind { CreamPie, Boomerang, PogoStick, BubbleWand, WindUpCar, BoxingJack }
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
        public NoveltyItemKind NoveltyKind;
        public WobbleBrawl.Data.MoveDef ContactHit;
        [Min(1)] public int StatusFrames = 90;
        [Range(1, 100)] public int SlowPercent = 65;
        [Min(1)] public int OutboundFrames = 35;
        [Min(.1f)] public float EffectRadius = 1.8f;
        public Vector2 UseVelocity = new Vector2(4f, 10f);
        [Min(1)] public int ActionFrames = 12;
        public Vector2 AIUseRange;

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
