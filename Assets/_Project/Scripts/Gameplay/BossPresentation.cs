using UnityEngine;
using WobbleBrawl.Core;

namespace WobbleBrawl.Gameplay
{
    [DefaultExecutionOrder(20)]
    public sealed class BossPresentation : MonoBehaviour
    {
        [SerializeField] private LineRenderer warningRing;
        [SerializeField] private LineRenderer shockwaveRing;
        [SerializeField] private AudioSource warningAudio;
        [SerializeField] private Transform[] springJoints;
        [SerializeField] private float springDegreesPerSpeed = 2.5f;
        [SerializeField, Min(0f)] private float springAngleLimit = 12f;
        [SerializeField, Min(.01f)] private float springSmoothSeconds = .12f;
        [SerializeField, Min(0f)] private float shockwaveRadius = 1.6f;
        private Rigidbody body;
        private float bend, bendVelocity;
        private Fighter fighter;
        private bool warning;
        private readonly Vector3[] ring = new Vector3[32];
        private void Awake()
        {
            fighter = GetComponentInParent<Fighter>();
            body = fighter.GetComponent<Rigidbody>();
            for (int index = 0; index < ring.Length; index++)
            {
                float angle = index * Mathf.PI * 2f / ring.Length;
                ring[index] = new Vector3(Mathf.Cos(angle), .04f, Mathf.Sin(angle) * .35f);
            }
            warningRing.enabled = false;
            shockwaveRing.enabled = false;
        }
        private void OnEnable()
        {
            fighter.PresentationReset += Reset;
        }
        private void OnDisable()
        {
            if (fighter == null) return;
            fighter.PresentationReset -= Reset;
            Reset();
        }
        private void Reset()
        {
            bend = bendVelocity = 0f;
            warning = false;
            warningRing.enabled = false;
            shockwaveRing.enabled = false;
            warningAudio.Stop();
        }
        private void LateUpdate()
        {
            if (MatchPause.IsPaused || fighter.IsHitStopped) return;
            if (fighter.IsAlive)
            {
                bend = Mathf.SmoothDamp(bend, Mathf.Clamp(-body.linearVelocity.x * springDegreesPerSpeed, -springAngleLimit, springAngleLimit),
                    ref bendVelocity, springSmoothSeconds);
                foreach (var joint in springJoints)
                    joint.localRotation *= Quaternion.AngleAxis(bend / springJoints.Length, Vector3.forward);
            }
            bool active = fighter.CanInteractWithItems && fighter.IsBossSpecialWarning;
            if (active && !warning) warningAudio.Play();
            if (!active && warning) warningAudio.Stop();
            warning = active;
            warningRing.enabled = active;
            if (active)
            {
                var center = new Vector3(fighter.BossTargetX, fighter.GetItemGroundHeight(new Vector3(fighter.BossTargetX, fighter.transform.position.y, 0f)), 0f);
                for (int index = 0; index < ring.Length; index++) warningRing.SetPosition(index, center + ring[index]);
            }
            shockwaveRing.enabled = fighter.CanInteractWithItems && fighter.IsBossShockwaveActive;
            if (shockwaveRing.enabled)
            {
                Vector3 center = new Vector3(fighter.transform.position.x, fighter.GetItemGroundHeight(fighter.transform.position), 0f);
                for (int index = 0; index < ring.Length; index++) shockwaveRing.SetPosition(index, center + ring[index] * shockwaveRadius);
            }
        }
    }
}
