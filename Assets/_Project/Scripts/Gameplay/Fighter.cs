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
        private const float HurtboxHeightPadding = .2f;
        [SerializeField] private CharacterDef character;
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private Animator animator;
        private const int SandboxGroundMask = 1 << 8;
        [SerializeField] private LayerMask groundLayers = SandboxGroundMask;
        [SerializeField] private int team;

        private Rigidbody body;
        private Hitbox hitbox;
        private IFighterInput input;
        private FighterIntent touchIntent;

        public void SubmitTouchIntent(FighterIntent intent)
        {
            touchIntent.Move = intent.Move;
            touchIntent.Block = intent.Block;
            touchIntent.Jump |= intent.Jump;
            touchIntent.Punch |= intent.Punch;
            touchIntent.Kick |= intent.Kick;
            touchIntent.Special |= intent.Special;
            touchIntent.Throw |= intent.Throw;
        }
        private MoveDef move, comboMove, moveRoot, comboRoot;
        private int moveFrame, comboWindow, stun, immunity, comboHits, accumulatedStun, guardRecovery;
        private bool hit, roundActive;
        private int hitPulse = -1;
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
        private float waterMove = 1f, waterJump = 1f, waterGravity = 1f;
        private float conveyorSpeed;
        public bool IsSpecialMove(MoveDef attack) => character != null && attack != null && attack == character.Special;

        public void ConfigureCharacter(CharacterDef definition)
        {
            if (!roundActive && definition != null)
            {
                character = definition;
                if (definition.VisualPrefab != null)
                {
                    ragdoll?.Reset();
                    var previous = animator != null ? animator.gameObject : null;
                    var visual = Instantiate(definition.VisualPrefab, transform);
                    animator = visual.GetComponent<Animator>();
                    animator.enabled = false;
                    visualClips = definition.VisualClips;
                    activeClip = null;
                    playingClip = null;
                    ragdoll = new VisualRagdoll(visual.transform, definition.RagdollReactionScale,
                        definition.PartialRagdollSeconds);
                    if (TryGetComponent<SquashStretch>(out var squash))
                        squash.SetVisual(visual.transform);
                    if (visual.TryGetComponent<FighterVisual>(out var references))
                        Items?.SetSocket(references.ItemSocket);
                    if (previous != null)
                    {
                        previous.SetActive(false);
                        Destroy(previous);
                    }
                    if (TryGetComponent<CapsuleCollider>(out var capsule))
                    {
                        capsule.height = definition.Height;
                        capsule.center = Vector3.up * definition.Height * .5f;
                        capsule.radius = definition.BodyWidth * .5f;
                    }
                    foreach (var hurt in GetComponentsInChildren<Hurtbox>())
                        if (hurt.Zone == HurtZone.Body && hurt.TryGetComponent<BoxCollider>(out var shape))
                        {
                            hurt.transform.localPosition = Vector3.up * definition.Height * .5f;
                            shape.size = new Vector3(definition.BodyWidth,
                                definition.Height - HurtboxHeightPadding, shape.size.z);
                        }
                }
            }
        }

        public float AttackRange => character != null && character.Punch != null
            ? character.Punch.HitboxOffset.x + character.Punch.HitboxSize.x * .5f : 0f;

        public void PlayTaunt()
        {
            if (roundActive && (State == FighterState.Idle || State == FighterState.Moving))
                PlayClip("Taunt");
        }

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
        public int MaxHealth => character.MaxHealth;
        public int MaxGuard => character.MaxGuard;
        public int MaxSpecial => character.MaxSpecial;
        public event Action<Fighter> VitalsChanged;
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
                ragdoll = new VisualRagdoll(animator.transform, character.RagdollReactionScale,
                    character.PartialRagdollSeconds);
            }
            body.constraints |= RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        public void ResetRound(Vector3 position, int facing)
        {
            ragdoll?.Reset();
            SetWater(1f, 1f, 1f);
            conveyorSpeed = 0f;
            EndFreeze();
            body.position = position;
            body.linearVelocity = Vector3.zero;
            Facing = facing;
            itemHitCharges = 0;
            SetVitals(MaxHealth, MaxGuard, 0, true);
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

        public void CelebrateVictory()
        {
            if (!roundActive && IsAlive)
                PlayClip("Win");
        }
        public void SetSuddenDeath() => SetVitals(1, Guard, Special);

        private void SetVitals(int health, int guard, int special, bool force = false)
        {
            if (!force && Health == health && Guard == guard && Special == special)
                return;
            Health = health;
            Guard = guard;
            Special = special;
            VitalsChanged?.Invoke(this);
        }

        private void FixedUpdate()
        {
            ragdoll?.ApplyWaterGravity(waterGravity);
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
                    SetVitals(Health, MaxGuard, Special);
                    immunity = WakeupFrames;
                    if (playingClip == "GuardBreak")
                        PlayClip("GetUp");
                }
                return;
            }

            if (comboWindow > 0)
                comboWindow--;
            FighterIntent intent = input?.ReadIntent() ?? default;
            if (touchIntent.Move != 0f)
                intent.Move = touchIntent.Move;
            intent.Block |= touchIntent.Block;
            intent.Jump |= touchIntent.Jump;
            intent.Punch |= touchIntent.Punch;
            intent.Kick |= touchIntent.Kick;
            intent.Special |= touchIntent.Special;
            intent.Throw |= touchIntent.Throw;
            touchIntent.Jump = touchIntent.Punch = touchIntent.Kick = touchIntent.Special = touchIntent.Throw = false;
            if (input is AIInput { IsPaused: true })
                move = null;
            if (move != null)
            {
                int pulse = MoveHitSchedule.Pulse(moveFrame, move.Startup, move.Active, move.HitIntervalFrames);
                if (pulse != hitPulse)
                {
                    hitPulse = pulse;
                    hit = false;
                }
                if (!hit && pulse >= 0)
                    hit = hitbox.TryHit(this, move,
                        MoveHitSchedule.Direction(Facing, pulse, move.AlternateSides));
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
            {
                PlayClip("Throw");
                return;
            }

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
                    SetVitals(Health, Guard, 0);
                moveFrame = 0;
                hitPulse = -1;
                hit = false;
                State = FighterState.Attacking;
                SetHorizontal(conveyorSpeed);
                if (animator != null && !string.IsNullOrEmpty(move.AnimationTrigger))
                    PlayClip(move.AnimationTrigger);
                return;
            }

            if (intent.Block && grounded)
            {
                State = FighterState.Blocking;
                PlayClip("Block");
                SetHorizontal(conveyorSpeed);
                return;
            }
            if (Guard < character.MaxGuard && ++guardRecovery >= GuardRecoveryInterval)
            {
                SetVitals(Health, Guard + 1, Special);
                guardRecovery = 0;
            }
            if (accumulatedStun > 0 && --accumulatedStun == 0)
                comboHits = 0;
            float horizontal = Mathf.Clamp(intent.Move, -1f, 1f);
            if (horizontal != 0f)
                Facing = horizontal > 0f ? 1 : -1;
            SetHorizontal(horizontal * character.MoveSpeed * waterMove + (grounded ? conveyorSpeed : 0f));
            if (intent.Jump && grounded)
                body.linearVelocity = new Vector3(body.linearVelocity.x, character.JumpSpeed * waterJump, 0f);
            if (grounded && State == FighterState.Airborne && !intent.Jump)
                PlayClip("JumpLand");
            State = !grounded || intent.Jump ? FighterState.Airborne :
                horizontal == 0f ? FighterState.Idle : FighterState.Moving;
            PlayClip(State == FighterState.Airborne ? (body.linearVelocity.y > 0f ? "JumpRise" : "JumpFall") :
                State == FighterState.Moving ? "Walk" : "Idle");
        }

        private void PlayClip(string clip)
        {
            if (visualClips == null || playingClip == clip)
                return;
            if ((clip == "Idle" || clip == "Walk") && activeClip != null && clipTime < activeClip.length &&
                (playingClip == "JumpLand" || playingClip == "Throw" || playingClip == "GetUp" || playingClip == "Taunt"))
                return;

            foreach (var candidate in visualClips)
            {
                if (candidate == null || !candidate.name.EndsWith(character.AnimationPrefix + "_" + clip, StringComparison.Ordinal))
                    continue;
                playingClip = clip;
                activeClip = candidate;
                clipTime = 0f;
                SamplePose();
                return;
            }
            if (clip == "GuardBreak")
                PlayClip("HitHeavy");
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
            // Authored attack frames follow the move clock even with a 50 Hz physics step.
            if (move != null && activeClip == move.Clip)
                time = Mathf.Min(moveFrame / activeClip.frameRate, activeClip.length);
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
        internal void SetConveyorSpeed(float speed) => conveyorSpeed = speed;

        internal void SetWater(float moveMultiplier, float jumpMultiplier, float gravityMultiplier)
        {
            waterMove = moveMultiplier;
            waterJump = jumpMultiplier;
            waterGravity = gravityMultiplier;
        }

        private bool ResolveHit(HitInfo info, bool externalHit)
        {
            if (!roundActive || !IsAlive || immunity > 0)
                return false;
            var result = DamageResolver.Resolve(info.Zone, info.Move.BodyDamage, info.Move.LegDamage,
                Guard, info.Move.GuardCost, info.Move.StunFrames, comboHits, accumulatedStun,
                State == FighterState.Blocking, !externalHit && info.Attacker != null &&
                info.Attacker.IsSpecialMove(info.Move), false);
            SetVitals(Mathf.Max(0, Health - result.Damage), result.Guard,
                Mathf.Min(MaxSpecial, Special + info.Move.GaugeGain / 2));
            move = null;
            comboMove = null;
            stun = result.StunFrames;
            accumulatedStun += stun;
            comboHits++;
            State = !IsAlive ? FighterState.KnockedOut : result.GuardBroken ? FighterState.GuardBreak : FighterState.HitStun;
            PlayClip(!IsAlive ? "Knockdown" : result.GuardBroken ? "GuardBreak" :
                info.Move.HeavyReaction ? "HitHeavy" : "HitLight");
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
            if (info.Attacker != null)
            {
                var attacker = info.Attacker;
                attacker.SetVitals(attacker.Health, attacker.Guard,
                    Mathf.Min(attacker.MaxSpecial, attacker.Special + info.Move.GaugeGain));
            }
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
