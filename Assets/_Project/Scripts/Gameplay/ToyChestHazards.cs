using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    [RequireComponent(typeof(HazardController))]
    public sealed class ToyChestHazards : MonoBehaviour, IHazardAwareness
    {
        [SerializeField] private Fighter player, opponent;
        [SerializeField] private Transform deck, tiltWarning;
        [SerializeField] private Transform[] blocks, shadows;
        [SerializeField] private AudioSource warning, music;
        [SerializeField] private AudioClip rattle, creak;
        [SerializeField] private MoveDef blockHit;
        [SerializeField] private float dropSeconds = .65f, dropHeight = 5f, slideSpeed = 1.8f, tiltDegrees = 7f;
        private HazardController cycle;
        private Collider[] obstacles;
        private bool rain, playerHit, opponentHit;
        private int direction;
        private Quaternion rest;

        private void Awake()
        {
            cycle = GetComponent<HazardController>();
            rest = deck.localRotation;
            obstacles = new Collider[blocks.Length];
            for (int i = 0; i < blocks.Length; i++) obstacles[i] = blocks[i].GetComponent<Collider>();
        }

        private void OnEnable() => cycle.PhaseChanged += Change;
        private void OnDisable()
        {
            cycle.PhaseChanged -= Change;
            Change(HazardPhase.Inactive);
        }

        private void Change(HazardPhase phase)
        {
            deck.localRotation = rest;

            warning.Stop();
            tiltWarning.gameObject.SetActive(false);
            if (phase == HazardPhase.Inactive) { rain = false; direction = -1; }
            if (phase == HazardPhase.Telegraph)
            {
                rain = !rain;
                if (!rain)
                {
                    direction = -direction;
                    tiltWarning.gameObject.SetActive(true);
                    tiltWarning.localRotation = Quaternion.Euler(0, direction < 0 ? 180f : 0f, 0);
                }
                warning.PlayOneShot(rain ? rattle : creak);
            }
            playerHit = opponentHit = false;
            for (int i = 0; i < blocks.Length; i++)
            {
                obstacles[i].enabled = false;
                blocks[i].gameObject.SetActive(rain && phase == HazardPhase.Active);
                shadows[i].gameObject.SetActive(rain && phase == HazardPhase.Telegraph);
                if (phase == HazardPhase.Active && rain)
                    blocks[i].position = shadows[i].position + Vector3.up * dropHeight;
            }
        }

        public bool TryGetAvoidance(Vector3 position, out FighterIntent intent)
        {
            if (rain)
                foreach (var shadow in shadows)
                    if (HazardController.TryMoveAway(position.x, shadow.position.x, 1.15f, out intent))
                        return true;
            intent = default;
            return false;
        }

        private void Update()
        {
            float pace = opponent.IsBossSecondPhase ? 1.25f : 1f;
            music.volume = (opponent.IsBossSecondPhase ? .44f : .35f) * GameSettings.Music;
            music.pitch = !rain && cycle.Phase == HazardPhase.Telegraph ? pace * .68f : pace;
        }

        private void FixedUpdate()
        {
            if (cycle.Phase == HazardPhase.Telegraph && rain)
            {
                float size = Mathf.Lerp(.3f, 1.5f, cycle.PhaseProgress);
                foreach (var shadow in shadows) shadow.localScale = new Vector3(size, .02f, size);
            }

            if (cycle.Phase != HazardPhase.Active) return;
            if (!rain)
            {
                float amount = Mathf.Sin(cycle.PhaseProgress * Mathf.PI);
                deck.localRotation = rest * Quaternion.Euler(0, 0, -direction * tiltDegrees * amount);
                float step = direction * slideSpeed * amount * Time.fixedDeltaTime;
                player.ApplyGroundDrift(step);
                opponent.ApplyGroundDrift(step);
                return;
            }

            float drop = Mathf.Clamp01(cycle.PhaseElapsed / dropSeconds);
            for (int i = 0; i < blocks.Length && cycle.Phase == HazardPhase.Active; i++)
            {
                var center = shadows[i].position;
                blocks[i].position = center + Vector3.up * (.4f + dropHeight * (1f - drop * drop));
                if (drop < 1f || obstacles[i].enabled) continue;
                obstacles[i].enabled = true;
                Strike(player, center, ref playerHit);
                if (cycle.Phase == HazardPhase.Active) Strike(opponent, center, ref opponentHit);
            }
        }

        private void Strike(Fighter fighter, Vector3 center, ref bool hit)
        {
            if (hit || Mathf.Abs(fighter.transform.position.x - center.x) > .72f ||
                fighter.transform.position.y > center.y + .8f) return;
            float sign = fighter.transform.position.x < center.x ? -1f : 1f;
            hit = fighter.ReceiveHazardHit(new HitInfo(null, blockHit, HurtZone.Body, center, Vector3.right * sign));
        }
    }
}
