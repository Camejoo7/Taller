using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// El libro de Kira: la explicación de cada tema, adentro del juego. Se abre
/// con TAB en cualquier momento, también desde adentro de una terminal (y ahí
/// se abre directo en la página de ese tema). Mientras está abierto el juego
/// se congela: consultar no es trampa, la evaluación es formativa.
///
/// Igual que <see cref="CodeTerminalUI"/>, la pantalla se arma sola en
/// runtime: alcanza con poner este componente en la escena (o el prefab
/// LibroDeKira) con el contenido, la fuente y los sprites del pack.
///
/// Las páginas no vienen todas escritas: Kira las dicta al llegar a cada zona
/// (<see cref="BookPageUnlock"/>), y lo escrito se guarda en
/// <see cref="KiraBookProgress"/>.
/// </summary>
public class KiraBook : MonoBehaviour
{
    public static KiraBook Instance { get; private set; }

    [Header("Contenido")]
    public KiraBookContent content;

    [Header("Fuente")]
    [Tooltip("PressStart2P: tiene acentos, ñ y signos de apertura.")]
    public TMP_FontAsset font;

    [Header("Sprites del pack (ASSETS/Libro/01_TravelBookLite/Sprites)")]
    [Tooltip("UI_TravelBook_BookCover01a")]
    public Sprite coverSprite;
    [Tooltip("UI_TravelBook_BookPageLeft01a")]
    public Sprite pageLeftSprite;
    [Tooltip("UI_TravelBook_BookPageRight01a")]
    public Sprite pageRightSprite;
    [Tooltip("UI_TravelBook_IconArrow01a")]
    public Sprite arrowSprite;
    [Tooltip("UI_TravelBook_Marker01a")]
    public Sprite tabSprite;
    [Tooltip("UI_TravelBook_Popup01a")]
    public Sprite toastSprite;
    [Tooltip("UI_TravelBook_Frame01a")]
    public Sprite hudSprite;

    [Header("Tamaño")]
    [Tooltip("Cuántos pixeles de pantalla (referencia 1920x1080) mide cada " +
             "pixel del dibujo. Tiene que ser entero: el pixel art se ve " +
             "nítido con escala entera y borroneado con decimales.")]
    public int pixelScale = 6;

    [Tooltip("La misma idea para el cartel de TAB y el aviso de página nueva.")]
    public int hudPixelScale = 5;

    [Tooltip("18 es lo más grande que entra en la página más cargada (la de " +
             "variables). Más chico se pierde en monitores de 1366x768.")]
    public float bodySize = 18f;
    public float titleSize = 26f;

    [Header("Cartel de 'TAB LIBRO' en pantalla")]
    [Tooltip("Aparece cuando Kira escribe una página nueva y se va cuando el " +
             "jugador abre el libro. El libro se puede abrir igual sin él.")]
    public bool showHudHint = true;

    [Header("Colores (del mismo pack)")]
    public Color inkColor = new Color32(73, 51, 51, 255);
    public Color accentColor = new Color32(156, 67, 81, 255);
    public Color ruleColor = new Color32(222, 169, 116, 255);
    public Color codeBoxColor = new Color32(222, 169, 116, 120);
    public Color screenBoxColor = new Color32(73, 51, 51, 255);
    public Color outputColor = new Color32(255, 215, 168, 255);
    public Color errorColor = new Color32(255, 143, 122, 255);
    public Color kiraColor = new Color32(31, 110, 118, 255);

    // ------------------------------------------------------------ estado

    static int closedFrame = -1;
    static string context;

    /// <summary>Cuántas veces se abrió el libro en la partida. La terminal lo
    /// usa para registrar si el alumno lo consultó mientras resolvía.</summary>
    public static int TimesOpened { get; private set; }

    /// <summary>
    /// Si el libro se está quedando con el teclado. Las terminales, la pausa y
    /// los diálogos lo consultan para no reaccionar a las teclas que son del
    /// libro, incluido el cuadro en que se cerró: si no, el ESC que lo cierra
    /// también cierra la terminal que estaba abajo.
    /// </summary>
    public static bool BlocksInput
    {
        get { return (Instance != null && Instance.isOpen) || Time.frameCount == closedFrame; }
    }

    /// <summary>Si el jugador tiene el libro y se puede abrir en esta escena.</summary>
    public static bool Available
    {
        get { return Instance != null && Instance.content != null && KiraBookProgress.HasBook; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay()
    {
        closedFrame = -1;
        context = null;
        TimesOpened = 0;
    }

    private Canvas canvas;
    private GameObject root;
    private CanvasGroup rootGroup;
    private RectTransform book;
    private RectTransform tabsRoot;
    private PageView leftPage;
    private PageView rightPage;
    private PageView flipLeft;
    private PageView flipRight;
    private Button prevButton;
    private Button nextButton;
    private TextMeshProUGUI footerText;

    private RectTransform hud;
    private TextMeshProUGUI hudAlert;
    private RectTransform toast;
    private TextMeshProUGUI toastHeaderText;
    private TextMeshProUGUI toastBodyText;
    private TextMeshProUGUI toastHintText;

    private readonly List<BookChapter> spreads = new List<BookChapter>();
    private readonly List<RectTransform> tabs = new List<RectTransform>();
    private int current;
    private string lastChapterId;
    private bool isOpen;
    private bool flipping;
    private float previousTimeScale = 1f;

    private PlayerMovement movement;
    private PlayerShooting shooting;
    private CodeTerminalUI terminalUI;
    private bool movementWasEnabled;
    private bool shootingWasEnabled;

    private Coroutine anim;
    private Coroutine flipRoutine;
    private Coroutine toastRoutine;
    private readonly Queue<BookChapter> pendingToasts = new Queue<BookChapter>();

    public bool IsOpen { get { return isOpen; } }

    float PageW { get { return pageLeftSprite != null ? pageLeftSprite.rect.width * pixelScale : 624f; } }
    float PageH { get { return pageLeftSprite != null ? pageLeftSprite.rect.height * pixelScale : 882f; } }

    /// <summary>Una página dibujada: el papel y la columna donde se apilan los bloques.</summary>
    class PageView
    {
        public RectTransform rect;
        public Image paper;
        public RectTransform column;
    }

    // ------------------------------------------------------------ ciclo de vida

    void Awake()
    {
        Instance = this;
        Build();
        root.SetActive(false);
        toast.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        KiraBookProgress.Unlocked += OnUnlocked;
    }

    void OnDisable()
    {
        KiraBookProgress.Unlocked -= OnUnlocked;

        // Nunca dejar el juego congelado: si la escena se descarga con el libro
        // abierto, el Time.timeScale en 0 pasaría a la escena siguiente.
        if (isOpen)
        {
            isOpen = false;
            Time.timeScale = previousTimeScale;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null) shooting = movement.GetComponent<PlayerShooting>();
        terminalUI = FindFirstObjectByType<CodeTerminalUI>();
        RefreshHud();
    }

    void Update()
    {
        RefreshHud();

        Keyboard kb = Keyboard.current;
        if (kb == null || content == null) return;

        if (isOpen)
        {
            if (kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
                Turn(1);
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
                Turn(-1);
            return;
        }

        if (kb.tabKey.wasPressedThisFrame && CanOpen())
            Open();
    }

    bool CanOpen()
    {
        if (!Available) return false;
        if (PauseMenu.IsPaused) return false;
        // El TAB que lo cerró no lo vuelve a abrir en el mismo cuadro.
        return Time.frameCount != closedFrame;
    }

    // ------------------------------------------------------------ API para las terminales

    /// <summary>La terminal avisa qué tema está abierto: el libro se abre ahí.</summary>
    public static void SetContext(string concept)
    {
        context = concept;
    }

    public static void ClearContext()
    {
        context = null;
    }

    /// <summary>
    /// El aviso de Kira para quien falló dos veces: qué página abrir. Null si
    /// el libro no está o esa página todavía no se escribió (mandarlo a una
    /// página en blanco sería peor que no decir nada).
    /// </summary>
    public static string HintFor(string concept)
    {
        if (!Available) return null;

        BookChapter chapter = Instance.content.Find(concept);
        if (!KiraBookProgress.IsUnlocked(chapter)) return null;

        return string.Format(Instance.content.terminalHint, chapter.title);
    }

    // ------------------------------------------------------------ abrir y cerrar

    public void Open()
    {
        if (isOpen) return;

        CollectSpreads();
        if (spreads.Count == 0) return;

        isOpen = true;
        TimesOpened++;
        KiraBookProgress.ClearNews();

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Con el tiempo en 0 la física no corre, pero el Update del jugador
        // sí: las flechas que pasan de página lo darían vuelta y la barra
        // espaciadora lo haría saltar apenas se cierra el libro.
        if (movement != null)
        {
            movementWasEnabled = movement.enabled;
            movement.enabled = false;
        }
        if (shooting != null)
        {
            shootingWasEnabled = shooting.enabled;
            shooting.enabled = false;
        }

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = null;
        toast.gameObject.SetActive(false);
        hud.gameObject.SetActive(false);

        // Activo antes de escribir: un TMP apagado todavía no sabe medirse, y
        // el armado de la página necesita el alto de cada renglón.
        rootGroup.alpha = 0f;
        root.SetActive(true);
        flipping = false;

        current = PickStartSpread();
        ShowSpread(current);
        RebuildTabs();

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(OpenAnim());
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        closedFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;

        if (movement != null) movement.enabled = movementWasEnabled;
        if (shooting != null) shooting.enabled = shootingWasEnabled;

        // Una hoja a medio girar se queda donde está: al volver a abrir, las
        // páginas se escriben de nuevo enteras.
        if (flipRoutine != null) StopCoroutine(flipRoutine);
        flipRoutine = null;
        flipping = false;

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(CloseAnim());
    }

    void CollectSpreads()
    {
        spreads.Clear();
        if (content == null) return;
        foreach (BookChapter c in content.chapters)
            if (KiraBookProgress.IsUnlocked(c)) spreads.Add(c);
    }

    /// <summary>
    /// Dónde se abre: en el tema de la terminal si se abrió desde una; si no,
    /// en la primera página nueva sin leer; si no, donde se dejó la última vez.
    /// </summary>
    int PickStartSpread()
    {
        if (!string.IsNullOrEmpty(context))
        {
            int i = IndexOf(context);
            if (i >= 0) return i;
        }

        for (int i = 0; i < spreads.Count; i++)
            if (!KiraBookProgress.IsRead(spreads[i])) return i;

        int last = IndexOf(lastChapterId);
        return last >= 0 ? last : 0;
    }

    int IndexOf(string id)
    {
        if (string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < spreads.Count; i++)
            if (string.Equals(spreads[i].id, id, System.StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    IEnumerator OpenAnim()
    {
        // Sube desde abajo de la pantalla y se pasa un poquito, como un libro
        // que se apoya de golpe sobre la mesa.
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.32f;
            float k = Mathf.Clamp01(t);
            rootGroup.alpha = Mathf.Clamp01(k * 2.5f);
            book.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-1100f, 0f, EaseOutBack(k)));
            yield return null;
        }
        rootGroup.alpha = 1f;
        book.anchoredPosition = Vector2.zero;
        anim = null;
    }

    IEnumerator CloseAnim()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.2f;
            float k = Mathf.Clamp01(t);
            rootGroup.alpha = 1f - k;
            book.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, -1100f, k * k));
            yield return null;
        }
        root.SetActive(false);
        anim = null;
        RefreshHud();
        ShowNextToast();
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.4f;
        const float c3 = c1 + 1f;
        float p = x - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    // ------------------------------------------------------------ pasar de página

    void Turn(int dir)
    {
        GoTo(current + dir);
    }

    void GoTo(int index)
    {
        if (flipping || !isOpen) return;
        if (index < 0 || index >= spreads.Count || index == current) return;
        flipRoutine = StartCoroutine(Flip(current, index));
    }

    void ShowSpread(int index)
    {
        BookChapter c = spreads[index];
        Fill(leftPage, c.left, c);
        Fill(rightPage, c.right, c);
        flipLeft.rect.gameObject.SetActive(false);
        flipRight.rect.gameObject.SetActive(false);
        Arrived(index);
    }

    void Arrived(int index)
    {
        current = index;
        lastChapterId = spreads[index].id;
        KiraBookProgress.MarkRead(spreads[index]);
        prevButton.gameObject.SetActive(index > 0);
        nextButton.gameObject.SetActive(index < spreads.Count - 1);
        LayoutTabs(index);
    }

    /// <summary>
    /// La hoja da vuelta sobre el lomo: la que se va se achica hacia el centro
    /// y la que llega se abre desde ahí. Es una escala en X, pero con el papel
    /// oscureciéndose al quedar de canto alcanza para leerse como una hoja.
    /// </summary>
    IEnumerator Flip(int from, int to)
    {
        flipping = true;
        BookChapter a = spreads[from];
        BookChapter b = spreads[to];
        bool forward = to > from;

        PageView going = forward ? flipRight : flipLeft;
        PageView coming = forward ? flipLeft : flipRight;

        // La pestaña salta apenas se pide la página, no cuando termina de girar.
        LayoutTabs(to);

        going.rect.localScale = Vector3.one;
        going.rect.gameObject.SetActive(true);
        Fill(going, forward ? a.right : a.left, a);
        if (forward) Fill(rightPage, b.right, b);
        else Fill(leftPage, b.left, b);

        yield return StartCoroutine(Squash(going, 1f, 0f, 0.13f));
        going.rect.gameObject.SetActive(false);

        coming.rect.localScale = new Vector3(0.001f, 1f, 1f);
        coming.rect.gameObject.SetActive(true);
        Fill(coming, forward ? b.left : b.right, b);
        yield return StartCoroutine(Squash(coming, 0f, 1f, 0.13f));

        if (forward) Fill(leftPage, b.left, b);
        else Fill(rightPage, b.right, b);
        coming.rect.gameObject.SetActive(false);

        Arrived(to);
        flipping = false;
        flipRoutine = null;
    }

    IEnumerator Squash(PageView page, float from, float to, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float k = Mathf.Clamp01(t);
            // Acelera al irse y frena al llegar.
            float e = from > to ? k * k : 1f - (1f - k) * (1f - k);
            float s = Mathf.Lerp(from, to, e);
            page.rect.localScale = new Vector3(Mathf.Max(0.001f, s), 1f, 1f);
            float shade = Mathf.Lerp(0.72f, 1f, s);
            page.paper.color = new Color(shade, shade, shade, 1f);
            yield return null;
        }
        page.rect.localScale = Vector3.one;
        page.paper.color = Color.white;
    }

    // ------------------------------------------------------------ escribir una página

    void Fill(PageView page, List<BookBlock> blocks, BookChapter chapter)
    {
        // Destroy recién borra al final del cuadro: si no se apagan antes, el
        // layout de este cuadro los cuenta y la página nueva sale corrida.
        for (int i = page.column.childCount - 1; i >= 0; i--)
        {
            GameObject old = page.column.GetChild(i).gameObject;
            old.SetActive(false);
            Destroy(old);
        }

        if (blocks != null)
        {
            bool first = true;
            foreach (BookBlock b in blocks)
            {
                if (b == null) continue;
                AddBlock(page.column, b, first);
                first = false;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(page.column);

        // Si alguien escribe de más, que se entere en la consola en vez de que
        // el texto se salga del papel sin que nadie lo note.
        float need = LayoutUtility.GetPreferredHeight(page.column);
        if (need > page.column.rect.height + 1f)
            Debug.LogWarning("Libro de Kira: la página '" + (chapter != null ? chapter.id : "?") +
                             "' no entra (" + Mathf.RoundToInt(need) + " de " +
                             Mathf.RoundToInt(page.column.rect.height) + "). Acortá el texto.", this);
    }

    void AddBlock(RectTransform column, BookBlock b, bool first)
    {
        switch (b.kind)
        {
            case BookBlockKind.Titulo:
                AddText(column, b.text, titleSize, accentColor, true);
                GameObject rule = NewImage("Raya", column, ruleColor);
                LayoutElement le = rule.AddComponent<LayoutElement>();
                le.preferredHeight = pixelScale;
                le.minHeight = pixelScale;
                break;

            case BookBlockKind.Etiqueta:
                TextMeshProUGUI label = AddText(column, b.text, bodySize, accentColor, true);
                // Más aire arriba que abajo: el subtítulo es de lo que sigue,
                // no de lo que vino antes.
                if (!first) label.margin = new Vector4(0f, pixelScale * 1.5f, 0f, 0f);
                break;

            case BookBlockKind.Texto:
                AddText(column, b.text, bodySize, inkColor, true);
                break;

            case BookBlockKind.Kira:
                AddText(column, "<color=#" + ColorUtility.ToHtmlStringRGB(kiraColor) + ">KIRA:</color> " + b.text,
                        bodySize, inkColor, true);
                break;

            case BookBlockKind.Codigo:
                AddBox(column, b.text, codeBoxColor, inkColor);
                break;

            case BookBlockKind.Salida:
                AddBox(column, b.text, screenBoxColor, outputColor);
                break;

            case BookBlockKind.Error:
                AddBox(column, b.text, screenBoxColor, errorColor);
                break;

            case BookBlockKind.Espacio:
                GameObject gap = new GameObject("Espacio", typeof(RectTransform));
                gap.transform.SetParent(column, false);
                LayoutElement g = gap.AddComponent<LayoutElement>();
                g.preferredHeight = bodySize;
                break;
        }
    }

    TextMeshProUGUI AddText(Transform parent, string text, float size, Color color, bool rich)
    {
        TextMeshProUGUI t = NewText("Texto", parent, size, color, TextAlignmentOptions.TopLeft);
        t.richText = rich;
        t.lineSpacing = 28f;
        t.text = text ?? "";
        return t;
    }

    /// <summary>
    /// Un recuadro: claro para el código que se escribe, oscuro (como la
    /// pantalla de la terminal) para lo que Python imprime. Que se distingan
    /// de un vistazo es la mitad de entender un ejemplo.
    /// </summary>
    void AddBox(Transform parent, string text, Color box, Color color)
    {
        GameObject go = NewImage("Recuadro", parent, box);
        VerticalLayoutGroup v = go.AddComponent<VerticalLayoutGroup>();
        int pad = pixelScale * 2;
        v.padding = new RectOffset(pad, pad, pad, pad);
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        // Sin formato: el código lleva < y > que TMP se comería como etiquetas.
        AddText(go.transform, text, bodySize, color, false);
    }

    // ------------------------------------------------------------ pestañas

    /// <summary>
    /// Una pestaña por tema escrito, asomando del costado de la tapa. Es lo que
    /// hace que se vea que el libro se va llenando: al principio hay una sola.
    /// </summary>
    void RebuildTabs()
    {
        foreach (RectTransform t in tabs) Destroy(t.gameObject);
        tabs.Clear();

        for (int i = 0; i < spreads.Count; i++)
        {
            int index = i;
            GameObject go = NewImage("Pestana_" + spreads[i].id, tabsRoot, Color.white);
            Image img = go.GetComponent<Image>();
            img.sprite = tabSprite;
            img.raycastTarget = true;
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(1f, 0.5f);
            r.sizeDelta = tabSprite != null
                ? new Vector2(tabSprite.rect.width, tabSprite.rect.height) * pixelScale
                : new Vector2(120f, 84f);

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(delegate { GoTo(index); });

            TextMeshProUGUI label = NewText("Rotulo", r, titleSize, inkColor, TextAlignmentOptions.Center);
            RectTransform lr = label.rectTransform;
            lr.anchorMin = new Vector2(1f, 0f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(1f, 0.5f);
            label.text = spreads[i].tabLabel;

            tabs.Add(r);
        }
        LayoutTabs(current);
    }

    void LayoutTabs(int selected)
    {
        if (tabs.Count == 0) return;

        float coverRight = coverSprite != null ? coverSprite.rect.width * pixelScale * 0.5f : 672f;
        float step = (tabSprite != null ? tabSprite.rect.height : 14f) * pixelScale + pixelScale * 2;
        float top = PageH * 0.5f - step * 0.5f - pixelScale * 4;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool on = i == selected;
            float visible = pixelScale * (on ? 13 : 9);
            RectTransform r = tabs[i];
            r.anchoredPosition = new Vector2(coverRight + visible, top - step * i);
            r.GetComponent<Image>().color = on ? Color.white : new Color(0.86f, 0.8f, 0.76f, 1f);

            // El rótulo, centrado en lo que asoma de la tapa.
            RectTransform lr = (RectTransform)r.Find("Rotulo");
            lr.sizeDelta = new Vector2(visible - pixelScale, 0f);
            lr.anchoredPosition = new Vector2(-pixelScale * 0.5f, 0f);
        }
    }

    // ------------------------------------------------------------ avisos

    void OnUnlocked(string id)
    {
        RefreshHud();
        if (content == null) return;

        BookChapter chapter = content.Find(id);
        if (chapter == null)
        {
            // Sin tema (o con uno que no existe) es la entrega del libro: el
            // aviso es el de la portada.
            foreach (BookChapter c in content.chapters)
                if (c != null && c.alwaysUnlocked) { chapter = c; break; }
        }
        if (chapter == null) return;

        pendingToasts.Enqueue(chapter);
        if (toastRoutine == null && !isOpen) ShowNextToast();
    }

    void ShowNextToast()
    {
        if (isOpen || toastRoutine != null || pendingToasts.Count == 0) return;
        toastRoutine = StartCoroutine(ToastRoutine(pendingToasts.Dequeue()));
    }

    IEnumerator ToastRoutine(BookChapter chapter)
    {
        toastHeaderText.text = string.IsNullOrEmpty(chapter.toastHeader) ? content.toastHeader : chapter.toastHeader;
        toastBodyText.text = string.IsNullOrEmpty(chapter.toastText) ? chapter.title : chapter.toastText;
        toastHintText.text = content.toastHint;
        toast.gameObject.SetActive(true);

        float hidden = toast.sizeDelta.x + 40f;
        float shown = -24f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.35f;
            toast.anchoredPosition = new Vector2(Mathf.LerpUnclamped(hidden, shown, EaseOutBack(Mathf.Clamp01(t))), toast.anchoredPosition.y);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(4.5f);

        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.25f;
            toast.anchoredPosition = new Vector2(Mathf.Lerp(shown, hidden, t * t), toast.anchoredPosition.y);
            yield return null;
        }

        toast.gameObject.SetActive(false);
        toastRoutine = null;
        ShowNextToast();
    }

    void RefreshHud()
    {
        if (hud == null) return;
        // Con la terminal abierta el cartel sobra (su pie ya dice "TAB libro")
        // y además le tapa la esquina al monitor.
        // Fijo en pantalla molestaba (Kevin, 26/10): solo aparece cuando Kira
        // escribió algo que el jugador todavía no vio.
        bool terminalOpen = terminalUI != null && terminalUI.IsOpen;
        bool show = showHudHint && KiraBookProgress.HasNews && !isOpen && root != null && !root.activeSelf &&
                    !terminalOpen && Available;
        if (hud.gameObject.activeSelf != show) hud.gameObject.SetActive(show);
    }

    IEnumerator BlinkAlert()
    {
        // El signo de "hay algo sin leer" late, sin brillos: solo prende y apaga.
        while (true)
        {
            if (hudAlert != null) hudAlert.enabled = !hudAlert.enabled;
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    // ------------------------------------------------------------ armado

    void Build()
    {
        GameObject canvasGO = new GameObject("LibroCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Por encima de la terminal (90): el libro se abre desde adentro de ella.
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        BuildHud(canvasGO.transform);

        root = new GameObject("Libro");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.SetParent(canvasGO.transform, false);
        Stretch(rootRect);
        rootGroup = root.AddComponent<CanvasGroup>();

        // Hacer click afuera del libro también lo cierra.
        GameObject dim = NewImage("Fondo", rootRect, new Color(0f, 0f, 0f, 0.78f));
        Stretch(dim.GetComponent<RectTransform>());
        dim.GetComponent<Image>().raycastTarget = true;
        dim.AddComponent<Button>().onClick.AddListener(Close);

        GameObject bookGO = new GameObject("Tapa y hojas", typeof(RectTransform));
        book = bookGO.GetComponent<RectTransform>();
        book.SetParent(rootRect, false);
        Center(book, Vector2.zero);

        // Las pestañas van antes que la tapa, así asoman por detrás de ella.
        GameObject tabsGO = new GameObject("Pestanas", typeof(RectTransform));
        tabsRoot = tabsGO.GetComponent<RectTransform>();
        tabsRoot.SetParent(book, false);
        Center(tabsRoot, Vector2.zero);

        GameObject cover = NewImage("Tapa", book, Color.white);
        Image coverImg = cover.GetComponent<Image>();
        coverImg.sprite = coverSprite;
        coverImg.raycastTarget = true; // que el click en la tapa no cierre el libro
        Center(cover.GetComponent<RectTransform>(), coverSprite != null
            ? new Vector2(coverSprite.rect.width, coverSprite.rect.height) * pixelScale
            : new Vector2(1344f, 960f));
        if (coverSprite == null) coverImg.color = new Color32(183, 91, 95, 255);

        // Las hojas se apoyan contra el lomo (x = 0): la izquierda pivotea en
        // su borde derecho y la derecha en el izquierdo. Así la vuelta de
        // página es solo escalar en X.
        leftPage = BuildPage("Hoja izquierda", pageLeftSprite, true);
        rightPage = BuildPage("Hoja derecha", pageRightSprite, false);

        GameObject spine = NewImage("Lomo", book, new Color32(199, 137, 93, 255));
        RectTransform sr = spine.GetComponent<RectTransform>();
        Center(sr, new Vector2(pixelScale, PageH - pixelScale * 8));
        sr.anchoredPosition = new Vector2(0f, pixelScale * 4.5f);

        flipLeft = BuildPage("Hoja que gira (izq)", pageLeftSprite, true);
        flipRight = BuildPage("Hoja que gira (der)", pageRightSprite, false);

        prevButton = BuildArrow("Anterior", -1);
        nextButton = BuildArrow("Siguiente", 1);

        footerText = NewText("Pie", rootRect, bodySize, new Color32(255, 215, 168, 255), TextAlignmentOptions.Center);
        RectTransform fr = footerText.rectTransform;
        fr.anchorMin = new Vector2(0f, 0f);
        fr.anchorMax = new Vector2(1f, 0f);
        fr.pivot = new Vector2(0.5f, 0f);
        fr.sizeDelta = new Vector2(0f, 40f);
        fr.anchoredPosition = new Vector2(0f, 14f);
        footerText.text = content != null ? content.footerHint : "";
    }

    PageView BuildPage(string name, Sprite sprite, bool left)
    {
        PageView p = new PageView();
        GameObject go = NewImage(name, book, Color.white);
        p.paper = go.GetComponent<Image>();
        p.paper.sprite = sprite;
        if (sprite == null) p.paper.color = new Color32(255, 215, 168, 255);

        p.rect = go.GetComponent<RectTransform>();
        p.rect.anchorMin = p.rect.anchorMax = new Vector2(0.5f, 0.5f);
        p.rect.pivot = new Vector2(left ? 1f : 0f, 0.5f);
        p.rect.sizeDelta = new Vector2(PageW, PageH);
        p.rect.anchoredPosition = new Vector2(0f, pixelScale * 0.5f);

        GameObject col = new GameObject("Columna", typeof(RectTransform));
        p.column = col.GetComponent<RectTransform>();
        p.column.SetParent(p.rect, false);

        // El margen del lado del borde del libro es más ancho que el del lomo
        // (ahí está el contorno), y abajo se come el canto de las hojas y el
        // lugar de las flechas.
        float outer = pixelScale * 7f;
        float inner = pixelScale * 5f;
        p.column.anchorMin = Vector2.zero;
        p.column.anchorMax = Vector2.one;
        p.column.offsetMin = new Vector2(left ? outer : inner, pixelScale * 17f);
        p.column.offsetMax = new Vector2(-(left ? inner : outer), -pixelScale * 7f);

        VerticalLayoutGroup v = col.AddComponent<VerticalLayoutGroup>();
        v.spacing = pixelScale * 2.5f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        return p;
    }

    Button BuildArrow(string name, int dir)
    {
        GameObject go = NewImage(name, book, inkColor);
        Image img = go.GetComponent<Image>();
        img.sprite = arrowSprite;
        img.raycastTarget = true;
        RectTransform r = go.GetComponent<RectTransform>();
        Center(r, arrowSprite != null
            ? new Vector2(arrowSprite.rect.width, arrowSprite.rect.height) * (pixelScale - 2)
            : new Vector2(64f, 52f));
        r.anchoredPosition = new Vector2(dir * (PageW - pixelScale * 13f), -PageH * 0.5f + pixelScale * 12f);
        r.localScale = new Vector3(dir, 1f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(delegate { Turn(dir); });
        return btn;
    }

    void BuildHud(Transform parent)
    {
        int s = hudPixelScale;

        GameObject h = NewImage("CartelTab", parent, Color.white);
        hud = h.GetComponent<RectTransform>();
        Image hi = h.GetComponent<Image>();
        hi.sprite = hudSprite;
        Vector2 hudSize = hudSprite != null ? new Vector2(hudSprite.rect.width, hudSprite.rect.height) * s : new Vector2(310f, 70f);
        hud.anchorMin = hud.anchorMax = new Vector2(1f, 1f);
        hud.pivot = new Vector2(1f, 1f);
        hud.sizeDelta = hudSize;
        hud.anchoredPosition = new Vector2(-24f, -24f);
        if (hudSprite == null) hi.color = new Color32(198, 176, 155, 255);

        TextMeshProUGUI ht = NewText("Texto", hud, bodySize, inkColor, TextAlignmentOptions.Center);
        Stretch(ht.rectTransform);
        ht.text = "TAB  LIBRO";

        hudAlert = NewText("SinLeer", hud, titleSize, accentColor, TextAlignmentOptions.Center);
        RectTransform ar = hudAlert.rectTransform;
        ar.anchorMin = new Vector2(1f, 0f);
        ar.anchorMax = new Vector2(1f, 1f);
        ar.pivot = new Vector2(1f, 0.5f);
        ar.sizeDelta = new Vector2(s * 8f, 0f);
        ar.anchoredPosition = Vector2.zero;
        hudAlert.text = "!";
        StartCoroutine(BlinkAlert());

        GameObject t = NewImage("AvisoPagina", parent, Color.white);
        toast = t.GetComponent<RectTransform>();
        Image ti = t.GetComponent<Image>();
        ti.sprite = toastSprite;
        if (toastSprite == null) ti.color = new Color32(255, 215, 168, 255);
        toast.anchorMin = toast.anchorMax = new Vector2(1f, 1f);
        toast.pivot = new Vector2(1f, 1f);
        toast.sizeDelta = toastSprite != null ? new Vector2(toastSprite.rect.width, toastSprite.rect.height) * s : new Vector2(310f, 150f);
        toast.anchoredPosition = new Vector2(-24f, -24f - hudSize.y - 12f);

        float pad = s * 4f;
        toastHeaderText = NewText("Encabezado", toast, bodySize, accentColor, TextAlignmentOptions.TopLeft);
        PlaceRow(toastHeaderText.rectTransform, pad, pad);
        toastBodyText = NewText("Tema", toast, bodySize, inkColor, TextAlignmentOptions.TopLeft);
        PlaceRow(toastBodyText.rectTransform, pad, pad + bodySize * 2.2f);
        toastBodyText.enableAutoSizing = true;
        toastBodyText.fontSizeMin = 10f;
        toastBodyText.fontSizeMax = bodySize;
        toastHintText = NewText("Tecla", toast, bodySize, new Color32(199, 137, 93, 255), TextAlignmentOptions.TopLeft);
        PlaceRow(toastHintText.rectTransform, pad, pad + bodySize * 4.4f);
    }

    void PlaceRow(RectTransform r, float pad, float fromTop)
    {
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.offsetMin = new Vector2(pad, -fromTop - bodySize * 1.6f);
        r.offsetMax = new Vector2(-pad, -fromTop);
    }

    // ------------------------------------------------------------ helpers (los mismos que la terminal)

    GameObject NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name);
        RectTransform r = go.AddComponent<RectTransform>();
        r.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    TextMeshProUGUI NewText(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        RectTransform r = go.AddComponent<RectTransform>();
        r.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }

    void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    void Center(RectTransform r, Vector2 size)
    {
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = Vector2.zero;
    }
}
