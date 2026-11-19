using System.Collections.Generic;
using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    /// <summary>Reuses authored paw/spiral meshes and the shared seeded hazard clock.</summary>
    [RequireComponent(typeof(HazardController))]
    [DefaultExecutionOrder(-50)]
    public sealed class GiantSandboxHazards : MonoBehaviour, IHazardAwareness
    {
        [SerializeField] private GiantSandboxDef definition;
        [SerializeField] private Fighter[] fighters;
        [SerializeField] private MoveDef pawHit;
        [SerializeField] private Transform paw;
        [SerializeField] private Transform shadow;
        [SerializeField] private Transform whiskers;
        [SerializeField] private Transform spiral;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip purr;
        [SerializeField] private AudioClip swirl;
        [SerializeField] private AudioClip thump;
        private readonly HashSet<Fighter> affected = new HashSet<Fighter>(2);
        private HazardController cycle;
        private bool sandCycle, nextSand, fromLeft = true, nextFromLeft = true;
        private int patchIndex;
        private float previousPaw;
        public bool IsSandCycle => sandCycle;
        public float SandCenter { get; private set; }

        private void Awake() => cycle = GetComponent<HazardController>();
        private void OnEnable() => cycle.PhaseChanged += OnPhaseChanged;

        private void OnDisable()
        {
            cycle.PhaseChanged -= OnPhaseChanged;
            OnPhaseChanged(HazardPhase.Inactive);
        }

        private void OnPhaseChanged(HazardPhase phase)
        {
            if (phase == HazardPhase.Inactive)
            {
                sandCycle = nextSand = false;
                fromLeft = nextFromLeft = true;
                patchIndex = 0;
                affected.Clear();
                audioSource.Stop();
            }
            if (phase == HazardPhase.Telegraph)
            {
                sandCycle = nextSand;
                nextSand = !nextSand;
                affected.Clear();
                if (sandCycle)
                {
                    SandCenter = definition.SandCenters[patchIndex % definition.SandCenters.Length];
                    patchIndex++;
                    spiral.position = new Vector3(SandCenter, .035f, 0f);
                    spiral.rotation = Quaternion.identity;
                    audioSource.PlayOneShot(swirl);
                }
                else
                {
                    fromLeft = nextFromLeft;
                    nextFromLeft = !nextFromLeft;
                    previousPaw = SandboxRules.PawPosition(0f, definition.PawTravelHalfWidth, fromLeft);
                    paw.position = new Vector3(previousPaw, 0f, 0f);
                    whiskers.position = new Vector3(fromLeft ? -6.5f : 6.5f, 1.2f, 0f);
                    whiskers.rotation = Quaternion.Euler(0f, fromLeft ? 0f : 180f, 0f);
                    audioSource.PlayOneShot(purr);
                }
            }
            bool warning = phase == HazardPhase.Telegraph;
            bool active = phase == HazardPhase.Active;
            paw.gameObject.SetActive(active && !sandCycle);
            shadow.gameObject.SetActive((warning || active) && !sandCycle);
            whiskers.gameObject.SetActive(warning && !sandCycle);
            spiral.gameObject.SetActive((warning || active) && sandCycle);
            spiral.localScale = Vector3.one;
            foreach (var fighter in fighters)
            {
                fighter.SetStageMovementMultiplier(1f);
            }
        }

        private void FixedUpdate()
        {
            foreach (var fighter in fighters)
            {
                fighter.ConstrainToStage(-definition.HalfWidth, definition.HalfWidth);
                Vector3 position = fighter.transform.position;
                bool slowed = sandCycle && cycle.Phase == HazardPhase.Active &&
                    SandboxRules.InSand(position.x, SandCenter, definition.SandHalfWidth, position.y,
                        definition.GroundContactHeight);
                fighter.SetStageMovementMultiplier(slowed ? definition.MovementMultiplier : 1f);
            }
            if (cycle.Phase == HazardPhase.Telegraph && !sandCycle)
            {
                float sign = fromLeft ? -1f : 1f;
                shadow.position = new Vector3(sign * Mathf.Lerp(9f, 6f, cycle.PhaseProgress), .025f, 0f);
                float twitch = Mathf.Sin(cycle.PhaseElapsed * definition.WarningPulseHz * Mathf.PI * 2f);
                whiskers.localScale = new Vector3(1f, 1f + .12f * twitch, 1f);
            }
            if (cycle.Phase != HazardPhase.Active || sandCycle)
            {
                return;
            }
            float progress = cycle.PhaseElapsed / definition.SweepSeconds;
            paw.gameObject.SetActive(progress <= 1f);
            shadow.gameObject.SetActive(progress <= 1f);
            if (progress > 1f)
            {
                return;
            }
            float current = SandboxRules.PawPosition(progress, definition.PawTravelHalfWidth, fromLeft);
            paw.position = new Vector3(current, 0f, 0f);
            shadow.position = new Vector3(current, .025f, 0f);
            foreach (var fighter in fighters)
            {
                Vector3 position = fighter.transform.position;
                if (affected.Contains(fighter) || !SandboxRules.PawContact(previousPaw, current, position.x,
                    definition.PawHalfWidth, position.y, definition.JumpClearance,
                    fighter.State == FighterState.Blocking))
                {
                    continue;
                }
                var direction = fromLeft ? Vector3.right : Vector3.left;
                if (fighter.ReceiveHazardHit(new HitInfo(null, pawHit, HurtZone.Body, position, direction)))
                {
                    affected.Add(fighter);
                    audioSource.PlayOneShot(thump);
                }
            }
            previousPaw = current;
        }

        private void Update()
        {
            if (!spiral.gameObject.activeSelf)
            {
                return;
            }
            spiral.Rotate(0f, definition.SpiralDegreesPerSecond * Time.deltaTime, 0f, Space.World);
            if (cycle.Phase == HazardPhase.Telegraph)
            {
                float pulse = Mathf.Sin(cycle.PhaseElapsed * definition.WarningPulseHz * Mathf.PI * 2f);
                spiral.localScale = Vector3.one * (1f + .08f * pulse);
            }
        }

        public bool TryGetAvoidance(Vector3 position, out FighterIntent intent)
        {
            intent = default;
            if (sandCycle)
            {
                return HazardController.TryMoveAway(position.x, SandCenter, definition.SandHalfWidth + .4f, out intent);
            }
            // Wait under guard, then jump when the sweep is close enough to clear it.
            intent.Jump = cycle.Phase == HazardPhase.Active && Mathf.Abs(paw.position.x - position.x) < 2.5f;
            intent.Block = !intent.Jump;
            return true;
        }
    }
}
