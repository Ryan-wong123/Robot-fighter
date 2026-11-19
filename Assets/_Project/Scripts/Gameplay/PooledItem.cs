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
        private int telegraphFrames;
        private int bouncesRemaining;
        private float groundHeight;
        private bool falling;
        private NoveltyItem novelty;
        private ItemSpawner dispenser;
        private int dispenserIndex;

        public ItemDef Definition { get; private set; }
        internal bool IsAvailable => state == ItemState.Available;
        internal bool IsPickup => state == ItemState.Pickup;
        internal bool IsDeployed => state == ItemState.Deployed;
        internal void ConfigureDispenser(ItemSpawner source, int index)
        {
            dispenser = source;
            dispenserIndex = index;
        }
        internal void ExtendLifetime(int frames) => remainingFrames = Mathf.Max(remainingFrames, frames);

        internal void Initialize(ItemDef definition, Transform root)
        {
            Definition = definition;
            poolRoot = root;
            body = GetComponent<Rigidbody>();
            sensor = GetComponent<Collider>();
            novelty = GetComponent<NoveltyItem>();
            novelty?.Initialize(this, body, sensor);
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
            Vector3 placement = owner.transform.position + Vector3.right * (direction * Definition.PlacementOffset);
            groundHeight = owner.GetItemGroundHeight(placement) + Definition.GroundClearance;
            telegraphFrames = Definition.TelegraphFrames;
            bouncesRemaining = Definition.GroundBounces;
            falling = false;
            if (novelty != null)
            {
                novelty.Begin(owner, direction, groundHeight);
                return;
            }
            bool thrown = Definition.Mode == ItemUseMode.Thrown;
            if (thrown)
            {
                Vector3 position = transform.position;
                position.z = owner.transform.position.z;
                transform.position = position;
            }
            else
            {
                placement.y = groundHeight;
                transform.position = placement;
            }
            body.isKinematic = !thrown;
            body.useGravity = Definition.GroundBounces > 0 || Definition.DropHeight > 0;
            if (thrown)
                body.linearVelocity = new Vector3(direction * Definition.ThrowSpeed, Definition.ThrowLift, 0f);
            sensor.enabled = telegraphFrames == 0;
        }

        internal void Consume(Fighter source, Fighter target, int direction)
        {
            Vector3 position = transform.position;
            Recycle();
            Definition.Apply(source, target, position, direction);
        }

        internal void Recycle()
        {
            novelty?.Clear();
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
            if (state == ItemState.Pickup && dispenser != null) return;
            if ((state == ItemState.Pickup || state == ItemState.Deployed) && --remainingFrames <= 0)
            {
                Recycle();
                return;
            }
            if (state != ItemState.Deployed)
                return;
            if (novelty != null)
            {
                novelty.Tick();
                return;
            }
            if (telegraphFrames > 0)
            {
                if (--telegraphFrames == 0)
                {
                    body.position += Vector3.up * Definition.DropHeight;
                    body.isKinematic = false;
                    sensor.enabled = true;
                    falling = true;
                }
                return;
            }
            if (!body.useGravity || body.position.y > groundHeight || body.linearVelocity.y >= 0)
                return;
            if (falling || bouncesRemaining == 0)
            {
                Recycle();
                return;
            }
            bouncesRemaining--;
            Vector3 position = body.position;
            position.y = groundHeight;
            body.position = position;
            body.linearVelocity = new Vector3(body.linearVelocity.x, Definition.BounceSpeed, 0f);
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
            {
                if (dispenser != null) dispenser.TryDispense(dispenserIndex, target);
                else target.Items?.TryPickup(this);
            }
            else if (target != user && target.Team != user.Team)
            {
                if (novelty != null) novelty.Contact(target);
                else Consume(user, target, facing);
            }
        }
    }
}
