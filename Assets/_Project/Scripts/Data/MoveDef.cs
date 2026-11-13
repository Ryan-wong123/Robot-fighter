using UnityEngine;

namespace WobbleBrawl.Data
{
    [CreateAssetMenu(menuName = "Wobble Brawl/Move")]
    public sealed class MoveDef : ScriptableObject
    {
        public AnimationClip Clip;
        public string AnimationTrigger;
        [Min(0)] public int Startup = 6;
        [Min(1)] public int Active = 4;
        [Min(0)] public int Recovery = 12;
        [Min(0)] public int BodyDamage = 10;
        [Min(0)] public int LegDamage = 5;
        [Min(0)] public int GuardCost = 10;
        [Min(0)] public int StunFrames = 10;
        [Min(0)] public int HitStopFrames = 5;
        [Min(0)] public int GaugeGain = 10;
        [Min(0)] public float Pushback = 2f;
        public Vector3 HitboxOffset = new Vector3(0.7f, 0.8f, 0f);
        public Vector3 HitboxSize = Vector3.one;
        public MoveDef NextInCombo;

        public int Duration => Startup + Active + Recovery;
    }
}
