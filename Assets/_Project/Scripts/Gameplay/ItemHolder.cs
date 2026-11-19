using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    [RequireComponent(typeof(Fighter))]
    public sealed class ItemHolder : MonoBehaviour
    {
        [SerializeField] private Transform handSocket;
        private Fighter owner;
        private PooledItem held;

        public bool HasItem => held != null;
        internal ItemDef HeldDefinition => held != null ? held.Definition : null;

        private void Awake()
        {
            owner = GetComponent<Fighter>();
            var visual = GetComponentInChildren<FighterVisual>();
            if (visual != null && visual.ItemSocket != null) handSocket = visual.ItemSocket;
        }
        private void OnDisable() => Clear();

        internal void SetSocket(Transform socket)
        {
            Clear();
            handSocket = socket;
        }

        internal bool TryPickup(PooledItem item)
        {
            if (!owner.CanInteractWithItems || HasItem)
                return false;
            if (item.Definition.Mode == ItemUseMode.OnPickup)
            {
                item.Consume(owner, owner, owner.Facing);
                return true;
            }
            if (handSocket == null)
                return false;
            held = item;
            item.Attach(this, handSocket);
            return true;
        }

        internal bool TryUse()
        {
            if (!HasItem)
                return false;
            PooledItem item = held;
            held = null;
            if (item.Definition.Mode == ItemUseMode.Held)
                item.Consume(owner, owner, owner.Facing);
            else
                item.Deploy(owner, owner.Facing);
            return true;
        }

        internal void Clear()
        {
            if (HasItem)
                held.Recycle();
            held = null;
        }

        internal void Forget(PooledItem item)
        {
            if (held == item)
                held = null;
        }
    }
}
