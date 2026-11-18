using UnityEngine;
using UnityEngine.UI;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

namespace WobbleBrawl.UI
{
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private MatchController match;
        [SerializeField] private RoundController round;

        private static readonly Color Dark = new Color(.075f, .094f, .16f, .94f);
        private static readonly Color Track = new Color(.20f, .24f, .32f);
        private static readonly Color Ink = new Color(.06f, .09f, .14f);
        private static readonly Color Cream = new Color(1f, .96f, .87f);
        private static readonly Color Mint = new Color(.69f, .95f, .39f);
        private static readonly Color Coral = new Color(1f, .55f, .47f);
        private static readonly Color Cyan = new Color(.36f, .81f, .98f);
        private static readonly Color Gold = new Color(1f, .76f, .33f);

        private sealed class Side
        {
            public RectTransform Health;
            public RectTransform Guard;
            public RectTransform Special;
            public Text HealthValue;
            public Text SpecialLabel;
            public Image FirstWin;
            public Image SecondWin;
            public bool Right;
            public Color Tint;
            public int DisplayedHealth = -1;
            public int DisplayedMaxHealth = -1;
        }

        private Side player;
        private Side opponent;
        private RectTransform safeArea;
        private Rect previousSafeArea;
        private Vector2Int previousScreen;
        private Text timer;
        private Text phase;
        private Font font;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasNode = new GameObject("Match HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasNode.transform.SetParent(transform, false);
            var canvas = canvasNode.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1;
            var scaler = canvasNode.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            safeArea = Rect(canvasNode.transform, "Safe Area");
            Fit(safeArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            player = BuildSide("Player", Mint, false);
            opponent = BuildSide("Opponent", Coral, true);

            var clock = Block(safeArea, "Round Clock", Dark);
            Fit(clock.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(-68f, -95f), new Vector2(68f, -22f));
            timer = Label(clock.transform, "Timer", 32, TextAnchor.MiddleCenter, Cream);
            Fit(timer.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, 22f), Vector2.zero);
            phase = Label(clock.transform, "Round Phase", 15, TextAnchor.MiddleCenter, Gold);
            Fit(phase.rectTransform, Vector2.zero, Vector2.right, Vector2.zero, new Vector2(0f, 26f));
        }

        private void OnEnable()
        {
            round.Player.VitalsChanged += RenderVitals;
            round.Opponent.VitalsChanged += RenderVitals;
            round.TimeChanged += RenderTime;
            round.SuddenDeathStarted += RenderSuddenDeath;
            match.RoundStarted += RenderRound;
            match.RoundEnded += RenderWins;
            RenderVitals(round.Player);
            RenderVitals(round.Opponent);
            RenderTime(round.SecondsRemaining);
            RenderRound(match.RoundNumber);
        }

        private void OnDisable()
        {
            if (round != null)
            {
                if (round.Player != null) round.Player.VitalsChanged -= RenderVitals;
                if (round.Opponent != null) round.Opponent.VitalsChanged -= RenderVitals;
                round.TimeChanged -= RenderTime;
                round.SuddenDeathStarted -= RenderSuddenDeath;
            }
            if (match != null)
            {
                match.RoundStarted -= RenderRound;
                match.RoundEnded -= RenderWins;
            }
        }

        private void LateUpdate()
        {
            var display = new Vector2Int(Screen.width, Screen.height);
            Rect inset = Screen.safeArea;
            if (display.x <= 0 || display.y <= 0 || inset.width <= 0f || inset.height <= 0f ||
                (display == previousScreen && inset == previousSafeArea))
                return;
            previousScreen = display;
            previousSafeArea = inset;
            Fit(safeArea, new Vector2(inset.xMin / display.x, inset.yMin / display.y),
                new Vector2(inset.xMax / display.x, inset.yMax / display.y), Vector2.zero, Vector2.zero);
        }

        private void RenderVitals(Fighter fighter)
        {
            Side side = fighter == round.Player ? player : opponent;
            Fill(side.Health, fighter.Health, fighter.MaxHealth, side.Right);
            Fill(side.Guard, fighter.Guard, fighter.MaxGuard, side.Right);
            Fill(side.Special, fighter.Special, fighter.MaxSpecial, side.Right);
            if (fighter.Health != side.DisplayedHealth || fighter.MaxHealth != side.DisplayedMaxHealth)
            {
                side.DisplayedHealth = fighter.Health;
                side.DisplayedMaxHealth = fighter.MaxHealth;
                side.HealthValue.text = $"{fighter.Health} / {fighter.MaxHealth}";
            }
            side.SpecialLabel.text = fighter.Special >= fighter.MaxSpecial ? "READY" : "SPECIAL";
        }

        private void RenderTime(int seconds) => timer.text = seconds.ToString("00");
        private void RenderSuddenDeath() => phase.text = "SUDDEN DEATH";

        private void RenderRound(int number)
        {
            phase.text = number > 0 ? $"ROUND {number}" : "";
            RenderWins(default);
        }

        private void RenderWins(RoundResult _)
        {
            player.FirstWin.color = match.PlayerWins > 0 ? player.Tint : Track;
            player.SecondWin.color = match.PlayerWins > 1 ? player.Tint : Track;
            opponent.FirstWin.color = match.OpponentWins > 0 ? opponent.Tint : Track;
            opponent.SecondWin.color = match.OpponentWins > 1 ? opponent.Tint : Track;
        }

        private Side BuildSide(string name, Color tint, bool right)
        {
            var card = Block(safeArea, name, Dark).rectTransform;
            float left = right ? .57f : 0f;
            float edge = right ? 1f : .43f;
            Fit(card, new Vector2(left, 1f), new Vector2(edge, 1f),
                new Vector2(right ? 12f : 28f, -153f), new Vector2(right ? -28f : -12f, -22f));
            var heading = Label(card, "Fighter Name", 20, right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, Cream);
            heading.text = name.ToUpperInvariant();
            Fit(heading.rectTransform, Vector2.up, Vector2.one, new Vector2(14f, -33f), new Vector2(-14f, -4f));

            var health = Bar(card, "Health", tint, -38f, 26f, out var healthTrack);
            var healthValue = Label(healthTrack, "Health Value", 15, TextAnchor.MiddleCenter, Cream);
            var outline = healthValue.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            Fit(healthValue.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var guard = Bar(card, "Guard", Cyan, -75f, 9f, out _);
            var special = Bar(card, "Special", Gold, -106f, 9f, out _);
            GaugeCaption(card, "Guard Caption", "GUARD", right, -67f, Cyan);
            var specialLabel = GaugeCaption(card, "Special Caption", "SPECIAL", right, -98f, Gold);
            return new Side
            {
                Health = health, Guard = guard, Special = special, HealthValue = healthValue,
                SpecialLabel = specialLabel, Right = right, Tint = tint,
                FirstWin = Pip(card, "Win One", right, 14f), SecondWin = Pip(card, "Win Two", right, 37f)
            };
        }

        private Text GaugeCaption(Transform card, string name, string value, bool right, float y, Color color)
        {
            var label = Label(card, name, 11, right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, color);
            label.text = value;
            Fit(label.rectTransform, Vector2.up, Vector2.one, new Vector2(14f, y - 7f), new Vector2(-14f, y + 6f));
            return label;
        }

        private RectTransform Bar(Transform card, string name, Color tint, float top, float height, out RectTransform track)
        {
            track = Block(card, name, Track).rectTransform;
            Fit(track, Vector2.up, Vector2.one, new Vector2(14f, top - height), new Vector2(-14f, top));
            var fill = Block(track, "Fill", tint).rectTransform;
            Fit(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return fill;
        }

        private Image Pip(Transform card, string name, bool right, float offset)
        {
            var pip = Block(card, name, Track);
            Vector2 anchor = right ? Vector2.up : Vector2.one;
            float x = right ? offset : -offset - 16f;
            Fit(pip.rectTransform, anchor, anchor, new Vector2(x, -27f), new Vector2(x + 16f, -13f));
            return pip;
        }

        private static void Fill(RectTransform fill, int value, int maximum, bool right)
        {
            float ratio = maximum > 0 ? Mathf.Clamp01((float)value / maximum) : 0f;
            fill.anchorMin = new Vector2(right ? 1f - ratio : 0f, 0f);
            fill.anchorMax = new Vector2(right ? 1f : ratio, 1f);
        }

        private Text Label(Transform parent, string name, int size, TextAnchor alignment, Color color)
        {
            var node = Rect(parent, name);
            node.gameObject.AddComponent<CanvasRenderer>();
            var label = node.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static Image Block(Transform parent, string name, Color color)
        {
            var node = Rect(parent, name);
            node.gameObject.AddComponent<CanvasRenderer>();
            var image = node.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            return (RectTransform)node.transform;
        }

        private static void Fit(RectTransform rect, Vector2 min, Vector2 max, Vector2 low, Vector2 high)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = low;
            rect.offsetMax = high;
        }
    }
}
