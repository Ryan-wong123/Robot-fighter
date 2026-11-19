using UnityEngine;
using WobbleBrawl.Core;

namespace WobbleBrawl.Gameplay
{
    /// <summary>Optional pooled motion/contact adapter for the six additional launch items.</summary>
    public sealed class NoveltyItem : MonoBehaviour
    {
        [SerializeField] private Transform baseVisual;
        [SerializeField] private Transform actionVisual;
        [SerializeField] private Transform warningVisual;
        private PooledItem item;
        private Rigidbody body;
        private Rigidbody ownerBody;
        private Collider sensor;
        private Fighter owner, trapped, outboundHit, returnHit;
        private int facing, frame, windup, action;
        private bool deployed, returning, triggered;
        private float ground;
        private float pickupRadius;
        private Vector3 actionScale;
        private Quaternion baseRotation;
        private ItemDef Data => item.Definition;

        internal void Initialize(PooledItem pooled, Rigidbody physics, Collider contact)
        {
            item = pooled;
            body = physics;
            sensor = contact;
            if (sensor is SphereCollider sphere) pickupRadius = sphere.radius;
            if (actionVisual != null) actionScale = actionVisual.localScale;
            baseRotation = baseVisual.localRotation;
        }

        internal void Begin(Fighter user, int direction, float groundHeight)
        {
            owner = user;
            ownerBody = user.GetComponent<Rigidbody>();
            facing = direction;
            ground = groundHeight;
            frame = action = windup = 0;
            deployed = true;
            returning = triggered = false;
            outboundHit = returnHit = null;
            if (!body.isKinematic) body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
            Vector3 origin = body.position;
            origin.z = owner.transform.position.z;
            if (Data.Mode != ItemUseMode.Thrown)
            {
                origin = owner.transform.position + Vector3.right * (facing * Data.PlacementOffset);
                origin.y = groundHeight;
            }
            body.position = origin;
            sensor.enabled = Data.NoveltyKind != NoveltyItemKind.PogoStick && Data.NoveltyKind != NoveltyItemKind.BoxingJack;
            if (Data.NoveltyKind == NoveltyItemKind.BubbleWand)
            {
                if (sensor is SphereCollider sphere) sphere.radius = .45f;
                baseVisual.gameObject.SetActive(false);
                actionVisual.gameObject.SetActive(true);
            }
            if (Data.NoveltyKind == NoveltyItemKind.PogoStick)
            {
                owner.LaunchFromItem(new Vector2(Data.UseVelocity.x * facing, Data.UseVelocity.y));
                AudioManager.Play(AudioCue.ItemPogo);
            }
            if (Data.NoveltyKind == NoveltyItemKind.WindUpCar) AudioManager.Play(AudioCue.ItemCar);
            if (Data.NoveltyKind == NoveltyItemKind.WindUpCar && facing < 0)
                baseVisual.localRotation = baseRotation * Quaternion.Euler(0f, 180f, 0f);
            if (Data.NoveltyKind == NoveltyItemKind.Boomerang) AudioManager.Play(AudioCue.ItemBoomerang);
        }

        internal void Clear()
        {
            if (sensor is SphereCollider sphere) sphere.radius = pickupRadius;
            if (trapped != null) trapped.ReleaseItemTrap(this);
            trapped = owner = outboundHit = returnHit = null;
            deployed = false;
            baseVisual.gameObject.SetActive(true);
            baseVisual.localRotation = baseRotation;
            if (actionVisual != null)
            {
                actionVisual.localScale = actionScale;
                if (item.Definition.NoveltyKind == NoveltyItemKind.BubbleWand)
                    actionVisual.gameObject.SetActive(false);
            }
            if (warningVisual != null) warningVisual.gameObject.SetActive(false);
        }

        internal void Tick()
        {
            if (owner == null || !owner.IsAlive)
            {
                item.Recycle();
                return;
            }
            frame++;
            switch (Data.NoveltyKind)
            {
                case NoveltyItemKind.CreamPie:
                    Travel(Data.ThrowSpeed);
                    break;
                case NoveltyItemKind.Boomerang:
                    returning |= frame > Data.OutboundFrames;
                    if (returning)
                    {
                        Vector3 goal = owner.transform.position + Vector3.up * .85f;
                        if ((goal - body.position).sqrMagnitude < .12f) item.Recycle();
                        else body.MovePosition(Vector3.MoveTowards(body.position, goal, Data.ThrowSpeed * Time.fixedDeltaTime));
                    }
                    else Travel(Data.ThrowSpeed);
                    break;
                case NoveltyItemKind.BubbleWand:
                    if (trapped != null)
                    {
                        if (!trapped.HasItemTrap(this)) item.Recycle();
                        else body.MovePosition(trapped.transform.position + Vector3.up * .85f);
                    }
                    else Travel(Data.ThrowSpeed);
                    break;
                case NoveltyItemKind.WindUpCar:
                    var position = body.position + Vector3.right * (facing * Data.ThrowSpeed * Time.fixedDeltaTime);
                    position.y = owner.GetItemGroundHeight(position) + Data.GroundClearance;
                    body.MovePosition(position);
                    break;
                case NoveltyItemKind.PogoStick:
                    body.MovePosition(owner.transform.position + Vector3.down * .35f);
                    if (frame > 6 && owner.transform.position.y <= owner.GetItemGroundHeight(owner.transform.position) + .1f &&
                        ownerBody.linearVelocity.y <= .1f)
                    {
                        var target = owner.ItemOpponent;
                        if (InRange(target)) Strike(target, facing);
                        AudioManager.Play(AudioCue.ItemPogo);
                        item.Recycle();
                    }
                    break;
                case NoveltyItemKind.BoxingJack:
                    UpdateJack();
                    break;
            }
        }

        private void Travel(float speed) => body.MovePosition(body.position + Vector3.right *
            (facing * speed * Time.fixedDeltaTime));

        private bool InRange(Fighter target) => target != null && target.IsAlive &&
            Mathf.Abs(target.transform.position.x - body.position.x) <= Data.EffectRadius &&
            Mathf.Abs(target.transform.position.y - ground) <= 1.2f;

        private void UpdateJack()
        {
            if (!triggered)
            {
                if (!InRange(owner.ItemOpponent)) return;
                triggered = true;
                windup = Data.TelegraphFrames;
                item.ExtendLifetime(Data.TelegraphFrames + Data.ActionFrames + 1);
                warningVisual.gameObject.SetActive(true);
            }
            if (windup > 0)
            {
                windup--;
                warningVisual.localScale = Vector3.one * (1f + .1f * Mathf.Sin(frame * .2f));
                if (windup > 0) return;
                warningVisual.gameObject.SetActive(false);
                action = Data.ActionFrames;
                if (InRange(owner.ItemOpponent)) Strike(owner.ItemOpponent, facing);
                if (!deployed) return;
                AudioManager.Play(AudioCue.ItemJack);
            }
            if (action > 0)
            {
                float progress = 1f - action / (float)Data.ActionFrames;
                var scale = actionScale;
                scale.y *= 1f + Mathf.Sin(progress * Mathf.PI);
                actionVisual.localScale = scale;
                if (--action == 0) item.Recycle();
            }
        }

        internal void Contact(Fighter target)
        {
            switch (Data.NoveltyKind)
            {
                case NoveltyItemKind.Boomerang:
                    if ((!returning && outboundHit == target) || (returning && returnHit == target)) return;
                    if (Strike(target, returning ? (owner.transform.position.x < body.position.x ? -1 : 1) : facing))
                    {
                        if (returning) returnHit = target;
                        else outboundHit = target;
                    }
                    return;
                case NoveltyItemKind.BubbleWand:
                    if (trapped != null) return;
                    if (target.TryItemTrap(this, Data.StatusFrames))
                    {
                        trapped = target;
                        item.ExtendLifetime(Data.StatusFrames + 1);
                        sensor.enabled = false;
                        actionVisual.localScale = Vector3.Scale(actionScale, new Vector3(1.3f, 2.1f, 1.3f));
                        AudioManager.Play(AudioCue.ItemBubble);
                    }
                    else item.Recycle();
                    return;
                case NoveltyItemKind.CreamPie:
                    bool blocking = target.State == FighterState.Blocking && target.Guard > Data.ContactHit.GuardCost;
                    if (Strike(target, facing) && !blocking && target.CanInteractWithItems)
                        target.ApplyItemSlow(Data.StatusFrames, Data.SlowPercent);
                    AudioManager.Play(AudioCue.ItemPie);
                    item.Recycle();
                    return;
                case NoveltyItemKind.WindUpCar:
                    Strike(target, facing);
                    item.Recycle();
                    return;
            }
        }

        private bool Strike(Fighter target, int direction)
        {
            if (target != null && (Data.NoveltyKind == NoveltyItemKind.PogoStick || Data.NoveltyKind == NoveltyItemKind.BoxingJack))
            {
                float offset = target.transform.position.x - body.position.x;
                if (!Mathf.Approximately(offset, 0f)) direction = offset < 0f ? -1 : 1;
            }
            return Data.ContactHit != null && target != null && target.ReceiveItemHit(new HitInfo(owner,
                Data.ContactHit, HurtZone.Body, body.position, Vector3.right * direction));
        }

        private void LateUpdate()
        {
            if (deployed && Data.NoveltyKind == NoveltyItemKind.Boomerang)
                baseVisual.Rotate(0f, 0f, 540f * Time.deltaTime);
        }
    }
}
