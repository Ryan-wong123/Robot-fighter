using UnityEngine;

namespace WobbleBrawl.Data
{
    /// <summary>Layout and non-damaging hazard tuning for the backyard sandbox.</summary>
    [CreateAssetMenu(menuName = "Wobble Brawl/Giant Sandbox tuning")]
    public sealed class GiantSandboxDef : ScriptableObject
    {
        [Min(1f)] public float HalfWidth = 7f;
        [Min(1f)] public float PawTravelHalfWidth = 8.5f;
        [Min(.1f)] public float PawHalfWidth = .95f;
        [Min(.1f)] public float JumpClearance = .85f;
        [Min(.1f)] public float SweepSeconds = 3f;
        public float[] SandCenters = { -3f, 0f, 3f };
        [Min(.1f)] public float SandHalfWidth = 1.35f;
        [Min(.01f)] public float GroundContactHeight = .15f;
        [Range(.1f, 1f)] public float MovementMultiplier = .5f;
        [Min(.1f)] public float WarningPulseHz = 1.5f;
        [Min(0f)] public float SpiralDegreesPerSecond = 90f;
        public Vector3[] ItemPoints =
        {
            new Vector3(-4.5f, .65f, 0f), new Vector3(0f, .65f, 0f), new Vector3(4.5f, .65f, 0f)
        };

        private void OnValidate()
        {
            PawTravelHalfWidth = Mathf.Max(PawTravelHalfWidth, HalfWidth + PawHalfWidth);
            for (int index = 0; index < SandCenters.Length; index++)
            {
                SandCenters[index] = Mathf.Clamp(SandCenters[index], -HalfWidth + SandHalfWidth,
                    HalfWidth - SandHalfWidth);
            }
        }
    }
}
