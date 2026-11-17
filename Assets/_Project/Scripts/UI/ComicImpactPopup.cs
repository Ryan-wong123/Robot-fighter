using UnityEngine;
using UnityEngine.UI;
using WobbleBrawl.Gameplay;

namespace WobbleBrawl.UI
{
    public sealed class ComicImpactPopup : MonoBehaviour
    {
        [SerializeField] private Fighter fighter;
        [SerializeField] private Camera fightCamera;
        [SerializeField] private Text label;
        [SerializeField, Min(0.01f)] private float visibleSeconds = 0.35f;
        private float hideAt;
        private Vector3 point;

        private void OnEnable()
        {
            label.enabled = false;
            fighter.HitResolved += Show;
        }

        private void OnDisable()
        {
            fighter.HitResolved -= Show;
            label.enabled = false;
        }

        private void Show(HitInfo hit, DamageResult result)
        {
            if (result.Damage == 0)
                return;

            point = hit.Point;
            label.text = result.GuardBroken ? "CRACK!" : result.Blocked ? "CLANG!" : "BONK!";
            hideAt = Time.unscaledTime + visibleSeconds;
            label.enabled = true;
        }

        private void LateUpdate()
        {
            if (!label.enabled)
                return;

            Vector3 screenPoint = fightCamera.WorldToScreenPoint(point);
            label.enabled = Time.unscaledTime < hideAt && screenPoint.z > 0f;
            label.rectTransform.position = screenPoint;
        }
    }
}
