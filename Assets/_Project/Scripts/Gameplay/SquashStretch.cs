using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    public sealed class SquashStretch : MonoBehaviour
    {
        [SerializeField] private Fighter fighter;
        [SerializeField] private Transform visual;
        [SerializeField, Min(0.01f)] private float recoverySeconds = 0.18f;
        private Vector3 restScale;
        private float elapsed;

        internal void SetVisual(Transform target)
        {
            visual = target;
            restScale = target.localScale;
            elapsed = recoverySeconds;
        }

        private void OnEnable()
        {
            restScale = visual.localScale;
            elapsed = recoverySeconds;
            fighter.HitResolved += OnHit;
        }

        private void OnDisable()
        {
            fighter.HitResolved -= OnHit;
            visual.localScale = restScale;
        }

        private void OnHit(HitInfo hit, DamageResult result)
        {
            if (result.Damage > 0)
                elapsed = 0f;
        }

        private void LateUpdate()
        {
            if (elapsed >= recoverySeconds)
                return;

            elapsed = Mathf.Min(recoverySeconds, elapsed + Time.unscaledDeltaTime);
            float compression = 1f - elapsed / recoverySeconds;
            visual.localScale = Vector3.Scale(restScale,
                new Vector3(1f + 0.12f * compression, 1f - 0.18f * compression, 1f));
        }
    }
}
