using UnityEngine;

namespace WobbleBrawl.Data
{
    [CreateAssetMenu(menuName = "Wobble Brawl/Character")]
    public sealed class CharacterDef : ScriptableObject
    {
        public GameObject Prefab;
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
