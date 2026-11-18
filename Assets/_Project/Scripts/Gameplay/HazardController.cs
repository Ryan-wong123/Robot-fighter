using System;
using System.Collections.Generic;
using UnityEngine;
using WobbleBrawl.Data;

namespace WobbleBrawl.Gameplay
{
    public enum HazardPhase { Inactive, Idle, Telegraph, Active, Cooldown }

    public sealed class HazardController : MonoBehaviour
    {
        [SerializeField] private HazardDef definition;
        [SerializeField] private LayerMask hurtLayers = 1 << 9;
        [SerializeField] private GameObject warningMarker;
        [SerializeField] private GameObject activeMarker;
        [SerializeField] private int seed = 1;

        private readonly Collider[] overlaps = new Collider[16];
        private readonly List<Fighter> affected = new List<Fighter>(2);
        private System.Random random;
        private float remaining;
        private float phaseDuration;

        public event Action<HazardPhase> PhaseChanged;
        public HazardPhase Phase { get; private set; }
        public float PhaseProgress => phaseDuration <= 0f ? 1f : Mathf.Clamp01(1f - remaining / phaseDuration);

        public void BeginRound(int roundIndex)
        {
            if (definition == null || !isActiveAndEnabled)
                return;
            random = new System.Random(unchecked(seed + roundIndex));
            Enter(HazardPhase.Idle, definition.FirstWarningDelay);
        }

        public void BeginSuddenDeath()
        {
            if (definition != null && !definition.AllowSuddenDeath)
                EndRound();
        }

        public void EndRound()
        {
            affected.Clear();
            Enter(HazardPhase.Inactive, 0f);
        }

        private void OnDisable() => EndRound();

        private void FixedUpdate()
        {
            if (Phase == HazardPhase.Inactive)
                return;
            remaining -= Time.fixedDeltaTime;
            if (remaining <= 0f)
            {
                switch (Phase)
                {
                    case HazardPhase.Idle:
                        Enter(HazardPhase.Telegraph, Mathf.Max(HazardDef.MinimumWarningSeconds, definition.WarningSeconds));
                        break;
                    case HazardPhase.Telegraph:
                        affected.Clear();
                        Enter(HazardPhase.Active, definition.ActiveSeconds);
                        break;
                    case HazardPhase.Active:
                        Enter(HazardPhase.Cooldown, definition.CooldownSeconds);
                        break;
                    case HazardPhase.Cooldown:
                        float min = Mathf.Max(0f, definition.IdleSeconds.x);
                        float max = Mathf.Max(min, definition.IdleSeconds.y);
                        Enter(HazardPhase.Idle, Mathf.Lerp(min, max, (float)random.NextDouble()));
                        break;
                }
            }
            if (Phase == HazardPhase.Active)
                ApplyHits();
        }

        private void Enter(HazardPhase phase, float seconds)
        {
            remaining = seconds;
            phaseDuration = seconds;
            bool changed = Phase != phase;
            Phase = phase;
            if (warningMarker != null)
                warningMarker.SetActive(phase == HazardPhase.Telegraph);
            if (activeMarker != null)
                activeMarker.SetActive(phase == HazardPhase.Active);
            if (changed)
                PhaseChanged?.Invoke(phase);
        }

        private void ApplyHits()
        {
            if (definition.Hit == null)
                return;

            Vector3 center = transform.position;
            int count = Physics.OverlapBoxNonAlloc(center, definition.Size * .5f, overlaps,
                transform.rotation, hurtLayers, QueryTriggerInteraction.Collide);
            for (int index = 0; index < count && Phase == HazardPhase.Active; index++)
            {
                if (!overlaps[index].TryGetComponent(out Hurtbox hurtbox))
                    continue;
                Fighter fighter = hurtbox.Owner;
                if (fighter == null || affected.Contains(fighter))
                    continue;
                float sign = fighter.transform.position.x < center.x ? -1f : 1f;
                if (fighter.ReceiveHazardHit(new HitInfo(null, definition.Hit, hurtbox.Zone,
                        hurtbox.transform.position, Vector3.right * sign)) && Phase == HazardPhase.Active)
                    affected.Add(fighter);
            }
        }
    }
}
