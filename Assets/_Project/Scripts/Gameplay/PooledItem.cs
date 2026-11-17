using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PooledItem : MonoBehaviour
    {
        private enum ItemState { Available, Pickup, Held, Deployed }

        private Rigidbody body;
        private Collider sensor;
        private Transform poolRoot;
        private Fighter user;
        private ItemHolder holder;
        private ItemState state;
        private int facing;
        private int remainingFrames;

        internal ItemDef Definition { get; private set; }
        internal bool IsAvailable => state == ItemState.Available;

        internal void Initialize(ItemDef definition, Transform root)
        {
            Definition = definition;
            poolRoot = root;
            body = GetComponent<Rigidbody>();
            sensor = GetComponent<Collider>();
            sensor.isTrigger = true;
            body.constraints |= RigidbodyConstraints.FreezePositionZ;
            Recycle();
        }

        internal void Spawn(Vector3 position)
        {
            state = ItemState.Pickup;
            remainingFrames = Mathf.Max(1, Definition.LifetimeFrames);
            transform.SetPositionAndRotation(position, Quaternion.identity);
            body.isKinematic = true;
            sensor.enabled = true;
            gameObject.SetActive(true);
        }

        internal void Attach(ItemHolder itemHolder, Transform socket)
        {
            state = ItemState.Held;
            holder = itemHolder;
            sensor.enabled = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            transform.SetParent(socket, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        internal void Deploy(Fighter owner, int direction)
        {
            holder = null;
            user = owner;
            facing = direction;
            state = ItemState.Deployed;
            remainingFrames = Mathf.Max(1, Definition.LifetimeFrames);
            transform.SetParent(poolRoot, true);
            bool thrown = Definition.Mode == ItemUseMode.Thrown;
            if (!thrown)
                transform.position = owner.transform.position + Vector3.right * (direction * Definition.PlacementOffset);
            body.isKinematic = !thrown;
            if (thrown)
                body.linearVelocity = Vector3.right * (direction * Definition.ThrowSpeed);
            sensor.enabled = true;
        }

        internal void Consume(Fighter source, Fighter target, int direction)
        {
            Vector3 position = transform.position;
            Recycle();
            Definition.Apply(source, target, position, direction);
        }

        internal void Recycle()
        {
            state = ItemState.Available;
            holder?.Forget(this);
            holder = null;
            user = null;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            sensor.enabled = false;
            gameObject.SetActive(false);
            transform.SetParent(poolRoot, false);
        }

        private void FixedUpdate()
        {
            if ((state == ItemState.Pickup || state == ItemState.Deployed) && --remainingFrames <= 0)
                Recycle();
        }

        private void OnTriggerEnter(Collider other) => CheckContact(other);
        private void OnTriggerStay(Collider other) => CheckContact(other);

        private void CheckContact(Collider other)
        {
            if (state != ItemState.Pickup && state != ItemState.Deployed)
                return;
            if (!other.TryGetComponent<Hurtbox>(out var hurtbox) || hurtbox.Owner == null)
                return;
            Fighter target = hurtbox.Owner;
            if (!target.CanInteractWithItems)
                return;
            if (state == ItemState.Pickup)
                target.Items?.TryPickup(this);
            else if (target != user && target.Team != user.Team)
                Consume(user, target, facing);
        }
    }
}
