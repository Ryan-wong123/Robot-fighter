using UnityEngine;

namespace WobbleBrawl.Data
{
    [CreateAssetMenu(menuName = "Wobble Brawl/Hazard")]
    public sealed class HazardDef : ScriptableObject
    {
        public const float MinimumWarningSeconds = 1.5f;

        [Min(0)] public float FirstWarningDelay = 15f;
        [Min(MinimumWarningSeconds)] public float WarningSeconds = MinimumWarningSeconds;
        [Min(.02f)] public float ActiveSeconds = 2f;
        [Min(0)] public float CooldownSeconds = 2f;
        public Vector2 IdleSeconds = new Vector2(8f, 14f);
        public Vector3 Size = new Vector3(2f, 2f, 1f);
        public MoveDef Hit;
        public bool AllowSuddenDeath;
    }
}
