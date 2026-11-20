using System;
using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    public enum FighterState { Idle, Moving, Airborne, Blocking, Attacking, HitStun, GuardBreak, KnockedOut, Deflated, KnockedDown, Trapped }

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
        private int launchContainment;
        private Vector2 launchBounds;
        private bool hasTrainingBounds;
        private Vector2 trainingBounds;
        internal void SetTrainingBounds(float minimum, float maximum)
        {
            hasTrainingBounds = true;
            trainingBounds = new Vector2(minimum, maximum);
        }
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
        private InflationState inflation;
        public bool IsInflated => inflation != null && inflation.Inflated;
        public bool IsDeflated => inflation != null && inflation.Deflated;
        public int PopMeter => inflation?.Meter ?? 0;
        public int PopCapacity => character.PopCapacity;
        public bool HasInflation => character.HasInflation;
        public bool IsHitStopped => frozen;
        public bool IsBellyBlimp => character.HasInflation && move == character.Special && move != null;
        public event Action InflationPopped;
        public event Action PresentationReset;
        public event Action VictoryCelebrated;
        public event Action<Fighter, string> VoiceRequested;
        public string VoiceId => character.AnimationPrefix.ToLowerInvariant();
        public int StuffingStage => character.HasStuffingLoss ? StuffingRules.Stage(Health, MaxHealth) : 0;
        public bool IsTough => character.HasStuffingLoss && IsAlive && StuffingStage == 3;

        public BossCombatDef Boss => character.Boss;
        public bool IsBossSecondPhase => Boss != null && (bossPhaseSeen || BossPhaseRules.IsSecondPhase(Health, MaxHealth));
        private bool bossPhaseSeen;
        private float bossTargetX;
        public bool IsBossSpecialWarning => Boss != null && move != null && IsSpecialMove(move) && moveFrame < move.Startup;
        public float BossTargetX => bossTargetX;
        public bool IsBossShockwaveActive => Boss != null && move != null && move.GroundShockwave &&
            moveFrame >= move.Startup && moveFrame < move.Startup + move.Active;
        public event Action BossPhaseChanged;

        public int AttackDamage(int baseDamage) => character.HasStuffingLoss
            ? StuffingRules.Damage(baseDamage, Health, MaxHealth, character.LowHealthDamageBonus) : baseDamage;
        private float waterMove = 1f, waterJump = 1f, waterGravity = 1f;
        private float stageJump = 1f;
        private float stageMove = 1f;
        private readonly ItemStatusState itemStatus = new ItemStatusState();
        private object trapSource;
        public bool IsItemSlowed => itemStatus.SlowFrames > 0;
        public bool IsBubbleTrapped => itemStatus.TrapFrames > 0;
        public float CharacterHeight => character.Height;
        private float conveyorSpeed;
        public bool IsSpecialMove(MoveDef attack) => character != null && attack != null &&
            (attack == character.Special || (Boss != null && attack == Boss.SecondSpecial));

        public void ConfigureCharacter(CharacterDef definition)
        {
            if (!roundActive && definition != null)
            {
                character = definition;
                ConfigureInflation();
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
            {
                PlayClip("Taunt");
                VoiceRequested?.Invoke(this, "taunt");
            }
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
            ConfigureInflation();
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
            bossPhaseSeen = false;
            inflation?.Reset();
            ragdoll?.Reset();
            SetWater(1f, 1f, 1f);
            stageJump = 1f;
            stageMove = 1f;
            itemStatus.Reset();
            trapSource = null;
            conveyorSpeed = 0f;
            EndFreeze();
            body.position = position;
            body.linearVelocity = Vector3.zero;
            Facing = facing;
            itemHitCharges = 0;
            SetVitals(MaxHealth, MaxGuard, Boss != null ? character.MaxSpecial : 0, true);
            move = comboMove = moveRoot = comboRoot = null;
            moveFrame = comboWindow = stun = immunity = comboHits = accumulatedStun = guardRecovery = 0;
            State = FighterState.Idle;
            playingClip = null;
            PlayClip("Idle");
            roundActive = true;
            launchContainment = 0;
            PresentationReset?.Invoke();
        }

        public void StopRound()
        {
            itemStatus.Reset();
            trapSource = null;
            VitalsChanged?.Invoke(this);
            roundActive = false;
            launchContainment = 0;
            inflation?.Reset();
            Items?.Clear();
            PresentationReset?.Invoke();
            if (inflation != null)
                VitalsChanged?.Invoke(this);
        }

        public void CelebrateVictory()
        {
            if (!roundActive && IsAlive)
            {
                PlayClip("Win");
                VictoryCelebrated?.Invoke();
                VoiceRequested?.Invoke(this, "win");
            }
        }
        public void SetSuddenDeath() => SetVitals(1, Guard, Special);

        private void SetVitals(int health, int guard, int special, bool force = false)
        {
            if (!force && Health == health && Guard == guard && Special == special)
                return;
            Health = health;
            if (!bossPhaseSeen && IsBossSecondPhase)
            {
                bossPhaseSeen = true;
                playingClip = string.Empty;
                PlayClip("PhaseChange");
                BossPhaseChanged?.Invoke();
            }
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
            bool wasSlowed = IsItemSlowed;
            bool wasTrapped = IsBubbleTrapped;
            itemStatus.Advance();
            if (wasTrapped && !IsBubbleTrapped)
            {
                trapSource = null;
                State = FighterState.Idle;
            }
            if (wasSlowed != IsItemSlowed || wasTrapped != IsBubbleTrapped) VitalsChanged?.Invoke(this);
            if (IsBubbleTrapped)
            {
                State = FighterState.Trapped;
                touchIntent = default;
                SetHorizontal(0f);
                return;
            }
            if (launchContainment > 0)
            {
                launchContainment--;
                Vector3 position = body.position;
                float contained = Mathf.Clamp(position.x,
                    hasTrainingBounds ? trainingBounds.x + character.BodyWidth * .5f : launchBounds.x,
                    hasTrainingBounds ? trainingBounds.y - character.BodyWidth * .5f : launchBounds.y);
                if (!Mathf.Approximately(contained, position.x))
                {
                    position.x = contained;
                    body.position = position;
                    SetHorizontal(0f);
                }
            }
            int previousMeter = PopMeter;
            bool wasDeflated = IsDeflated;
            inflation?.Advance();
            if (previousMeter != PopMeter)
                VitalsChanged?.Invoke(this);
            if (IsDeflated)
            {
                input?.ReadIntent();
                touchIntent = default;
                int phase = inflation.RemainingFrames / character.DeflateTurnFrames;
                float sign = phase % 2 == 0 ? Facing : -Facing;
                if (body.position.x < (hasTrainingBounds ? trainingBounds.x + character.BodyWidth : character.DeflateBounds.x)) sign = 1f;
                if (body.position.x > (hasTrainingBounds ? trainingBounds.y - character.BodyWidth : character.DeflateBounds.y)) sign = -1f;
                SetHorizontal(sign * character.DeflateSpeed);
                State = FighterState.Deflated;
                PlayClip("Deflate");
                return;
            }
            if (wasDeflated)
            {
                stun = 0;
                State = FighterState.Idle;
                PlayClip("Refill");
            }
            if (immunity > 0)
                immunity--;
            if (stun > 0)
            {
                if (--stun == 0 && (State == FighterState.GuardBreak || State == FighterState.KnockedDown))
                {
                    if (State == FighterState.GuardBreak)
                    {
                        SetVitals(Health, MaxGuard, Special);
                    }
                    immunity = WakeupFrames;
                    if (playingClip == "GuardBreak" || State == FighterState.KnockedDown)
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
            if (!intent.Block || move != null)
                inflation?.SetBlocking(false, false);
            if (input is AIInput { IsPaused: true })
                move = null;
            if (move != null)
            {
                if (Boss != null && IsSpecialMove(move) && moveFrame == move.Startup)
                {
                    var destination = new Vector3(bossTargetX, body.position.y, body.position.z);
                    destination.y = GetItemGroundHeight(destination) + Boss.PopGroundClearance;
                    body.position = destination;
                    body.linearVelocity = Vector3.zero;
                }
                if (move.ForwardSpeed > 0f)
                {
                    bool active = moveFrame >= move.Startup && moveFrame < move.Startup + move.Active;
                    SetHorizontal((active ? Facing * move.ForwardSpeed : 0f) + conveyorSpeed);
                }
                if (move.HopSpeed > 0f && moveFrame == move.Startup)
                    body.linearVelocity = new Vector3(body.linearVelocity.x, move.HopSpeed * waterJump, 0f);
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
            inflation?.SetBlocking(intent.Block && grounded, Guard == MaxGuard);
            var punch = IsBossSecondPhase ? Boss.SecondPunch : character.Punch;
            var kick = IsBossSecondPhase ? Boss.SecondKick : character.Kick;
            var special = IsBossSecondPhase ? Boss.SecondSpecial : character.Special;
            var next = intent.Special && Special == character.MaxSpecial ? special :
                intent.Punch ? punch : intent.Kick ? kick : null;
            if (next != null && grounded)
            {
                move = comboWindow > 0 && comboMove != null && comboMove.NextInCombo != null &&
                    next == comboRoot
                    ? comboMove.NextInCombo : next;
                moveRoot = next;
                inflation?.SetBlocking(false, false);
                if (IsSpecialMove(next))
                {
                    SetVitals(Health, Guard, 0);
                    if (Boss != null && ItemOpponent != null)
                        bossTargetX = Mathf.Clamp(ItemOpponent.transform.position.x,
                            hasTrainingBounds ? trainingBounds.x + character.BodyWidth * .5f : Boss.SpecialBounds.x,
                            hasTrainingBounds ? trainingBounds.y - character.BodyWidth * .5f : Boss.SpecialBounds.y);
                }
                moveFrame = 0;
                hitPulse = -1;
                hit = false;
                State = FighterState.Attacking;
                VoiceRequested?.Invoke(this, move.HeavyReaction || IsSpecialMove(move) ? "attack_heavy" : "attack_light");
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
            SetHorizontal(horizontal * (IsBossSecondPhase ? Boss.SecondMoveSpeed : character.MoveSpeed) * waterMove * stageMove * itemStatus.SlowPercent / 100f +
                (grounded ? conveyorSpeed : 0f));
            if (intent.Jump && grounded)
                body.linearVelocity = new Vector3(body.linearVelocity.x, character.JumpSpeed * waterJump * stageJump, 0f);
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
                (playingClip == "JumpLand" || playingClip == "Throw" || playingClip == "GetUp" ||
                 playingClip == "Taunt" || playingClip == "PhaseChange" || playingClip == "Refill"))
                return;

            string phasePrefix = IsBossSecondPhase && clip != "PhaseChange" && !clip.StartsWith("Out", StringComparison.Ordinal) ? "Out" : "";
            string clipName = character.AnimationPrefix + "_" + phasePrefix + clip;
            foreach (var candidate in visualClips)
            {
                if (candidate == null || !candidate.name.EndsWith(clipName, StringComparison.Ordinal))
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
        internal bool IsSupportedBy(Collider surface) =>
            surface != null && CanInteractWithItems && !frozen && body.linearVelocity.y <= .1f &&
            surface.Raycast(new Ray(body.position + Vector3.up * .15f, Vector3.down), out _, .25f);

        internal void FollowDeckHeight(Vector3 pivot, Vector3 previousNormal, Vector3 currentNormal)
        {
            Vector3 offset = body.position - pivot;
            offset.y = 0;
            float heightDelta = Vector3.Dot(previousNormal, offset) / previousNormal.y -
                Vector3.Dot(currentNormal, offset) / currentNormal.y;
            body.position += Vector3.up * heightDelta;
        }

        internal void ApplyGroundDrift(Vector3 displacement)
        {
            if (!CanInteractWithItems || frozen || MatchPause.IsPaused ||
                displacement.sqrMagnitude < 0.00000001f || body.linearVelocity.y > .1f ||
                !Physics.Raycast(body.position + Vector3.up * .15f, Vector3.down, .25f,
                    groundLayers, QueryTriggerInteraction.Ignore))
                return;

            float travel = displacement.magnitude;
            Vector3 direction = displacement / travel;
            if (body.SweepTest(direction, out var hit, travel, QueryTriggerInteraction.Ignore))
                travel = Mathf.Max(0f, hit.distance - .005f);
            body.position += direction * travel;
        }

        internal void SetConveyorSpeed(float speed) => conveyorSpeed = speed;

        internal void SetStageJumpMultiplier(float multiplier) => stageJump = multiplier;
        internal void SetStageMovementMultiplier(float multiplier) => stageMove = multiplier;

        internal void ApplyItemSlow(int frames, int percent)
        {
            itemStatus.Slow(frames, percent);
            VitalsChanged?.Invoke(this);
        }

        internal bool TryItemTrap(object source, int frames)
        {
            if (!roundActive || !IsAlive || immunity > 0 || IsBubbleTrapped || State == FighterState.Blocking)
                return false;
            trapSource = source;
            itemStatus.Trap(frames);
            move = comboMove = null;
            stun = 0;
            inflation?.SetBlocking(false, false);
            State = FighterState.Trapped;
            PlayClip("HitLight");
            VitalsChanged?.Invoke(this);
            return true;
        }

        internal bool HasItemTrap(object source) => IsBubbleTrapped && ReferenceEquals(trapSource, source);

        internal void ReleaseItemTrap(object source)
        {
            if (source == null || !ReferenceEquals(trapSource, source)) return;
            itemStatus.BreakTrap();
            trapSource = null;
            if (State == FighterState.Trapped) State = FighterState.Idle;
            VitalsChanged?.Invoke(this);
        }

        internal bool TryBounceFromStage(float multiplier)
        {
            if (!roundActive || !IsAlive || frozen || body.linearVelocity.y > .1f ||
                !Physics.Raycast(body.position + Vector3.up * .15f, Vector3.down, .25f,
                    groundLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            body.linearVelocity = new Vector3(body.linearVelocity.x, character.JumpSpeed * multiplier, 0f);
            inflation?.SetBlocking(false, false);
            if (move == null && stun == 0)
            {
                State = FighterState.Airborne;
                PlayClip("JumpRise");
            }
            return true;
        }

        internal void ConstrainToStage(float minimum, float maximum)
        {
            if (!roundActive || !IsAlive)
            {
                return;
            }
            Vector3 position = body.position;
            float contained = Mathf.Clamp(position.x, minimum + character.BodyWidth * .5f,
                maximum - character.BodyWidth * .5f);
            if (Mathf.Approximately(contained, position.x))
            {
                return;
            }
            position.x = contained;
            body.position = position;
            if (frozen)
            {
                velocity.x = 0f;
            }
            else
            {
                SetHorizontal(0f);
            }
        }

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
            ReleaseItemTrap(trapSource);
            bool fighterAttack = !externalHit && info.Attacker != null;
            int bodyDamage = fighterAttack ? info.Attacker.AttackDamage(info.Move.BodyDamage) : info.Move.BodyDamage;
            int legDamage = fighterAttack ? info.Attacker.AttackDamage(info.Move.LegDamage) : info.Move.LegDamage;
            var result = DamageResolver.Resolve(info.Zone, bodyDamage, legDamage,
                Guard, info.Move.GuardCost, info.Move.StunFrames, comboHits, accumulatedStun,
                State == FighterState.Blocking, !externalHit && info.Attacker != null &&
                info.Attacker.IsSpecialMove(info.Move), false);
            bool armored = IsInflated && !result.GuardBroken;
            if (armored)
                result = new DamageResult(inflation.ReduceDamage(result.Damage), result.Guard,
                    result.StunFrames, result.Blocked, result.GuardBroken);
            SetVitals(Mathf.Max(0, Health - result.Damage), result.Guard,
                Mathf.Min(MaxSpecial, Special + info.Move.GaugeGain / 2));
            move = null;
            comboMove = null;
            stun = result.StunFrames;
            accumulatedStun += stun;
            comboHits++;
            State = !IsAlive ? FighterState.KnockedOut : result.GuardBroken ? FighterState.GuardBreak : FighterState.HitStun;
            if (IsAlive && info.Move.KnocksDown && !result.Blocked)
            {
                State = FighterState.KnockedDown;
            }
            bool popped = IsAlive && inflation != null && inflation.RegisterHit();
            if (result.GuardBroken || !IsAlive)
                inflation?.SetBlocking(false, false);
            if (popped)
            {
                State = FighterState.Deflated;
                InflationPopped?.Invoke();
            }
            if (inflation != null)
                VitalsChanged?.Invoke(this);
            PlayClip(!IsAlive || State == FighterState.KnockedDown ? "Knockdown" : popped ? "Deflate" : result.GuardBroken ? "GuardBreak" :
                info.Move.HeavyReaction ? "HitHeavy" : "HitLight");
            float bonusPushback = externalHit || result.Blocked || info.Attacker == null
                ? 0f : info.Attacker.ConsumeItemPushback();
            float pushback = armored ? 0f : info.Direction.x * (info.Move.Pushback + bonusPushback);
            if (!Mathf.Approximately(pushback, 0f))
            {
                // Hit-stop restores this velocity when the body becomes dynamic again.
                if (frozen)
                    velocity.x = pushback;
                else
                    SetHorizontal(pushback);
            }
            if (!armored && info.Move.LaunchSpeed > 0f)
            {
                launchContainment = info.Move.LaunchContainmentFrames;
                launchBounds = info.Move.LaunchBounds;
            }
            if (!armored && !result.Blocked && info.Move.LaunchSpeed > 0f)
            {
                if (frozen)
                    velocity.y = Mathf.Max(velocity.y, info.Move.LaunchSpeed);
                else
                {
                    var launched = body.linearVelocity;
                    launched.y = Mathf.Max(launched.y, info.Move.LaunchSpeed);
                    body.linearVelocity = launched;
                }
            }
            if (info.Attacker != null)
            {
                var attacker = info.Attacker;
                attacker.SetVitals(attacker.Health, attacker.Guard,
                    Mathf.Min(attacker.MaxSpecial, attacker.Special + info.Move.GaugeGain));
            }
            HitStopService.Apply(info.Attacker, this, info.Move.HitStopFrames);
            if (!IsAlive)
            {
                if (info.Move.LaunchSpeed > 0f)
                    ragdoll?.Launch(info.Direction, info.Move.Pushback, info.Move.LaunchSpeed);
                else
                    ragdoll?.Launch(info.Direction);
            }
            else if (!result.Blocked && !armored)
                ragdoll?.React(info.Direction, info.Zone);

            HitResolved?.Invoke(info, result);
            if (!IsAlive)
                KnockedOut?.Invoke(this);
            return true;
        }

        private void ConfigureInflation()
        {
            inflation = character != null && character.HasInflation
                ? new InflationState(character.PopCapacity, character.PopGainPerHit,
                    character.DeflateFrames, character.InflationDamageReduction) : null;
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
