using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private Fighter owner;
        [SerializeField] private HurtZone zone;
        public Fighter Owner => owner;
        public HurtZone Zone => zone;
    }
}
