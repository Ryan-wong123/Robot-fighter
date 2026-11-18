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
        public MoveDef Punch;
        public MoveDef Kick;
        public MoveDef Special;
    }
}
