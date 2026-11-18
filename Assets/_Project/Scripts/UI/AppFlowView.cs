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
        private RectTransform safeContent;
        private RawImage menuArt;
        private GameObject menuRail;
        private Text title;
        private Text description;
        private Button primary;
        private Button secondary;
        private Button pauseButton;
        private Button[] stageButtons;
        private Text primaryText;
        private Text secondaryText;
        private Font font;
        private EventSystem input;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

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
            Stretch((RectTransform)panel.transform, Vector2.zero, Vector2.one);
            BuildMenuArtwork();
            safeContent = (RectTransform)Child(panel.transform, "Safe Content", typeof(RectTransform)).transform;
            Stretch(safeContent, Vector2.zero, Vector2.one);
            title = Label(safeContent, "Title", 60, 126f);
            description = Label(safeContent, "Description", 24, 47f);
            primary = CreateButton("Primary Action", -38f, Green, out primaryText);
            secondary = CreateButton("Secondary Action", -114f, Cream, out secondaryText);
            stageButtons = new Button[app.StageCount];
            for (int index = 0; index < stageButtons.Length; index++)
            {
                int choice = index;
                var button = CreateButton("Stage " + index, 47f, Cream, out var caption);
                caption.text = app.GetStage(index).Name;
                caption.fontSize = 22;
                button.onClick.AddListener(() => app.SelectStage(choice));
                var rect = (RectTransform)button.transform;
                Place(rect, 218f, 62f, 47f);
                rect.anchoredPosition += Vector2.right * ((index - (stageButtons.Length - 1) * .5f) * 235f);
                stageButtons[index] = button;
            }
            primary.onClick.AddListener(PrimaryAction);
            secondary.onClick.AddListener(SecondaryAction);
            pauseButton = CreateButton("Pause Match", 0f, Cream, out var pauseCaption);
            pauseCaption.text = "Pause";
            var pauseRect = (RectTransform)pauseButton.transform;
            Place(pauseRect, 120f, 48f, -185f, new Vector2(.5f, 1f));
            pauseRect.SetParent(canvas.transform, false);
            pauseButton.onClick.AddListener(app.TogglePause);
            pauseButton.gameObject.SetActive(false);
            var inputObject = new GameObject("App Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            inputObject.transform.SetParent(transform, false);
            input = inputObject.GetComponent<EventSystem>();
            panel.SetActive(false);
        }

        private void BuildMenuArtwork()
        {
            var art = Child(panel.transform, "Bathtub Brawl Illustration", typeof(CanvasRenderer), typeof(RawImage));
            menuArt = art.GetComponent<RawImage>();
            menuArt.texture = Resources.Load<Texture2D>("MainMenuArtwork");
            menuArt.raycastTarget = false;
            Stretch(menuArt.rectTransform, Vector2.zero, Vector2.one);
            menuRail = Child(panel.transform, "Menu Backing", typeof(CanvasRenderer), typeof(Image));
            var backing = menuRail.GetComponent<Image>();
            backing.color = new Color(.075f, .094f, .16f, .84f);
            backing.raycastTarget = false;
            Stretch(backing.rectTransform, Vector2.zero, new Vector2(.40f, 1f));
        }

        private void Update()
        {
            if (app.IsPrimary && Input.GetKeyDown(KeyCode.Escape))
            {
                if (app.State == AppState.Match)
                    app.TogglePause();
                else if (app.State == AppState.Selection)
                    app.ReturnToMenu();
            }
            if (!app.IsPrimary || !panel.activeSelf)
                return;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            if (screenSize == lastScreenSize && safeArea == lastSafeArea)
                return;
            lastScreenSize = screenSize;
            lastSafeArea = safeArea;
            if (screenSize.x <= 0 || screenSize.y <= 0)
                return;
            if (safeArea.width > 0 && safeArea.height > 0)
            {
                Stretch(safeContent, new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y),
                    new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y));
            }
            if (menuArt.texture != null)
            {
                float screenAspect = (float)screenSize.x / screenSize.y;
                float artAspect = (float)menuArt.texture.width / menuArt.texture.height;
                float width = Mathf.Min(1f, screenAspect / artAspect);
                float height = Mathf.Min(1f, artAspect / screenAspect);
                menuArt.uvRect = new Rect((1f - width) * .5f, (1f - height) * .5f, width, height);
            }
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
            bool visible = app.State != AppState.Match || app.IsLoading || app.IsPaused;
            pauseButton.gameObject.SetActive(app.State == AppState.Match && !app.IsLoading && !app.IsPaused);
            pauseButton.interactable = app.CanPause;
            panel.SetActive(visible);
            if (!visible || app.IsLoading)
                input.SetSelectedGameObject(null);
            if (!visible)
                return;

            bool mainMenu = !app.IsLoading && app.State == AppState.MainMenu;
            bool stageSelection = !app.IsLoading && app.State == AppState.Selection && app.Step == SelectionStep.Stage;
            menuArt.gameObject.SetActive(mainMenu);
            menuRail.SetActive(mainMenu && menuArt.texture != null);
            if (mainMenu)
                LayoutMainMenu();
            else
                LayoutFlowScreen();
            primary.gameObject.SetActive(!app.IsLoading);
            secondary.gameObject.SetActive(!app.IsLoading && app.State != AppState.MainMenu);
            primary.interactable = !stageSelection || app.CanStartMatch;
            for (int index = 0; index < stageButtons.Length; index++)
            {
                stageButtons[index].gameObject.SetActive(stageSelection);
                stageButtons[index].targetGraphic.color = index == app.StageIndex ? Green : Cream;
            }
            if (app.IsLoading)
            {
                title.text = "Loading";
                description.text = "";
                return;
            }

            switch (app.State)
            {
                case AppState.MainMenu:
                    title.text = "WOBBLE\nBRAWL";
                    description.text = "";
                    primaryText.text = "Quick Fight";
                    break;
                case AppState.Selection:
                    title.text = stageSelection ? "Choose Arena" : "Choose Fighter";
                    description.text = stageSelection
                        ? (app.StageCount > 0 ? app.GetStage(app.StageIndex).Hazards : "")
                        : "Quick Fight vs AI";
                    if (stageSelection)
                    {
                        Place(description.rectTransform, 640f, 52f, -6f);
                        Place((RectTransform)primary.transform, 310f, 58f, -70f);
                        Place((RectTransform)secondary.transform, 310f, 58f, -144f);
                    }
                    primaryText.text = stageSelection ? "Start Match" : "Blobby";
                    secondaryText.text = "Back";
                    break;
                case AppState.Results:
                    title.text = app.Result == RoundResult.PlayerWins ? "You Win" :
                        app.Result == RoundResult.OpponentWins ? "Opponent Wins" : "Draw";
                    description.text = app.MatchSummary;
                    primaryText.text = "Rematch";
                    secondaryText.text = "Main Menu";
                    break;
                case AppState.Match:
                    title.text = "Paused";
                    description.text = "";
                    primaryText.text = "Resume";
                    secondaryText.text = "Main Menu";
                    break;
            }
            input.SetSelectedGameObject(stageSelection && stageButtons.Length > 0
                ? stageButtons[app.StageIndex].gameObject : primary.gameObject);
        }

        private void LayoutMainMenu()
        {
            var left = new Vector2(.2f, .5f);
            title.alignment = TextAnchor.MiddleLeft;
            title.fontStyle = FontStyle.Bold;
            title.fontSize = 82;
            title.lineSpacing = .85f;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 44;
            title.resizeTextMaxSize = 82;
            Place(title.rectTransform, 350f, 205f, 78f, left);
            Place((RectTransform)primary.transform, 350f, 74f, -89f, left);
        }

        private void LayoutFlowScreen()
        {
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Normal;
            title.fontSize = 60;
            title.lineSpacing = 1f;
            title.resizeTextForBestFit = false;
            Place(title.rectTransform, 640f, 78f, 126f);
            Place(description.rectTransform, 640f, 78f, 47f);
            Place((RectTransform)primary.transform, 310f, 58f, -38f);
            Place((RectTransform)secondary.transform, 310f, 58f, -114f);
        }

        private void PrimaryAction()
        {
            if (app.IsPaused)
                app.TogglePause();
            else if (app.State == AppState.MainMenu)
                app.OpenSelection();
            else if (app.State == AppState.Selection && app.Step == SelectionStep.Fighter)
                app.SelectBlobby();
            else
                app.StartMatch();
        }

        private void SecondaryAction() => app.ReturnToMenu();

        private Button CreateButton(string name, float y, Color background, out Text caption)
        {
            var target = Child(safeContent, name, typeof(CanvasRenderer), typeof(Image), typeof(Button));
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

        private static void Place(RectTransform rect, float width, float height, float y, Vector2? anchor = null)
        {
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static GameObject Child(Transform parent, string name, params System.Type[] components)
        {
            var node = new GameObject(name, components);
            node.transform.SetParent(parent, false);
            return node;
        }
    }
}
