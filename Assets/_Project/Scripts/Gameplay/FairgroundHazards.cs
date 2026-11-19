using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    /// <summary>Alternating pads and moving side bounds; no combat damage or runtime spawning.</summary>
    [RequireComponent(typeof(HazardController))]
    [DefaultExecutionOrder(-50)]
    public sealed class FairgroundHazards : MonoBehaviour, IHazardAwareness
    {
        [SerializeField] private FairgroundDef definition;
        [SerializeField] private Fighter[] fighters;
        [SerializeField] private Rigidbody[] walls;
        [SerializeField] private Transform[] wallVisuals;
        [SerializeField] private Transform[] padWarnings;
        [SerializeField] private Transform[] wallWarnings;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip padWarning;
        [SerializeField] private AudioClip pumpWarning;
        [SerializeField] private AudioClip boing;
        private HazardController cycle;
        private Vector3[] wallRest;
        private Vector3[] wallScales;
        private bool squeezeCycle;
        private bool nextSqueeze;

        public float CurrentHalfWidth { get; private set; }
        public bool PadsActive => !squeezeCycle && cycle.Phase == HazardPhase.Active;

        private void Awake()
        {
            cycle = GetComponent<HazardController>();
            wallRest = new Vector3[walls.Length];
            wallScales = new Vector3[wallVisuals.Length];
            for (int index = 0; index < walls.Length; index++)
            {
                wallRest[index] = walls[index].position;
                wallScales[index] = wallVisuals[index].localScale;
            }
            CurrentHalfWidth = definition.HalfWidth;
        }

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
                squeezeCycle = nextSqueeze = false;
                ResetWalls();
                audioSource.Stop();
                foreach (var fighter in fighters)
                {
                    fighter.SetStageJumpMultiplier(1f);
                }
            }
            else if (phase == HazardPhase.Telegraph)
            {
                squeezeCycle = nextSqueeze;
                nextSqueeze = !nextSqueeze;
                audioSource.PlayOneShot(squeezeCycle ? pumpWarning : padWarning);
            }
            bool visible = phase == HazardPhase.Telegraph || phase == HazardPhase.Active;
            for (int index = 0; index < padWarnings.Length; index++)
            {
                padWarnings[index].gameObject.SetActive(visible && !squeezeCycle);
                padWarnings[index].localScale = Vector3.one;
            }
            for (int index = 0; index < wallWarnings.Length; index++)
            {
                wallWarnings[index].gameObject.SetActive(visible && squeezeCycle);
            }
        }

        private void ResetWalls()
        {
            CurrentHalfWidth = definition.HalfWidth;
            for (int index = 0; index < walls.Length; index++)
            {
                walls[index].position = wallRest[index];
                wallVisuals[index].localScale = wallScales[index];
            }
        }

        private void FixedUpdate()
        {
            bool warning = cycle.Phase == HazardPhase.Telegraph;
            float weight = squeezeCycle && cycle.Phase == HazardPhase.Active
                ? FairgroundRules.Squeeze(cycle.PhaseProgress, definition.SqueezeRampFraction) : 0f;
            CurrentHalfWidth = FairgroundRules.HalfWidth(definition.HalfWidth, definition.WidthReduction, weight);
            for (int index = 0; index < walls.Length; index++)
            {
                float direction = index == 0 ? -1f : 1f;
                Vector3 target = wallRest[index];
                target.x -= direction * (definition.HalfWidth - CurrentHalfWidth);
                walls[index].MovePosition(target);
            }
            foreach (var fighter in fighters)
            {
                fighter.ConstrainToStage(-CurrentHalfWidth, CurrentHalfWidth);
                bool onPad = false;
                foreach (float center in definition.PadCenters)
                {
                    Vector3 position = fighter.transform.position;
                    onPad |= FairgroundRules.OnPad(position.x, position.y, center, definition.PadHalfWidth,
                        definition.ContactHeight);
                }
                fighter.SetStageJumpMultiplier(PadsActive && onPad ? definition.JumpMultiplier : 1f);
                if (!PadsActive)
                {
                    continue;
                }
                foreach (float center in definition.PadCenters)
                {
                    Vector3 position = fighter.transform.position;
                    if (FairgroundRules.OnPad(position.x, position.y, center, definition.PadHalfWidth,
                        definition.ContactHeight) && fighter.TryBounceFromStage(definition.JumpMultiplier))
                    {
                        audioSource.PlayOneShot(boing);
                        break;
                    }
                }
            }
            if (!warning)
            {
                return;
            }
            float pulse = .5f + .5f * Mathf.Sin(cycle.PhaseElapsed * definition.WarningPulseHz * Mathf.PI * 2f);
            for (int index = 0; index < wallVisuals.Length; index++)
            {
                wallVisuals[index].localScale = wallScales[index] *
                    (1f + (squeezeCycle ? definition.WarningBulge * pulse : 0f));
            }
            foreach (var marker in padWarnings)
            {
                marker.localScale = Vector3.one * (1f + definition.WarningBulge * 2f * pulse);
            }
        }

        private void LateUpdate()
        {
            if (cycle.Phase != HazardPhase.Telegraph)
            {
                for (int index = 0; index < wallVisuals.Length; index++)
                {
                    wallVisuals[index].localScale = wallScales[index];
                }
            }
        }

        public bool TryGetAvoidance(Vector3 position, out FighterIntent intent)
        {
            if (squeezeCycle)
            {
                float safe = FairgroundRules.HalfWidth(definition.HalfWidth, definition.WidthReduction, 1f) - 1f;
                intent = default;
                if (Mathf.Abs(position.x) <= safe)
                {
                    return false;
                }
                intent.Move = position.x < 0f ? 1f : -1f;
                return true;
            }
            foreach (float center in definition.PadCenters)
            {
                if (HazardController.TryMoveAway(position.x, center, definition.PadHalfWidth + .4f, out intent))
                {
                    return true;
                }
            }
            intent = default;
            return false;
        }
    }
}
