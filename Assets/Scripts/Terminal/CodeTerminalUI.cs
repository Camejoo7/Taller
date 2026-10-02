using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// La pantalla de la terminal. Se arma sola en runtime, así que alcanza con
/// poner el componente en un GameObject vacío y asignarle la fuente — no hay
/// que montar un Canvas a mano para probarla en una escena nueva.
///
/// El look importa tanto como la mecánica acá: si esto parece un formulario,
/// es un formulario. Por eso hay líneas de barrido, parpadeo del tubo, cursor
/// que titila y la salida aparece tecleada en vez de de golpe.
/// </summary>
public class CodeTerminalUI : MonoBehaviour
{
    [Header("Fuente")]
    [Tooltip("PressStart2P: es la del juego y tiene acentos y signos de " +
             "apertura, que la Cyberpunk no tiene.")]
    public TMP_FontAsset font;

    [Header("Colores")]
    public Color screenColor = new Color(0.016f, 0.059f, 0.047f, 0.97f);
    public Color frameColor = new Color(0f, 0.85f, 0.62f, 1f);
    public Color codeColor = new Color(0.78f, 0.95f, 0.88f, 1f);
    public Color outputColor = new Color(0f, 1f, 0.53f, 1f);
    public Color errorColor = new Color(1f, 0.33f, 0.33f, 1f);
    public Color kiraColor = new Color(0f, 1f, 1f, 1f);

    private Canvas canvas;
    private GameObject root;
    private RectTransform screen;
    private Image screenImage;
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI codeText;
    private TextMeshProUGUI outputText;
    private TextMeshProUGUI kiraText;
    private TextMeshProUGUI footerText;

    private readonly List<string> outputLines = new List<string>();
    private Coroutine flicker;

    public bool IsOpen { get { return root != null && root.activeSelf; } }

    void Awake()
    {
        Build();
        root.SetActive(false);
    }

    // ---------------------------------------------------------------- armado

    void Build()
    {
        GameObject canvasGO = new GameObject("TerminalCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Por encima del HUD de corazones (que está en 40).
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        root = new GameObject("Terminal");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.SetParent(canvasGO.transform, false);
        Stretch(rootRect);

        // Oscurecemos el mundo para que la pantalla sea lo único que importa.
        GameObject dim = NewImage("Dimmer", rootRect, new Color(0f, 0f, 0f, 0.72f));
        Stretch(dim.GetComponent<RectTransform>());

        // ----- el monitor
        GameObject screenGO = NewImage("Pantalla", rootRect, screenColor);
        screen = screenGO.GetComponent<RectTransform>();
        screenImage = screenGO.GetComponent<Image>();
        screen.anchorMin = new Vector2(0.5f, 0.5f);
        screen.anchorMax = new Vector2(0.5f, 0.5f);
        screen.pivot = new Vector2(0.5f, 0.5f);
        screen.sizeDelta = new Vector2(1180f, 680f);
        screen.anchoredPosition = Vector2.zero;

        // Marco: cuatro barras finas en vez de un borde, que TMP/Image no tiene.
        AddBar(screen, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 6f), new Vector2(0f, -3f));
        AddBar(screen, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 6f), new Vector2(0f, 3f));
        AddBar(screen, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(6f, 0f), new Vector2(3f, 0f));
        AddBar(screen, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(6f, 0f), new Vector2(-3f, 0f));

        // Líneas de barrido del tubo. Es lo que más separa "monitor" de "div".
        GameObject scan = NewRawImage("Barrido", screen, ScanlineTexture());
        Stretch(scan.GetComponent<RectTransform>());
        RawImage raw = scan.GetComponent<RawImage>();
        raw.color = new Color(0f, 0f, 0f, 0.30f);
        raw.uvRect = new Rect(0f, 0f, 1f, 170f);
        raw.raycastTarget = false;

        headerText = NewText("Header", screen, 26f, frameColor, TextAlignmentOptions.Left);
        Place(headerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -58f), new Vector2(-44f, -20f));

        promptText = NewText("Consigna", screen, 24f, codeColor, TextAlignmentOptions.TopLeft);
        Place(promptText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -170f), new Vector2(-44f, -78f));

        codeText = NewText("Codigo", screen, 40f, codeColor, TextAlignmentOptions.Left);
        Place(codeText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -262f), new Vector2(-44f, -180f));

        outputText = NewText("Salida", screen, 24f, outputColor, TextAlignmentOptions.TopLeft);
        Place(outputText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(44f, 150f), new Vector2(-44f, -280f));

        kiraText = NewText("Kira", screen, 22f, kiraColor, TextAlignmentOptions.BottomLeft);
        Place(kiraText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(44f, 54f), new Vector2(-44f, 146f));

        footerText = NewText("Footer", screen, 18f, new Color(0.45f, 0.62f, 0.56f, 1f), TextAlignmentOptions.Left);
        Place(footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(44f, 18f), new Vector2(-44f, 48f));
        footerText.text = "ENTER ejecutar    ESC salir";
    }

    void AddBar(RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 size, Vector2 pos)
    {
        GameObject bar = NewImage("Marco", parent, frameColor);
        RectTransform r = bar.GetComponent<RectTransform>();
        r.anchorMin = aMin;
        r.anchorMax = aMax;
        r.sizeDelta = size;
        r.anchoredPosition = pos;
    }

    /// <summary>Textura de 1x4 con una línea oscura: tileada da el barrido del tubo.</summary>
    Texture2D ScanlineTexture()
    {
        Texture2D tex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.SetPixel(0, 0, new Color(0f, 0f, 0f, 1f));
        tex.SetPixel(0, 1, new Color(0f, 0f, 0f, 0f));
        tex.SetPixel(0, 2, new Color(0f, 0f, 0f, 0f));
        tex.SetPixel(0, 3, new Color(0f, 0f, 0f, 0f));
        tex.Apply();
        return tex;
    }

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

    GameObject NewRawImage(string name, Transform parent, Texture tex)
    {
        GameObject go = new GameObject(name);
        RectTransform r = go.AddComponent<RectTransform>();
        r.SetParent(parent, false);
        RawImage img = go.AddComponent<RawImage>();
        img.texture = tex;
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

    void Place(RectTransform r, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        r.anchorMin = aMin;
        r.anchorMax = aMax;
        r.offsetMin = offMin;
        r.offsetMax = offMax;
    }

    // ------------------------------------------------------------- operación

    public void Open(TerminalChallenge challenge)
    {
        outputLines.Clear();
        root.SetActive(true);

        headerText.text = "TERMINAL NEXCORP  ::  " + (challenge != null ? challenge.concept.ToUpper() : "");
        promptText.text = challenge != null ? challenge.prompt : "";
        kiraText.text = "";
        outputText.text = "";

        if (flicker != null) StopCoroutine(flicker);
        flicker = StartCoroutine(Flicker());
    }

    public void Close()
    {
        if (flicker != null) StopCoroutine(flicker);
        flicker = null;
        root.SetActive(false);
    }

    /// <summary>Redibuja la línea con lo que lleva tecleado y el cursor.</summary>
    public void SetTyped(TerminalChallenge challenge, string typed, bool caretVisible)
    {
        if (challenge == null) return;

        string hueco;
        if (string.IsNullOrEmpty(typed))
            hueco = "<color=#3F6B5C>" + challenge.Blank + "</color>";
        else
            hueco = "<mark=#00FF8820>" + Escape(typed) + "</mark>";

        string caret = caretVisible ? "<color=#00FF88>|</color>" : "<color=#00000000>|</color>";

        codeText.text = "<color=#4FD6A8>>>> </color>" +
                        Escape(challenge.codeBefore) + hueco + caret + Escape(challenge.codeAfter);
    }

    public void PushLine(string text, Color color)
    {
        outputLines.Add("<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>");

        // La pantalla es chica: nos quedamos con lo último, como una consola real.
        while (outputLines.Count > 6) outputLines.RemoveAt(0);

        outputText.text = string.Join("\n", outputLines.ToArray());
    }

    public void SayKira(string text)
    {
        kiraText.text = string.IsNullOrEmpty(text) ? "" : "KIRA: " + text;
    }

    public Color OutputColor { get { return outputColor; } }
    public Color ErrorColor { get { return errorColor; } }
    public Color CodeColor { get { return codeColor; } }

    /// <summary>Escribe una línea de salida letra por letra, como una consola de verdad.</summary>
    public IEnumerator TypeLine(string text, Color color, float perChar)
    {
        outputLines.Add("");
        while (outputLines.Count > 6) outputLines.RemoveAt(0);

        int index = outputLines.Count - 1;
        string hex = ColorUtility.ToHtmlStringRGB(color);

        for (int i = 1; i <= text.Length; i++)
        {
            outputLines[index] = "<color=#" + hex + ">" + text.Substring(0, i) + "</color>";
            outputText.text = string.Join("\n", outputLines.ToArray());
            yield return new WaitForSeconds(perChar);
        }
    }

    /// <summary>El tubo no da una luz pareja: este temblor chiquito es la diferencia
    /// entre un monitor y un rectángulo de color.</summary>
    IEnumerator Flicker()
    {
        Color baseColor = screenColor;
        while (true)
        {
            float n = Random.Range(-0.012f, 0.012f);
            screenImage.color = new Color(
                Mathf.Clamp01(baseColor.r + n),
                Mathf.Clamp01(baseColor.g + n),
                Mathf.Clamp01(baseColor.b + n),
                baseColor.a);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.18f));
        }
    }

    /// <summary>TMP se come los signos &lt; como etiquetas: hay que neutralizarlos.</summary>
    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("<", "<noparse><</noparse>");
    }
}
