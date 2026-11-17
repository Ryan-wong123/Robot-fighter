using UnityEngine;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    public enum CombatItemAction { Strike, BoostHits, Launch, AreaStrike }

    [CreateAssetMenu(menuName = "Wobble Brawl/Combat Item Effect")]
    public sealed class CombatItemEffect : ItemEffect
    {
        public CombatItemAction Action;
        public MoveDef Hit;
        [Min(1)] public int Charges = 3;
        [Min(0)] public float ExtraPushback;
        public Vector2 LaunchVelocity;
        [Min(0)] public float Radius;

        public override void Apply(ItemContext context)
        {
            switch (Action)
            {
                case CombatItemAction.Strike:
                    Strike(context, context.Target);
                    break;
                case CombatItemAction.BoostHits:
                    context.User.BoostItemHits(Charges, ExtraPushback);
                    break;
                case CombatItemAction.Launch:
                    context.User.LaunchFromItem(new Vector2(LaunchVelocity.x * context.Facing, LaunchVelocity.y));
                    break;
                case CombatItemAction.AreaStrike:
                    var opponent = context.User.ItemOpponent;
                    if (opponent != null &&
                        (opponent.transform.position - context.User.transform.position).sqrMagnitude <= Radius * Radius)
                        Strike(context, opponent);
                    break;
            }
        }

        private void Strike(ItemContext context, Fighter target)
        {
            if (Hit == null || target == null)
                return;
            float offset = target.transform.position.x - context.User.transform.position.x;
            float direction = Mathf.Approximately(offset, 0f) ? context.Facing : Mathf.Sign(offset);
            target.ReceiveItemHit(new HitInfo(context.User, Hit, HurtZone.Body, context.Position, Vector3.right * direction));
        }
    }
}
