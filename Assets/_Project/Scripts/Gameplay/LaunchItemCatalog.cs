using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    /// <summary>Additional installed pickups appended once to each scene's authored spawn table.</summary>
    [CreateAssetMenu(menuName = "Wobble Brawl/Launch Item Catalog")]
    public sealed class LaunchItemCatalog : ScriptableObject
    {
        public ItemDef[] Items;
    }
}
