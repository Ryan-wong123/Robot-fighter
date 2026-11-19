using UnityEngine;

namespace WobbleBrawl.Data
{
    /// <summary>Installed stage metadata, discovered once when Boot starts.</summary>
    [CreateAssetMenu(menuName = "Wobble Brawl/Stage")]
    public sealed class StageDef : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public string SceneName;
        public string HazardHint;
        public Texture2D Thumbnail;
    }
}
