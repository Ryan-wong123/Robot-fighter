using UnityEngine;

namespace WobbleBrawl.Data
{
    [CreateAssetMenu(menuName = "Wobble Brawl/AI Profile")]
    public sealed class AIProfile : ScriptableObject
    {
        [Min(1)] public int ReactionFrames = 8;
        [Min(1)] public int AttackIntervalFrames = 28;
        [Range(0f, 1f)] public float Aggression = .8f;
        [Range(0f, 1f)] public float BlockChance = .6f;
        [Range(0f, 1f)] public float MistakeChance = .12f;
        [Range(0f, 1f)] public float ItemUseChance = .65f;
        [Range(0f, 1f)] public float HazardAvoidance = .8f;
        [Min(0f)] public float PickupRange = 3f;
    }
}
