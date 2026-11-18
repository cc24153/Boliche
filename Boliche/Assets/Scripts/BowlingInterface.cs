using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class BowlingInterface
{
    private static readonly Color Ink = new Color(.035f, .062f, .09f, .97f);
    private static readonly Color Slate = new Color(.09f, .14f, .18f);
    private static readonly Color Ivory = new Color(.97f, .93f, .85f);
    private static readonly Color Mist = new Color(.66f, .76f, .79f);
    private static readonly Color Gold = new Color(1f, .68f, .33f);
    private static readonly Color Mint = new Color(.36f, .86f, .75f);

    private const float ChargeWidth = 852f;
    private const float ChargeHeight = 10f;
    private const float ButtonHeight = 70f;

    private readonly RectTransform canvas, menu, hud, pause, results;
    private readonly Text[] rolls = new Text[10];
    private readonly Text[] totals = new Text[10];
    private readonly Image[] frames = new Image[10];
    private readonly Text status, power, spin, finalScore;
    private readonly RectTransform aim, chargeFill;
    private readonly Font font;
    private readonly EventSystem eventSystem;
    private readonly Button playButton, resumeButton, replayButton;

    public BowlingInterface(BowlingGame game)
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject overlay = new GameObject("Interface de boliche", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(game.transform);
        canvas = overlay.GetComponent<RectTransform>();
        overlay.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        GameObject events = new GameObject("Eventos da interface", typeof(EventSystem));
        events.transform.SetParent(game.transform);
        eventSystem = events.GetComponent<EventSystem>();
        InputSystemUIInputModule input = events.AddComponent<InputSystemUIInputModule>();
        if (input.actionsAsset == null)
            input.AssignDefaultActions();

        GameObject menuAction = new GameObject("Ação do menu", typeof(IrAoJogo));
        menuAction.transform.SetParent(game.transform);
        IrAoJogo start = menuAction.GetComponent<IrAoJogo>();
        start.Configure(game);

        menu = Screen("Menu inicial");
        Image shade = Panel(menu, "Sombra", new Color(.015f, .025f, .043f, .22f), Vector2.zero, Vector2.zero);
        Stretch(shade.rectTransform);
        RectTransform card = Panel(menu, "Cartão inicial", Ink, new Vector2(720, 610), Vector2.zero).rectTransform;
        card.anchoredPosition = new Vector2(-255, 0);
        Panel(card, "Friso", Gold, new Vector2(720, 5), new Vector2(0, 302.5f));
        Label(card, "BOLICHE EM PRIMEIRA PESSOA", 28, Mint, 640, 42, 0, 233, true);
        Label(card, "Boliche", 96, Ivory, 640, 111, 0, 150, true);
        Label(card, "Mire na pista e jogue uma partida de 10 quadros.", 29, Mist, 640, 52, 0, 70);
        playButton = Button(card, "Jogar", 550, -26, start.Jogar, true);
        Label(card, "Mouse para mirar  •  A / D para se posicionar\nSegure e solte Espaço ou o clique esquerdo\nQ / E para ajustar o efeito\nEsc ou clique direito para pausar",
            29, Mist, 630, 175, 0, -168);

        hud = Screen("Partida");
        RectTransform board = Panel(hud, "Placar", Ink, new Vector2(1288, 170), Vector2.zero).rectTransform;
        At(board, new Vector2(.5f, 1f), new Vector2(0, -98));
        Label(board, "PLACAR", 27, Mint, 164, 38, -545, 60, true);
        for (int i = 0; i < 10; i++)
        {
            float x = -558 + i * 124;
            frames[i] = Panel(board, $"Quadro {i + 1}", Slate, new Vector2(116, 116), new Vector2(x, -15));
            RectTransform frame = frames[i].rectTransform;
            Label(frame, (i + 1).ToString("00"), 25, Mist, 116, 32, 0, 39, true);
            rolls[i] = Label(frame, "", 31, Ivory, 116, 40, 0, 2, true);
            totals[i] = Label(frame, "", 30, Mint, 116, 38, 0, -37, true);
        }

        aim = Panel(hud, "Mira", Mint, new Vector2(6, 6), Vector2.zero).rectTransform;
        RectTransform meter = Panel(hud, "Arremesso", Ink, new Vector2(920, 144), Vector2.zero).rectTransform;
        At(meter, new Vector2(.5f, 0), new Vector2(0, 87));
        status = Label(meter, "", 36, Ivory, 886, 48, 0, 40, true);
        RectTransform track = Panel(meter, "Medidor", Slate, new Vector2(ChargeWidth, ChargeHeight), new Vector2(0, 1)).rectTransform;
        chargeFill = Panel(track, "Carga", Gold, new Vector2(0, ChargeHeight), Vector2.zero).rectTransform;
        chargeFill.pivot = new Vector2(0, .5f);
        At(chargeFill, new Vector2(0, .5f), Vector2.zero);
        power = Label(meter, "", 28, Mist, 242, 42, -295, -36, true);
        spin = Label(meter, "", 28, Mint, 330, 42, 0, -36, true);
        Label(meter, "Esc / Clique dir.", 28, Mist, 242, 42, 295, -36, true);

        pause = Modal("Partida pausada", "Volte à pista quando estiver pronto.", out RectTransform pauseCard);
        resumeButton = Button(pauseCard, "Continuar", 470, -28, game.Resume, true);
        Button(pauseCard, "Menu inicial", 470, -125, game.ShowMenu, false);

        results = Modal("Fim da partida", "", out RectTransform resultsCard);
        finalScore = Label(resultsCard, "", 54, Mint, 586, 68, 0, 58, true);
        replayButton = Button(resultsCard, "Jogar novamente", 470, -28, start.Jogar, true);
        Button(resultsCard, "Menu inicial", 470, -125, game.ShowMenu, false);
    }

    public void Menu() => Show(menu);
    public void Play() => Show(hud);
    public void Pause() => Show(pause, true);

    public void Results(int score)
    {
        finalScore.text = $"{score} / 300";
        Show(results, true);
    }

    public void Refresh(BowlingScore score, string feedback, float charge, float hook, bool ready)
    {
        for (int i = 0; i < frames.Length; i++)
        {
            string marks = score.Mark(i, 0);
            for (int roll = 1; roll < score.Frames[i].Count; roll++)
                marks += "  " + score.Mark(i, roll);
            SetText(rolls[i], marks);
            SetText(totals[i], score.Cumulative(i)?.ToString() ?? "");
            frames[i].color = score.Frame == i ? new Color(.12f, .28f, .28f) : Slate;
        }

        string title = ready ? $"Quadro {score.Frame + 1}  •  Arremesso {score.Frames[score.Frame].Count + 1}" : feedback;
        SetText(status, title);
        SetText(power, $"Potência  {Mathf.RoundToInt(charge * 100)}%");
        int amount = Mathf.RoundToInt(hook * 100);
        string direction = amount < 0 ? "esquerda" : "direita";
        SetText(spin, amount == 0 ? "Efeito  neutro" : $"Efeito  {Mathf.Abs(amount)}% {direction}");
        chargeFill.sizeDelta = new Vector2(ChargeWidth * charge, ChargeHeight);
        aim.gameObject.SetActive(ready);
    }

    private static void SetText(Text label, string value)
    {
        if (label.text != value)
            label.text = value;
    }

    private void Show(RectTransform screen, bool keepHud = false)
    {
        menu.gameObject.SetActive(screen == menu);
        hud.gameObject.SetActive(screen == hud || keepHud);
        pause.gameObject.SetActive(screen == pause);
        results.gameObject.SetActive(screen == results);
        Button selected = screen == menu ? playButton : screen == pause ? resumeButton :
            screen == results ? replayButton : null;
        eventSystem.SetSelectedGameObject(selected != null ? selected.gameObject : null);
    }

    private RectTransform Modal(string title, string subtitle, out RectTransform card)
    {
        RectTransform screen = Screen(title);
        Stretch(Panel(screen, "Fundo", new Color(.02f, .035f, .05f, .72f), Vector2.zero, Vector2.zero).rectTransform);
        card = Panel(screen, "Cartão", Ink, new Vector2(660, 420), Vector2.zero).rectTransform;
        Panel(card, "Friso", Gold, new Vector2(660, 5), new Vector2(0, 207.5f));
        Label(card, title, 56, Ivory, 586, 77, 0, 132, true);
        if (subtitle.Length > 0)
            Label(card, subtitle, 30, Mist, 586, 60, 0, 57);
        return screen;
    }

    private RectTransform Screen(string name)
    {
        RectTransform rect = Rect(canvas, name, Vector2.zero, Vector2.zero);
        Stretch(rect);
        return rect;
    }

    private Button Button(RectTransform parent, string title, float width, float y,
        UnityEngine.Events.UnityAction action, bool primary)
    {
        Image image = Panel(parent, title, primary ? Gold : Slate, new Vector2(width, ButtonHeight), new Vector2(0, y));
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1, 1, 1, .85f);
        colors.pressedColor = new Color(.80f, .85f, .86f);
        button.colors = colors;
        button.onClick.AddListener(action);
        Label(image.rectTransform, title, 32, primary ? Ink : Ivory, width, ButtonHeight, 0, 0, true);
        return button;
    }

    private Text Label(RectTransform parent, string value, int size, Color color,
        float width, float height, float x, float y, bool bold = false)
    {
        RectTransform rect = Rect(parent, value.Length == 0 ? "Texto dinâmico" : value,
            new Vector2(width, height), new Vector2(x, y));
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private static Image Panel(RectTransform parent, string name, Color color, Vector2 size, Vector2 offset)
    {
        Image image = Rect(parent, name, size, offset).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Rect(RectTransform parent, string name, Vector2 size, Vector2 offset)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        At(rect, new Vector2(.5f, .5f), offset);
        return rect;
    }

    private static void At(RectTransform rect, Vector2 anchor, Vector2 offset)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = offset;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
