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
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private int team;

        private Rigidbody body;
        private Hitbox hitbox;
        private IFighterInput input;
        private MoveDef move, comboMove;
        private int moveFrame, comboWindow, stun, immunity, comboHits, accumulatedStun, guardRecovery;
        private bool hit, roundActive;
        private int freeze;
        private bool wasKinematic;
        private float animationSpeed;
        private int frozenAtFrame;
        private Vector3 velocity;
        public int Facing { get; private set; } = 1;
        public int Team => team;
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
            input = inputSource as IFighterInput;
            body.constraints |= RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        public void ResetRound(Vector3 position, int facing)
        {
            EndFreeze();
            body.position = position;
            body.linearVelocity = Vector3.zero;
            Facing = facing;
            Health = character.MaxHealth;
            Guard = character.MaxGuard;
            Special = 0;
            move = comboMove = null;
            moveFrame = comboWindow = stun = immunity = comboHits = accumulatedStun = guardRecovery = 0;
            State = FighterState.Idle;
            roundActive = true;
        }

        public void StopRound() => roundActive = false;
        public void SetSuddenDeath() => Health = 1;

        private void FixedUpdate()
        {
            if (freeze > 0)
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
            if (move != null)
            {
                if (!hit && moveFrame >= move.Startup && moveFrame < move.Startup + move.Active)
                    hit = hitbox.TryHit(this, move);
                if (++moveFrame >= move.Duration)
                {
                    comboMove = move;
                    comboWindow = ComboWindowFrames;
                    move = null;
                }
                return;
            }

            bool grounded = Physics.Raycast(body.position + Vector3.up * 0.15f, Vector3.down, 0.25f,
                groundLayers, QueryTriggerInteraction.Ignore);
            var next = intent.Special && Special == character.MaxSpecial ? character.Special :
                intent.Punch ? character.Punch : intent.Kick ? character.Kick : null;
            if (next != null && grounded)
            {
                move = comboWindow > 0 && comboMove != null && comboMove.NextInCombo != null &&
                    ((intent.Punch && next == character.Punch) || (intent.Kick && next == character.Kick))
                    ? comboMove.NextInCombo : next;
                if (next == character.Special)
                    Special = 0;
                moveFrame = 0;
                hit = false;
                State = FighterState.Attacking;
                SetHorizontal(0);
                if (animator != null && !string.IsNullOrEmpty(move.AnimationTrigger))
                    animator.SetTrigger(move.AnimationTrigger);
                return;
            }

            if (intent.Block && grounded)
            {
                State = FighterState.Blocking;
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
        }

        private void SetHorizontal(float value)
        {
            var current = body.linearVelocity;
            current.x = value;
            body.linearVelocity = current;
        }

        public bool ReceiveHit(HitInfo info)
        {
            if (!roundActive || !IsAlive || immunity > 0)
                return false;
            var result = DamageResolver.Resolve(info.Zone, info.Move.BodyDamage, info.Move.LegDamage,
                Guard, info.Move.GuardCost, info.Move.StunFrames, comboHits, accumulatedStun,
                State == FighterState.Blocking, info.Move == info.Attacker.character.Special, false);
            Health = Mathf.Max(0, Health - result.Damage);
            Guard = result.Guard;
            move = null;
            comboMove = null;
            stun = result.StunFrames;
            accumulatedStun += stun;
            comboHits++;
            State = !IsAlive ? FighterState.KnockedOut : result.GuardBroken ? FighterState.GuardBreak : FighterState.HitStun;
            body.AddForce(info.Direction * info.Move.Pushback, ForceMode.VelocityChange);
            Special = Mathf.Min(character.MaxSpecial, Special + info.Move.GaugeGain / 2);
            info.Attacker.Special = Mathf.Min(info.Attacker.character.MaxSpecial, info.Attacker.Special + info.Move.GaugeGain);
            HitStopService.Apply(info.Attacker, this, info.Move.HitStopFrames);
            HitResolved?.Invoke(info, result);
            if (!IsAlive)
                KnockedOut?.Invoke(this);
            return true;
        }

        internal void Freeze(int frames)
        {
            if (frames <= 0)
                return;
            if (freeze == 0)
            {
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

        private void EndFreeze()
        {
            if (body == null || !body.isKinematic || freeze < 0)
                return;
            body.isKinematic = wasKinematic;
            if (!wasKinematic)
                body.linearVelocity = velocity;
            if (animator != null)
                animator.speed = animationSpeed;
            freeze = 0;
        }
    }
}
