using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WobbleBrawl.Flow;

namespace WobbleBrawl.UI
{
    [RequireComponent(typeof(AppStateMachine))]
    public sealed class AppFlowView : MonoBehaviour
    {
        private static readonly Color Navy = new Color(.075f, .094f, .16f);
        private static readonly Color Cream = new Color(1f, .96f, .86f);
        private static readonly Color Green = new Color(.64f, .93f, .38f);
        private AppStateMachine app;
        private GameObject panel;
        private Text title;
        private Text description;
        private Button primary;
        private Button secondary;
        private Text primaryText;
        private Text secondaryText;
        private Font font;

        private void Awake()
        {
            app = GetComponent<AppStateMachine>();
            if (!app.IsPrimary)
                return;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = Child(transform, "App Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 20;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            panel = Child(canvas.transform, "App Screen", typeof(CanvasRenderer), typeof(Image));
            panel.GetComponent<Image>().color = Navy;
            var bounds = (RectTransform)panel.transform;
            bounds.anchorMin = Vector2.zero;
            bounds.anchorMax = Vector2.one;
            bounds.offsetMin = bounds.offsetMax = Vector2.zero;
            title = Label(panel.transform, "Title", 60, 126f);
            description = Label(panel.transform, "Description", 24, 47f);
            primary = CreateButton("Primary Action", -38f, Green, out primaryText);
            secondary = CreateButton("Secondary Action", -114f, Cream, out secondaryText);
            primary.onClick.AddListener(PrimaryAction);
            secondary.onClick.AddListener(SecondaryAction);
            var input = new GameObject("App Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            input.transform.SetParent(transform, false);
            panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (app.IsPrimary)
                app.Changed += Render;
        }

        private void OnDisable()
        {
            if (app != null)
                app.Changed -= Render;
        }

        private void Render()
        {
            bool visible = app.State != AppState.Match || app.IsLoading;
            panel.SetActive(visible);
            if (!visible)
                return;

            primary.gameObject.SetActive(!app.IsLoading);
            secondary.gameObject.SetActive(!app.IsLoading && app.State != AppState.MainMenu);
            if (app.IsLoading)
            {
                title.text = "Loading";
                description.text = "";
                return;
            }

            switch (app.State)
            {
                case AppState.MainMenu:
                    title.text = "Wobble Brawl";
                    description.text = "";
                    primaryText.text = "Quick Fight";
                    break;
                case AppState.Selection:
                    title.text = "Quick Fight";
                    description.text = "Blobby vs AI · Combat Sandbox";
                    primaryText.text = "Start Match";
                    secondaryText.text = "Back";
                    break;
                case AppState.Results:
                    title.text = app.Result == RoundResult.PlayerWins ? "You Win" :
                        app.Result == RoundResult.OpponentWins ? "Opponent Wins" : "Draw";
                    description.text = "";
                    primaryText.text = "Rematch";
                    secondaryText.text = "Main Menu";
                    break;
            }
        }

        private void PrimaryAction()
        {
            if (app.State == AppState.MainMenu)
                app.OpenSelection();
            else
                app.StartMatch();
        }

        private void SecondaryAction() => app.ReturnToMenu();

        private Button CreateButton(string name, float y, Color background, out Text caption)
        {
            var target = Child(panel.transform, name, typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Place((RectTransform)target.transform, 310f, 58f, y);
            target.GetComponent<Image>().color = background;
            caption = Label(target.transform, "Caption", 27, 0f);
            caption.color = Navy;
            caption.fontStyle = FontStyle.Bold;
            var button = target.GetComponent<Button>();
            button.targetGraphic = target.GetComponent<Image>();
            return button;
        }

        private Text Label(Transform parent, string name, int size, float y)
        {
            var node = Child(parent, name, typeof(CanvasRenderer), typeof(Text));
            var label = node.GetComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Cream;
            label.raycastTarget = false;
            Place(label.rectTransform, 640f, 78f, y);
            return label;
        }

        private static void Place(RectTransform rect, float width, float height, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static GameObject Child(Transform parent, string name, params System.Type[] components)
        {
            var node = new GameObject(name, components);
            node.transform.SetParent(parent, false);
            return node;
        }
    }
}
