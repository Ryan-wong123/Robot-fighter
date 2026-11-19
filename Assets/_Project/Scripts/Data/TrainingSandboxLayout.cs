using UnityEngine;

namespace WobbleBrawl.Data
{
    [CreateAssetMenu(menuName = "Wobble Brawl/Training Sandbox Layout")]
    public sealed class TrainingSandboxLayout : ScriptableObject
    {
        public int InstalledRevision;
        [Min(1f)] public float HalfWidth = 15f;
        [Min(.5f)] public float ItemSpacing = 2f;
        [Min(1f)] public float FighterSpawnX = 13f;
        [Min(1f)] public float CameraSize = 9.2f;
        [Min(2)] public int CopiesPerItem = 4;
        [Min(1)] public int DispenseCooldownFrames = 25;
    }
}
