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
        private static ImpactPalette palette;
        private ComicImpactShape shape;
        private ImpactStyle style;
        private float startedAt;
        private Vector3 point;
        private int popCount;
        private int flashUntilFrame;
        private ImpactKind currentKind;
        private Color tint;

        private void OnEnable()
        {
            label.enabled = false;
            if (palette == null)
            {
                var asset = Resources.Load<TextAsset>("CombatImpactPalette");
                if (asset != null)
                    palette = JsonUtility.FromJson<ImpactPalette>(asset.text);
            }
            if (shape == null)
            {
                var visual = new GameObject("Impact Burst", typeof(RectTransform), typeof(ComicImpactShape));
                visual.transform.SetParent(transform.parent, false);
                visual.transform.SetSiblingIndex(transform.GetSiblingIndex());
                shape = visual.GetComponent<ComicImpactShape>();
                shape.raycastTarget = false;
                label.fontStyle = FontStyle.Bold;
                label.rectTransform.sizeDelta = new Vector2(260f, 90f);
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.16f, .08f, .15f, 1f);
                outline.effectDistance = new Vector2(2f, -2f);
            }
            shape.enabled = false;
            fighter.HitResolved += Show;
        }

        private void OnDisable()
        {
            fighter.HitResolved -= Show;
            label.enabled = false;
            if (shape != null)
                shape.enabled = false;
        }

        private void OnDestroy()
        {
            if (shape != null)
                Destroy(shape.gameObject);
        }

        private void Show(HitInfo hit, DamageResult result)
        {
            if (palette == null || (result.Damage == 0 && result.StunFrames == 0 && !result.Blocked))
                return;

            ImpactKind kind = palette.Classify(result, !fighter.IsAlive, hit.Attacker != null && hit.Attacker.IsSpecialMove(hit.Move));
            style = palette.styles[(int)kind];
            currentKind = kind;
            tint = style.Tint;
            point = hit.Point;
            label.text = style.text;
            label.color = tint;
            label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, ++popCount % 2 == 0 ? -9f : 9f);
            startedAt = Time.unscaledTime;
            label.enabled = true;
            shape.enabled = true;
            flashUntilFrame = kind == ImpactKind.Heavy || kind == ImpactKind.Special
                ? Time.frameCount + 2 : -1;
            shape.Show(kind, flashUntilFrame >= 0 ? Color.white : tint);
        }

        private void LateUpdate()
        {
            if (!label.enabled)
                return;
            float elapsed = Time.unscaledTime - startedAt;
            if (flashUntilFrame >= 0 && Time.frameCount >= flashUntilFrame)
            {
                shape.Show(currentKind, tint);
                flashUntilFrame = -1;
            }
            Vector3 screenPoint = fightCamera.WorldToScreenPoint(point);
            if (elapsed >= style.lifetime || screenPoint.z <= 0f)
            {
                label.enabled = shape.enabled = false;
                return;
            }
            float progress = elapsed / style.lifetime;
            float scale = progress < .2f ? Mathf.Lerp(.6f, 1.3f, progress / .2f) :
                Mathf.Lerp(1.3f, 1f, (progress - .2f) / .8f);
            label.rectTransform.position = screenPoint + Vector3.up * (24f + 48f * progress);
            label.rectTransform.localScale = Vector3.one * (style.size * scale);
            shape.rectTransform.position = screenPoint + Vector3.up * (14f * progress);
            shape.rectTransform.localScale = Vector3.one * (style.size * scale);
            float alpha = 1f - Mathf.InverseLerp(.58f, 1f, progress);
            label.canvasRenderer.SetAlpha(alpha);
            shape.canvasRenderer.SetAlpha(alpha);
        }
    }
}
