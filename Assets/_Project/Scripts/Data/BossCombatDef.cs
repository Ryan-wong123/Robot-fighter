using UnityEngine;

namespace WobbleBrawl.Data
{
    [CreateAssetMenu(menuName = "Wobble Brawl/Boss combat")]
    public sealed class BossCombatDef : ScriptableObject
    {
        [HideInInspector] public int InstalledRevision;
        public MoveDef SecondPunch, SecondKick, SecondSpecial;
        public AIProfile Profile;
        [Min(0f)] public float SecondMoveSpeed = 4.2f;
        [Min(1)] public int SpecialCooldownFrames = 250;
        public Vector2 SpecialRange = new Vector2(1.5f, 6f);
        [Min(0f)] public float PopGroundClearance = .02f;
        public Vector2 SpecialBounds = new Vector2(-5.7f, 5.7f);
    }
}
