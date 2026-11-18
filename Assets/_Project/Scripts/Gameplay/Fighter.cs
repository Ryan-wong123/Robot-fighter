using System;
using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    public enum FighterState { Idle, Moving, Airborne, Blocking, Attacking, HitStun, GuardBreak, KnockedOut }

    [RequireComponent(typeof(Rigidbody), typeof(Hitbox))]
    public sealed class Fighter : MonoBehaviour
    {
        private const int ComboWindowFrames = 30;
        private const int WakeupFrames = 20;
        private const int GuardRecoveryInterval = 3;
        [SerializeField] private CharacterDef character;
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private Animator animator;
        private const int SandboxGroundMask = 1 << 8;
        [SerializeField] private LayerMask groundLayers = SandboxGroundMask;
        [SerializeField] private int team;

        private Rigidbody body;
        private Hitbox hitbox;
        private IFighterInput input;
        private MoveDef move, comboMove, moveRoot, comboRoot;
        private int moveFrame, comboWindow, stun, immunity, comboHits, accumulatedStun, guardRecovery;
        private bool hit, roundActive;
        private int freeze;
        private bool wasKinematic;
        private float animationSpeed;
        private bool frozen;
        private int frozenAtFrame;
        private Vector3 velocity;
        private string playingClip;
        private AnimationClip activeClip;
        [SerializeField] private AnimationClip[] visualClips;
        private float clipTime;
        private VisualRagdoll ragdoll;
        public int Facing { get; private set; } = 1;
        public int Team => team;
        public bool CanInteractWithItems => roundActive && IsAlive;
        private const int ItemLaunchFrames = 22;
        private int itemHitCharges;
        private float itemHitPushback;
        public Fighter ItemOpponent { get; private set; }
        public void SetItemOpponent(Fighter opponent) => ItemOpponent = opponent;
        internal ItemHolder Items { get; private set; }
        public int Health { get; private set; }
        public int Guard { get; private set; }
        public int Special { get; private set; }
        public bool IsAlive => Health > 0;
        public FighterState State { get; private set; }
        public event Action<Fighter> KnockedOut;
        public event Action<HitInfo, DamageResult> HitResolved;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            hitbox = GetComponent<Hitbox>();
            Items = GetComponent<ItemHolder>();
            input = inputSource as IFighterInput;
            if (groundLayers.value == 0)
                groundLayers = SandboxGroundMask;
            if (animator != null)
            {
                if ((visualClips == null || visualClips.Length == 0) && animator.runtimeAnimatorController != null)
                    visualClips = animator.runtimeAnimatorController.animationClips;
                foreach (var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
                    skin.updateWhenOffscreen = true;
                // The imported Generic controller enters states but does not drive this rig's transforms.
                animator.enabled = false;
                ragdoll = new VisualRagdoll(animator.transform);
            }
            body.constraints |= RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        public void ResetRound(Vector3 position, int facing)
        {
            ragdoll?.Reset();
            EndFreeze();
            body.position = position;
            body.linearVelocity = Vector3.zero;
            Facing = facing;
            itemHitCharges = 0;
            Health = character.MaxHealth;
            Guard = character.MaxGuard;
            Special = 0;
            move = comboMove = moveRoot = comboRoot = null;
            moveFrame = comboWindow = stun = immunity = comboHits = accumulatedStun = guardRecovery = 0;
            State = FighterState.Idle;
            playingClip = null;
            PlayClip("Idle");
            roundActive = true;
        }

        public void StopRound()
        {
            roundActive = false;
            Items?.Clear();
        }
        public void SetSuddenDeath() => Health = 1;

        private void FixedUpdate()
        {
            if (frozen)
            {
                if (frozenAtFrame != Time.frameCount && --freeze == 0)
                    EndFreeze();
                return;
            }
            if (!roundActive || !IsAlive)
                return;
            if (immunity > 0)
                immunity--;
            if (stun > 0)
            {
                if (--stun == 0 && State == FighterState.GuardBreak)
                {
                    Guard = character.MaxGuard;
                    immunity = WakeupFrames;
                }
                return;
            }

            if (comboWindow > 0)
                comboWindow--;
            FighterIntent intent = input?.ReadIntent() ?? default;
            if (input is AIInput { IsPaused: true })
                move = null;
            if (move != null)
            {
                if (!hit && moveFrame >= move.Startup && moveFrame < move.Startup + move.Active)
                    hit = hitbox.TryHit(this, move);
                if (++moveFrame >= move.Duration)
                {
                    comboMove = move;
                    comboRoot = moveRoot;
                    comboWindow = ComboWindowFrames;
                    move = null;
                }
                return;
            }

            if (intent.Throw && Items != null && Items.TryUse())
                return;

            bool grounded = Physics.Raycast(body.position + Vector3.up * 0.15f, Vector3.down, 0.25f,
                groundLayers, QueryTriggerInteraction.Ignore);
            var next = intent.Special && Special == character.MaxSpecial ? character.Special :
                intent.Punch ? character.Punch : intent.Kick ? character.Kick : null;
            if (next != null && grounded)
            {
                move = comboWindow > 0 && comboMove != null && comboMove.NextInCombo != null &&
                    next == comboRoot
                    ? comboMove.NextInCombo : next;
                moveRoot = next;
                if (next == character.Special)
                    Special = 0;
                moveFrame = 0;
                hit = false;
                State = FighterState.Attacking;
                SetHorizontal(0);
                if (animator != null && !string.IsNullOrEmpty(move.AnimationTrigger))
                    PlayClip(move.AnimationTrigger);
                return;
            }

            if (intent.Block && grounded)
            {
                State = FighterState.Blocking;
                PlayClip("Block");
                SetHorizontal(0);
                return;
            }
            if (Guard < character.MaxGuard && ++guardRecovery >= GuardRecoveryInterval)
            {
                Guard++;
                guardRecovery = 0;
            }
            if (accumulatedStun > 0 && --accumulatedStun == 0)
                comboHits = 0;
            float horizontal = Mathf.Clamp(intent.Move, -1f, 1f);
            if (horizontal != 0f)
                Facing = horizontal > 0f ? 1 : -1;
            SetHorizontal(horizontal * character.MoveSpeed);
            if (intent.Jump && grounded)
                body.linearVelocity = new Vector3(body.linearVelocity.x, character.JumpSpeed, 0f);
            State = !grounded || intent.Jump ? FighterState.Airborne :
                horizontal == 0f ? FighterState.Idle : FighterState.Moving;
            PlayClip(State == FighterState.Airborne ? (body.linearVelocity.y > 0f ? "JumpRise" : "JumpFall") :
                State == FighterState.Moving ? "Walk" : "Idle");
        }

        private void PlayClip(string clip)
        {
            if (visualClips == null || playingClip == clip)
                return;

            foreach (var candidate in visualClips)
            {
                if (candidate == null || !candidate.name.EndsWith("Blobby_" + clip, StringComparison.Ordinal))
                    continue;
                playingClip = clip;
                activeClip = candidate;
                clipTime = 0f;
                SamplePose();
                return;
            }
        }

        private void LateUpdate()
        {
            if (activeClip == null || frozen)
                return;

            clipTime += Time.deltaTime;
            SamplePose();
        }

        private void SamplePose()
        {
            float time = activeClip.isLooping ? Mathf.Repeat(clipTime, activeClip.length) :
                Mathf.Min(clipTime, activeClip.length);
            if (ragdoll != null && ragdoll.Full)
                return;

            ragdoll?.CaptureHitPose();
            activeClip.SampleAnimation(animator.gameObject, time);
            // Blobby's face is on local +Z, while the fight camera sits on world -Z.
            animator.transform.localRotation = Quaternion.Euler(0f, 180f - 45f * Facing, 0f);
            ragdoll?.BlendHit(Time.deltaTime);
        }

        private void SetHorizontal(float value)
        {
            var current = body.linearVelocity;
            current.x = value;
            body.linearVelocity = current;
        }

        internal void BoostItemHits(int charges, float pushback)
        {
            itemHitCharges = charges;
            itemHitPushback = pushback;
        }

        internal float GetItemGroundHeight(Vector3 position)
        {
            return Physics.Raycast(position + Vector3.up, Vector3.down, out var hit, Mathf.Infinity,
                groundLayers, QueryTriggerInteraction.Ignore) ? hit.point.y : transform.position.y;
        }

        internal void LaunchFromItem(Vector2 velocity)
        {
            State = FighterState.Airborne;
            PlayClip("JumpRise");
            stun = ItemLaunchFrames;
            body.linearVelocity = new Vector3(velocity.x, velocity.y, 0f);
        }

        private float ConsumeItemPushback()
        {
            if (itemHitCharges <= 0)
                return 0f;
            itemHitCharges--;
            return itemHitPushback;
        }

        public bool ReceiveHit(HitInfo info) => ResolveHit(info, false);
        internal bool ReceiveItemHit(HitInfo info) => ResolveHit(info, true);
        internal bool ReceiveHazardHit(HitInfo info) => ResolveHit(info, true);

        private bool ResolveHit(HitInfo info, bool externalHit)
        {
            if (!roundActive || !IsAlive || immunity > 0)
                return false;
            var result = DamageResolver.Resolve(info.Zone, info.Move.BodyDamage, info.Move.LegDamage,
                Guard, info.Move.GuardCost, info.Move.StunFrames, comboHits, accumulatedStun,
                State == FighterState.Blocking, !externalHit && info.Attacker != null &&
                info.Move == info.Attacker.character.Special, false);
            Health = Mathf.Max(0, Health - result.Damage);
            Guard = result.Guard;
            move = null;
            comboMove = null;
            stun = result.StunFrames;
            accumulatedStun += stun;
            comboHits++;
            State = !IsAlive ? FighterState.KnockedOut : result.GuardBroken ? FighterState.GuardBreak : FighterState.HitStun;
            PlayClip(!IsAlive ? "Knockdown" : result.GuardBroken ? "HitHeavy" : "HitLight");
            float bonusPushback = externalHit || result.Blocked || info.Attacker == null
                ? 0f : info.Attacker.ConsumeItemPushback();
            float pushback = info.Direction.x * (info.Move.Pushback + bonusPushback);
            if (!Mathf.Approximately(pushback, 0f))
            {
                // Hit-stop restores this velocity when the body becomes dynamic again.
                if (frozen)
                    velocity.x = pushback;
                else
                    SetHorizontal(pushback);
            }
            Special = Mathf.Min(character.MaxSpecial, Special + info.Move.GaugeGain / 2);
            if (info.Attacker != null)
                info.Attacker.Special = Mathf.Min(info.Attacker.character.MaxSpecial, info.Attacker.Special + info.Move.GaugeGain);
            HitStopService.Apply(info.Attacker, this, info.Move.HitStopFrames);
            if (!IsAlive)
                ragdoll?.Launch(info.Direction);
            else if (!result.Blocked)
                ragdoll?.React(info.Direction, info.Zone);

            HitResolved?.Invoke(info, result);
            if (!IsAlive)
                KnockedOut?.Invoke(this);
            return true;
        }

        internal void Freeze(int frames)
        {
            if (frames <= 0)
                return;
            if (!frozen)
            {
                frozen = true;
                velocity = body.linearVelocity;
                wasKinematic = body.isKinematic;
                body.isKinematic = true;
                if (animator != null)
                {
                    animationSpeed = animator.speed;
                    animator.speed = 0f;
                }
            }
            freeze = Mathf.Max(freeze, frames);
            frozenAtFrame = Time.frameCount;
        }

        private void OnDisable()
        {
            ragdoll?.Reset();
            EndFreeze();
        }

        private void EndFreeze()
        {
            if (!frozen)
                return;
            body.isKinematic = wasKinematic;
            if (!wasKinematic)
                body.linearVelocity = velocity;
            if (animator != null)
                animator.speed = animationSpeed;
            freeze = 0;
            frozen = false;
        }
    }
}
