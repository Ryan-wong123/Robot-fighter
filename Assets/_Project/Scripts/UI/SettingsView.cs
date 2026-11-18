using UnityEngine;
using UnityEngine.UI;
using WobbleBrawl.Core;

namespace WobbleBrawl.UI
{
    public sealed class SettingsView : MonoBehaviour
    {
        private Font font;
        private Text haptics;
        private Text layout;
        private Text quality;
        private Text masterValue;
        private Text musicValue;
        private Text sfxValue;

        public void Build(Font uiFont)
        {
            font = uiFont;
            SliderRow("Master", 150f, GameSettings.Master, GameSettings.SetMaster, out masterValue);
            SliderRow("Music", 88f, GameSettings.Music, GameSettings.SetMusic, out musicValue);
            SliderRow("SFX", 26f, GameSettings.Sfx, GameSettings.SetSfx, out sfxValue);
            haptics = ButtonRow("Haptics", -42f, () => { GameSettings.SetHaptics(!GameSettings.Haptics); GameSettings.Vibrate(); });
            layout = ButtonRow("Controls", -104f, () => GameSettings.SetLayout(
                GameSettings.Layout == ControlLayout.Standard ? ControlLayout.Mirrored : ControlLayout.Standard));
            quality = ButtonRow("Graphics", -166f, () => GameSettings.SetQuality(
                (QualitySettings.GetQualityLevel() + 1) % Mathf.Max(1, QualitySettings.names.Length)));
            GameSettings.Changed += Render;
            Render();
        }

        private void OnDestroy() => GameSettings.Changed -= Render;

        private void Render()
        {
            masterValue.text = $"{Mathf.RoundToInt(GameSettings.Master * 100f)}%";
            musicValue.text = $"{Mathf.RoundToInt(GameSettings.Music * 100f)}%";
            sfxValue.text = $"{Mathf.RoundToInt(GameSettings.Sfx * 100f)}%";
            haptics.text = GameSettings.Haptics ? "Haptics   On" : "Haptics   Off";
            layout.text = "Controls   " + GameSettings.Layout;
            string[] names = QualitySettings.names;
            quality.text = names.Length > 0 ? "Graphics   " + names[QualitySettings.GetQualityLevel()] : "Graphics";
        }

        private void SliderRow(string name, float y, float initial, UnityEngine.Events.UnityAction<float> apply, out Text value)
        {
            Text heading = Label(name, new Vector2(-235f, y), new Vector2(160, 46));
            heading.text = name;
            value = Label(name + " Value", new Vector2(245f, y), new Vector2(80, 46));
            var node = Node(name + " Slider", transform, new Vector2(30f, y), new Vector2(310, 46));
            var slider = node.AddComponent<Slider>();
            var track = Node("Track", node.transform, Vector2.zero, new Vector2(310, 12)).AddComponent<Image>();
            track.color = new Color(.25f, .30f, .40f);
            var fill = Node("Fill", track.transform, Vector2.zero, Vector2.zero).AddComponent<Image>();
            Stretch(fill.rectTransform);
            fill.color = new Color(.64f, .93f, .38f);
            slider.fillRect = fill.rectTransform;
            var handle = Node("Handle", node.transform, Vector2.zero, new Vector2(22, 38)).AddComponent<Image>();
            handle.color = Color.white;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.SetValueWithoutNotify(initial);
            slider.onValueChanged.AddListener(apply);
        }

        private Text ButtonRow(string name, float y, UnityEngine.Events.UnityAction action)
        {
            var node = Node(name, transform, new Vector2(0, y), new Vector2(560, 48));
            var image = node.AddComponent<Image>();
            image.color = new Color(.21f, .26f, .34f);
            var button = node.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            var label = Label(name + " Caption", Vector2.zero, new Vector2(550, 46), node.transform);
            return label;
        }

        private Text Label(string name, Vector2 point, Vector2 size, Transform parent = null)
        {
            var label = Node(name, parent != null ? parent : transform, point, size).AddComponent<Text>();
            label.font = font;
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject Node(string name, Transform parent, Vector2 point, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = point;
            rect.sizeDelta = size;
            return node;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
