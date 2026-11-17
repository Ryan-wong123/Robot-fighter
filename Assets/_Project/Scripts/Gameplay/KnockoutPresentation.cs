using UnityEngine;

namespace WobbleBrawl.Gameplay
{
    public sealed class KnockoutPresentation : MonoBehaviour
    {
        [SerializeField] private Fighter fighter;
        [SerializeField] private Camera fightCamera;
        [SerializeField, Min(0.01f)] private float duration = 1.4f;
        [SerializeField, Range(0.05f, 1f)] private float slowMotion = 0.35f;
        [SerializeField, Range(0.5f, 1f)] private float zoom = 0.62f;

        private bool playing;
        private float elapsed, originalScale, originalSize;
        private Vector3 originalPosition;

        private void OnEnable() => fighter.KnockedOut += Begin;

        private void OnDisable()
        {
            fighter.KnockedOut -= Begin;
            Restore();
        }

        private void Begin(Fighter loser)
        {
            Restore();
            originalScale = Time.timeScale;
            originalSize = fightCamera.orthographicSize;
            originalPosition = fightCamera.transform.position;
            elapsed = 0f;
            playing = true;
            Time.timeScale = originalScale * slowMotion;
        }

        private void LateUpdate()
        {
            if (!playing)
                return;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= duration)
            {
                Restore();
                return;
            }

            float progress = elapsed / duration;
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.Min(progress / 0.2f, (1f - progress) / 0.3f));
            fightCamera.orthographicSize = originalSize * Mathf.Lerp(1f, zoom, weight);
            Vector3 target = originalPosition;
            target.x = fighter.transform.position.x;
            fightCamera.transform.position = Vector3.Lerp(originalPosition, target, weight * 0.5f);
        }

        private void Restore()
        {
            if (!playing)
                return;

            playing = false;
            Time.timeScale = originalScale;
            if (fightCamera == null)
                return;
            fightCamera.orthographicSize = originalSize;
            fightCamera.transform.position = originalPosition;
        }
    }
}
