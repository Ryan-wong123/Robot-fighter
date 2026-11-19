using UnityEngine;

namespace WobbleBrawl.Data
{
    /// <summary>Starting layout and non-damaging hazard tuning for Bouncy Castle Fair.</summary>
    [CreateAssetMenu(menuName = "Wobble Brawl/Fairground tuning")]
    public sealed class FairgroundDef : ScriptableObject
    {
        [Min(1f)] public float HalfWidth = 7f;
        [Range(0f, .25f)] public float WidthReduction = .25f;
        [Range(.01f, .5f)] public float SqueezeRampFraction = .25f;
        public float[] PadCenters = { -3f, 3f };
        [Min(.1f)] public float PadHalfWidth = 1f;
        [Min(.01f)] public float ContactHeight = .15f;
        [Min(1f)] public float JumpMultiplier = 1.8f;
        [Min(.1f)] public float WarningPulseHz = 1.5f;
        [Range(0f, .1f)] public float WarningBulge = .05f;
        [Min(.1f)] public float CameraResponse = 5f;
        [Min(.1f)] public float CameraHeadroom = 1f;
        public Vector3[] ItemPoints =
        {
            new Vector3(-3f, .65f, 0f), new Vector3(0f, .65f, 0f), new Vector3(3f, .65f, 0f)
        };
    }
}
