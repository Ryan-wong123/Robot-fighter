using UnityEngine;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    public readonly struct HitInfo
    {
        public readonly Fighter Attacker;
        public readonly MoveDef Move;
        public readonly HurtZone Zone;
        public readonly Vector3 Point, Direction;

        public HitInfo(Fighter attacker, MoveDef move, HurtZone zone, Vector3 point, Vector3 direction)
        {
            Attacker = attacker;
            Move = move;
            Zone = zone;
            Point = point;
            Direction = direction;
        }
    }

    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask hurtLayers;
        private readonly Collider[] overlaps = new Collider[16];

        public bool TryHit(Fighter attacker, MoveDef move)
        {
            Vector3 direction = Vector3.right * attacker.Facing;
            Vector3 offset = move.HitboxOffset;
            offset.x *= attacker.Facing;
            Vector3 center = attacker.transform.position + offset;
            int count = Physics.OverlapBoxNonAlloc(center, move.HitboxSize * 0.5f, overlaps,
                Quaternion.identity, hurtLayers, QueryTriggerInteraction.Collide);
            Hurtbox target = null;
            float closest = float.PositiveInfinity;
            for (int index = 0; index < count; index++)
            {
                if (!overlaps[index].TryGetComponent(out Hurtbox candidate) || candidate.Owner == null ||
                    candidate.Owner == attacker || candidate.Owner.Team == attacker.Team || !candidate.Owner.IsAlive)
                    continue;
                float distance = (overlaps[index].ClosestPoint(center) - center).sqrMagnitude;
                if (distance >= closest)
                    continue;
                closest = distance;
                target = candidate;
            }

            return target != null && target.Owner.ReceiveHit(
                new HitInfo(attacker, move, target.Zone, target.transform.position, direction));
        }
    }
}
