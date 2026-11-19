using UnityEngine;

namespace WobbleBrawl.Data
{
    [CreateAssetMenu(menuName = "Wobble Brawl/Character")]
    public sealed class CharacterDef : ScriptableObject
    {
        public GameObject Prefab;
        public GameObject VisualPrefab;
        public string DisplayName = "Blobby";
        public string AnimationPrefix = "Blobby";
        public AnimationClip[] VisualClips;
        [Min(.1f)] public float Height = 1.8f;
        [Min(.1f)] public float BodyWidth = .7f;
        [Min(.1f)] public float RagdollReactionScale = 1f;
        [Min(.1f)] public float PartialRagdollSeconds = .48f;
        [Min(1)] public int MaxHealth = 200;
        [Min(1)] public int MaxGuard = 100;
        [Min(1)] public int MaxSpecial = 100;
        [Min(0)] public float MoveSpeed = 4f;
        [Min(0)] public float JumpSpeed = 10f;
        public BossCombatDef Boss;
        public MoveDef Punch;
        public MoveDef Kick;
        public MoveDef Special;
        public bool HasInflation;
        [Min(1)] public int PopCapacity = 100;
        [Min(1)] public int PopGainPerHit = 25;
        [Min(1)] public int DeflateFrames = 75;
        [Range(0, 100)] public int InflationDamageReduction = 30;
        [Min(0)] public float DeflateSpeed = 5f;
        [Min(1)] public int DeflateTurnFrames = 12;
        public Vector2 DeflateBounds = new Vector2(-6.2f, 6.2f);
        public bool HasStuffingLoss;
        [Range(0, 100)] public int LowHealthDamageBonus = 10;
    }
}
