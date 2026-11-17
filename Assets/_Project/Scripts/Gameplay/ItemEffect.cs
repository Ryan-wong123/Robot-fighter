using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    public readonly struct ItemContext
    {
        public readonly Fighter User;
        public readonly Fighter Target;
        public readonly Vector3 Position;
        public readonly int Facing;

        public ItemContext(Fighter user, Fighter target, Vector3 position, int facing)
        {
            User = user;
            Target = target;
            Position = position;
            Facing = facing;
        }
    }

    public abstract class ItemEffect : ScriptableObject
    {
        public abstract void Apply(ItemContext context);
    }
}
