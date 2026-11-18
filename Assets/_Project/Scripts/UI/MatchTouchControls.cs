using UnityEngine;
using UnityEngine.UI;
using WobbleBrawl.Core;
using WobbleBrawl.Gameplay;

namespace WobbleBrawl.UI
{
    public sealed class MatchTouchControls : MonoBehaviour
    {
        private Fighter fighter;
        private RectTransform movement;
        private RectTransform actions;
        private TouchControlButton left;
        private TouchControlButton right;
        private TouchControlButton block;
        private FighterIntent pending;

        public void Initialize(Fighter player, RectTransform safeArea, Font font)
        {
            fighter = player;
            movement = Group(safeArea, "Movement Controls");
            actions = Group(safeArea, "Action Controls");
            left = Make(movement, "←", new Vector2(0, 0), font);
            right = Make(movement, "→", new Vector2(90, 0), font);
            Make(movement, "Jump", new Vector2(45, 82), font).onClick.AddListener(() => pending.Jump = true);
            block = Make(actions, "Block", new Vector2(0, 0), font);
            Make(actions, "Punch", new Vector2(90, 0), font).onClick.AddListener(() => pending.Punch = true);
            Make(actions, "Kick", new Vector2(180, 0), font).onClick.AddListener(() => pending.Kick = true);
            Make(actions, "Special", new Vector2(135, 82), font).onClick.AddListener(() => pending.Special = true);
            Make(actions, "Item", new Vector2(45, 82), font).onClick.AddListener(() => pending.Throw = true);
            GameSettings.Changed += ApplyLayout;
            ApplyLayout();
        }

        private void Update()
        {
            if (fighter == null)
                return;
            if (MatchPause.IsPaused || !fighter.CanInteractWithItems)
            {
                pending = default;
                fighter.SubmitTouchIntent(default);
                return;
            }
            pending.Move = (right.Held ? 1f : 0f) - (left.Held ? 1f : 0f);
            pending.Block = block.Held;
            fighter.SubmitTouchIntent(pending);
            pending = default;
        }

        private void OnDestroy() => GameSettings.Changed -= ApplyLayout;

        private void ApplyLayout()
        {
            bool mirrored = GameSettings.Layout == ControlLayout.Mirrored;
            Position(movement, mirrored, 190f);
            Position(actions, !mirrored, 280f);
        }

        private static void Position(RectTransform group, bool rightSide, float width)
        {
            group.anchorMin = group.anchorMax = new Vector2(rightSide ? 1f : 0f, 0f);
            group.anchoredPosition = new Vector2(rightSide ? -width - 22f : 22f, 24f);
        }

        private static RectTransform Group(Transform parent, string name)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.pivot = Vector2.zero;
            return rect;
        }

        private static TouchControlButton Make(Transform parent, string caption, Vector2 position, Font font)
        {
            var node = new GameObject(caption, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TouchControlButton));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(80f, 70f);
            rect.anchoredPosition = position;
            var image = node.GetComponent<Image>();
            image.color = new Color(.12f, .18f, .25f, .85f);
            var button = node.GetComponent<TouchControlButton>();
            button.targetGraphic = image;
            var textNode = new GameObject("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textNode.transform.SetParent(node.transform, false);
            var label = textNode.GetComponent<Text>();
            label.font = font;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
