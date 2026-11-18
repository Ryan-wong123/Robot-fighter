using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    [RequireComponent(typeof(HazardController))]
    public sealed class KitchenHazards : MonoBehaviour, IHazardAwareness
    {
        [SerializeField] private Fighter[] fighters;
        [SerializeField] private MoveDef toastHit;
        [SerializeField] private Transform[] toastSlices;
        [SerializeField] private Transform[] launchPoints;
        [SerializeField] private Transform[] landingMarkers;
        [SerializeField] private GameObject hotCoils;
        [SerializeField] private Transform belt;
        [SerializeField] private Transform beltArrows;
        [SerializeField] private AudioSource warningAudio;
        [SerializeField] private AudioClip ticking;
        [SerializeField] private AudioClip mixerWhir;
        [SerializeField, Min(.1f)] private float toastFlight = 1.65f;
        [SerializeField, Min(0)] private float toastStagger = .32f;
        [SerializeField, Min(.1f)] private float toastArc = 2.35f;
        [SerializeField, Min(.1f)] private float toastRadius = .32f;
        [SerializeField, Min(.1f)] private float beltSpeed = 2.3f;
        [SerializeField, Min(.1f)] private float beltReverseSeconds = 1.8f;
        [SerializeField, Min(.1f)] private float beltHalfWidth = 1.2f;
        [SerializeField, Min(0)] private float beltContactHeight = .18f;
        [SerializeField] private LayerMask hurtLayers = 1 << 9;

        private readonly Collider[] contacts = new Collider[16];
        private readonly System.Collections.Generic.HashSet<Fighter> toastHits = new(2);
        private HazardController cycle;
        private bool beltCycle;
        private bool nextIsBelt;

        public bool TryGetAvoidance(Vector3 position, out FighterIntent intent)
        {
            if (beltCycle)
                return HazardController.TryMoveAway(position.x, belt.position.x, beltHalfWidth + .45f, out intent);

            foreach (var marker in landingMarkers)
                if (marker.gameObject.activeSelf &&
                    HazardController.TryMoveAway(position.x, marker.position.x, .9f, out intent))
                    return true;
            foreach (var toast in toastSlices)
                if (toast.gameObject.activeSelf && toast.position.y < 2f &&
                    HazardController.TryMoveAway(position.x, toast.position.x, .75f, out intent))
                    return true;
            intent = default;
            return false;
        }

        private void Awake()
        {
            cycle = GetComponent<HazardController>();
            if (warningAudio != null && !warningAudio.TryGetComponent<SettingsAudioSource>(out _))
                warningAudio.gameObject.AddComponent<SettingsAudioSource>();
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
                beltCycle = nextIsBelt = false;
            else if (phase == HazardPhase.Telegraph)
            {
                beltCycle = nextIsBelt;
                nextIsBelt = !nextIsBelt;
                toastHits.Clear();
                var clip = beltCycle ? mixerWhir : ticking;
                if (warningAudio != null && clip != null)
                    warningAudio.PlayOneShot(clip);
            }

            bool warning = phase == HazardPhase.Telegraph;
            if (hotCoils != null)
                hotCoils.SetActive(warning && !beltCycle);
            foreach (var marker in landingMarkers)
                if (marker != null)
                    marker.gameObject.SetActive(!beltCycle && (warning || phase == HazardPhase.Active));
            if (beltArrows != null)
            {
                beltArrows.gameObject.SetActive(beltCycle && (warning || phase == HazardPhase.Active));
                beltArrows.localRotation = Quaternion.identity;
            }
            foreach (var slice in toastSlices)
                if (slice != null)
                    slice.gameObject.SetActive(false);
            foreach (var fighter in fighters)
                if (fighter != null)
                    fighter.SetConveyorSpeed(0f);
            if (phase == HazardPhase.Inactive && warningAudio != null)
                warningAudio.Stop();
        }

        private void FixedUpdate()
        {
            if (cycle.Phase != HazardPhase.Active)
                return;
            if (beltCycle)
                UpdateBelt();
            else
                UpdateToast();
        }

        private void UpdateToast()
        {
            for (int index = 0; index < toastSlices.Length && cycle.Phase == HazardPhase.Active; index++)
            {
                var slice = toastSlices[index];
                float progress = (cycle.PhaseElapsed - index * toastStagger) / toastFlight;
                bool flying = progress >= 0f && progress <= 1f;
                if (progress > 1f && landingMarkers[index].gameObject.activeSelf)
                    landingMarkers[index].gameObject.SetActive(false);
                if (slice.gameObject.activeSelf != flying)
                    slice.gameObject.SetActive(flying);
                if (!flying)
                    continue;
                Vector3 landing = landingMarkers[index].position + Vector3.up * toastRadius;
                Vector3 position = Vector3.Lerp(launchPoints[index].position, landing, progress);
                position.y += 4f * toastArc * progress * (1f - progress);
                slice.position = position;
                slice.rotation = Quaternion.Euler(0f, 0f, progress * 490f);

                int count = Physics.OverlapSphereNonAlloc(position, toastRadius, contacts,
                    hurtLayers, QueryTriggerInteraction.Collide);
                for (int contact = 0; contact < count && cycle.Phase == HazardPhase.Active; contact++)
                {
                    if (!contacts[contact].TryGetComponent(out Hurtbox hurtbox) ||
                        hurtbox.Owner == null || toastHits.Contains(hurtbox.Owner))
                        continue;
                    if (hurtbox.Owner.ReceiveHazardHit(new HitInfo(null, toastHit, hurtbox.Zone, position, Vector3.right)))
                        toastHits.Add(hurtbox.Owner);
                }
            }
        }

        private void UpdateBelt()
        {
            bool reverse = Mathf.FloorToInt(cycle.PhaseElapsed / beltReverseSeconds) % 2 != 0;
            beltArrows.localRotation = Quaternion.Euler(0f, reverse ? 180f : 0f, 0f);
            foreach (var fighter in fighters)
            {
                Vector3 feet = fighter.transform.position;
                bool standingOnBelt = Mathf.Abs(feet.x - belt.position.x) <= beltHalfWidth &&
                    feet.y <= belt.position.y + beltContactHeight;
                fighter.SetConveyorSpeed(standingOnBelt ? (reverse ? -beltSpeed : beltSpeed) : 0f);
            }
        }
    }
}
