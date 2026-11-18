using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    [RequireComponent(typeof(HazardController))]
    public sealed class BathtubHazards : MonoBehaviour, IHazardAwareness
    {
        [SerializeField] private Fighter[] fighters;
        [SerializeField] private Transform waterSurface;
        [SerializeField] private GameObject waterWarning;
        [SerializeField] private GameObject duckWarning;
        [SerializeField] private Transform duck;
        [SerializeField] private Transform wave;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip drip;
        [SerializeField] private AudioClip quack;
        [SerializeField] private MoveDef waveHit;
        [SerializeField] private float waterHeight = 1.2f;
        [SerializeField] private float waveStartX = 7f;
        [SerializeField, Min(.1f)] private float waveTravelSeconds = 3.4f;
        [SerializeField, Min(.1f)] private float waveHalfWidth = .75f;
        [SerializeField, Min(.1f)] private float waveHeight = .65f;
        [SerializeField] private Vector3 wetMultipliers = new Vector3(.8f, 1.2f, .55f);

        private HazardController cycle;
        private Vector3 duckHome;
        private bool[] waveHits;
        private bool isDuckCycle;
        private bool nextIsDuck;

        public bool TryGetAvoidance(Vector3 position, out FighterIntent intent)
        {
            intent = default;
            if (!isDuckCycle || !wave.gameObject.activeSelf || position.y > waveHeight)
                return false;
            float distanceAhead = wave.position.x - position.x;
            float lead = 2f * Mathf.Abs(waveStartX) / waveTravelSeconds * .45f;
            if (distanceAhead < -waveHalfWidth || distanceAhead > lead)
                return false;
            intent.Jump = true;
            return true;
        }

        private void Awake()
        {
            if (audioSource != null && !audioSource.TryGetComponent<SettingsAudioSource>(out _))
                audioSource.gameObject.AddComponent<SettingsAudioSource>();
            cycle = GetComponent<HazardController>();
            duckHome = duck.localPosition;
            waveHits = new bool[fighters.Length];
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
                isDuckCycle = nextIsDuck = false;
                if (audioSource != null)
                    audioSource.Stop();
            }
            else if (phase == HazardPhase.Telegraph)
            {
                isDuckCycle = nextIsDuck;
                nextIsDuck = !nextIsDuck;
                System.Array.Clear(waveHits, 0, waveHits.Length);
                if (audioSource != null)
                {
                    var warning = isDuckCycle ? quack : drip;
                    if (warning != null)
                        audioSource.PlayOneShot(warning);
                }
                if (isDuckCycle)
                    SetX(duck, waveStartX - .6f);
            }

            if (waterWarning != null)
                waterWarning.SetActive(phase == HazardPhase.Telegraph && !isDuckCycle);
            if (duckWarning != null)
                duckWarning.SetActive(phase == HazardPhase.Telegraph && isDuckCycle);
            if (wave != null)
                wave.gameObject.SetActive(phase == HazardPhase.Active && isDuckCycle);
            if (waterSurface != null)
                waterSurface.gameObject.SetActive(!isDuckCycle &&
                    (phase == HazardPhase.Active || phase == HazardPhase.Cooldown));

            if (phase == HazardPhase.Idle || phase == HazardPhase.Inactive)
            {
                if (duck != null)
                    duck.localPosition = duckHome;
                if (waterSurface != null)
                    SetHeight(waterSurface, 0f);
            }
            if (phase != HazardPhase.Active && phase != HazardPhase.Cooldown)
                UpdateWetFighters(0f);
        }

        private void FixedUpdate()
        {
            float progress = cycle.PhaseProgress;
            if (cycle.Phase == HazardPhase.Active)
            {
                if (isDuckCycle)
                    UpdateWave(cycle.PhaseElapsed / waveTravelSeconds);
                else
                    UpdateWater(Mathf.Clamp01(progress * 4f / 3f) * waterHeight);
            }
            else if (cycle.Phase == HazardPhase.Cooldown && !isDuckCycle)
                UpdateWater((1f - progress) * waterHeight);
        }

        private void UpdateWater(float height)
        {
            SetHeight(waterSurface, height);
            UpdateWetFighters(height);
        }

        private void UpdateWetFighters(float height)
        {
            foreach (var fighter in fighters)
            {
                if (fighter == null)
                    continue;
                bool wet = height > .05f && fighter.transform.position.y < height;
                fighter.SetWater(wet ? wetMultipliers.x : 1f, wet ? wetMultipliers.y : 1f,
                    wet ? wetMultipliers.z : 1f);
            }
        }

        private void UpdateWave(float progress)
        {
            if (progress >= 1f)
            {
                SetX(duck, -waveStartX);
                wave.gameObject.SetActive(false);
                return;
            }
            float x = Mathf.Lerp(waveStartX, -waveStartX, progress);
            SetX(wave, x);
            SetX(duck, x);
            for (int index = 0; index < fighters.Length; index++)
            {
                var fighter = fighters[index];
                Vector3 feet = fighter.transform.position;
                if (waveHits[index] || Mathf.Abs(feet.x - x) > waveHalfWidth || feet.y > waveHeight)
                    continue;
                waveHits[index] = fighter.ReceiveHazardHit(new HitInfo(null, waveHit, HurtZone.Legs,
                    feet + Vector3.up * .45f, Vector3.left));
            }
        }

        private static void SetHeight(Transform target, float height)
        {
            Vector3 position = target.localPosition;
            position.y = height;
            target.localPosition = position;
        }

        private static void SetX(Transform target, float x)
        {
            Vector3 position = target.localPosition;
            position.x = x;
            target.localPosition = position;
        }
    }
}
