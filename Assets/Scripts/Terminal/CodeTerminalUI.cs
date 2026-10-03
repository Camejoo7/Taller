using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// La pantalla de la terminal. Se arma sola en runtime, así que alcanza con
/// poner el componente en un GameObject vacío y asignarle la fuente y el sprite
/// del monitor — no hay que montar un Canvas a mano para probarla en una escena
/// nueva.
///
/// El look importa tanto como la mecánica acá: si esto parece un formulario,
/// es un formulario. Por eso el texto va adentro de un monitor dibujado (pack
/// "Consola"), el tubo se prende con el barrido vertical de un CRT de verdad,
/// y acertar o errar deja un sello de ACCESS GRANTED / DENIED en el vidrio.
/// </summary>
public class CodeTerminalUI : MonoBehaviour
{
    [Header("Fuente")]
    [Tooltip("PressStart2P: es la del juego y tiene acentos y signos de " +
             "apertura, que la Cyberpunk no tiene.")]
    public TMP_FontAsset font;

    [Header("El monitor")]
    [Tooltip("El dibujo del monitor. Del pack: ASSETS/Consola/1 Monitor/. " +
             "Si se deja vacío, cae a un marco dibujado por código.")]
    public Sprite monitorSprite;

    [Tooltip("Alto del monitor en pixeles de canvas (referencia 1920x1080). " +
             "El ancho sale solo del aspecto del sprite.\n" +
             "1010 es 2.png a 5x exacto: el pixel art se ve nítido cuando la " +
             "escala es entera, y borroneado cuando no.")]
    public float monitorHeight = 1010f;

    [Tooltip("Dónde cae el vidrio DENTRO del sprite, en fracciones 0-1 " +
             "(xMin, yMin, xMax, yMax) medidas desde abajo a la izquierda.\n" +
             "Medidos sobre el pack:\n" +
             "2.png = (0.046, 0.243, 0.956, 0.911)\n" +
             "1.png = (0.032, 0.045, 0.972, 0.923)\n" +
             "3.png = (0.135, 0.369, 0.888, 0.906)\n" +
             "Si cambiás de sprite, cambiá estos cuatro números.")]
    public Vector4 glassRect = new Vector4(0.046f, 0.243f, 0.956f, 0.911f);

    [Header("Sellos")]
    [Tooltip("ASSETS/Consola/3 Other/Access_granted.png")]
    public Sprite grantedSprite;

    [Tooltip("ASSETS/Consola/3 Other/Access_denied.png")]
    public Sprite deniedSprite;

    [Header("Colores")]
    public Color screenColor = new Color(0.016f, 0.059f, 0.047f, 0.97f);
    public Color frameColor = new Color(0f, 0.85f, 0.62f, 1f);
    public Color codeColor = new Color(0.78f, 0.95f, 0.88f, 1f);
    public Color outputColor = new Color(0f, 1f, 0.53f, 1f);
    public Color errorColor = new Color(1f, 0.33f, 0.33f, 1f);
    public Color kiraColor = new Color(0f, 1f, 1f, 1f);

    // El layout de adentro está tuneado contra este tamaño y después se escala
    // entero. Así cambiar monitorHeight no desacomoda nada.
    const float DesignWidth = 1154f;
    const float DesignHeight = 650f;

    private Canvas canvas;
    private GameObject root;
    private RectTransform glass;
    private Image glassImage;
    private RectTransform content;
    private CanvasGroup contentGroup;
    private RectTransform roll;
    private Image stampImage;
    private RectTransform stampRect;
    private Image flashImage;
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI codeText;
    private TextMeshProUGUI outputText;
    private TextMeshProUGUI kiraText;
    private TextMeshProUGUI footerText;

    private readonly List<string> outputLines = new List<string>();
    private Coroutine flicker;
    private Coroutine stamping;
    private float glassHeight;

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
        GameObject dim = NewImage("Dimmer", rootRect, new Color(0f, 0f, 0f, 0.78f));
        Stretch(dim.GetComponent<RectTransform>());

        // ----- el mueble del monitor
        float aspect = monitorSprite != null
            ? monitorSprite.rect.width / monitorSprite.rect.height
            : DesignWidth / DesignHeight;
        float monitorWidth = monitorHeight * aspect;

        GameObject monitorGO = NewImage("Monitor", rootRect, Color.white);
        RectTransform monitor = monitorGO.GetComponent<RectTransform>();
        Center(monitor, new Vector2(monitorWidth, monitorHeight));
        Image monitorImage = monitorGO.GetComponent<Image>();
        monitorImage.sprite = monitorSprite;
        // Sin sprite el mueble sobra: lo dejamos transparente y queda solo el
        // vidrio, que es como se veía la terminal antes de tener el dibujo.
        if (monitorSprite == null) monitorImage.color = new Color(0f, 0f, 0f, 0f);

        // ----- el vidrio, recortado adentro del dibujo
        GameObject glassGO = NewImage("Vidrio", monitor, screenColor);
        glass = glassGO.GetComponent<RectTransform>();
        glassImage = glassGO.GetComponent<Image>();
        glass.anchorMin = new Vector2(glassRect.x, glassRect.y);
        glass.anchorMax = new Vector2(glassRect.z, glassRect.w);
        glass.offsetMin = Vector2.zero;
        glass.offsetMax = Vector2.zero;

        float glassWidth = monitorWidth * (glassRect.z - glassRect.x);
        glassHeight = monitorHeight * (glassRect.w - glassRect.y);

        // Líneas de barrido del tubo. Es lo que más separa "monitor" de "div".
        GameObject scan = NewRawImage("Barrido", glass, ScanlineTexture());
        Stretch(scan.GetComponent<RectTransform>());
        RawImage scanRaw = scan.GetComponent<RawImage>();
        scanRaw.color = new Color(0f, 0f, 0f, 0.30f);
        scanRaw.uvRect = new Rect(0f, 0f, 1f, glassHeight / 3f);
        scanRaw.raycastTarget = false;

        // La banda que recorre la pantalla de arriba a abajo: es el defecto de
        // sincronismo de un CRT, y es lo que lo delata como tubo y no como LCD.
        GameObject rollGO = NewRawImage("Banda", glass, BandTexture());
        roll = rollGO.GetComponent<RectTransform>();
        roll.anchorMin = new Vector2(0f, 1f);
        roll.anchorMax = new Vector2(1f, 1f);
        roll.pivot = new Vector2(0.5f, 0.5f);
        // Bien ancha y bien tenue: si se la ve como una franja, parece un error
        // de dibujado en vez del tubo respirando.
        roll.sizeDelta = new Vector2(0f, glassHeight * 0.45f);
        rollGO.GetComponent<RawImage>().color = new Color(0.6f, 1f, 0.85f, 0.022f);
        rollGO.GetComponent<RawImage>().raycastTarget = false;

        // Viñeta: el tubo es curvo, así que las esquinas pierden luz. Sin esto
        // el vidrio se ve como un rectángulo plano pegado encima del dibujo.
        GameObject vig = NewRawImage("Vineta", glass, VignetteTexture());
        Stretch(vig.GetComponent<RectTransform>());
        RawImage vigRaw = vig.GetComponent<RawImage>();
        vigRaw.color = Color.white;
        vigRaw.raycastTarget = false;

        // El tinte rojo del error va DEBAJO del texto: si va encima, el
        // traceback — que es lo que hay que leer — queda lavado.
        GameObject flashGO = NewImage("Flash", glass, new Color(1f, 0.15f, 0.15f, 0f));
        Stretch(flashGO.GetComponent<RectTransform>());
        flashImage = flashGO.GetComponent<Image>();

        // ----- el contenido, en su propio espacio de diseño
        GameObject contentGO = new GameObject("Contenido");
        content = contentGO.AddComponent<RectTransform>();
        content.SetParent(glass, false);
        Center(content, new Vector2(DesignWidth, DesignHeight));
        float fit = Mathf.Min(glassWidth / DesignWidth, glassHeight / DesignHeight);
        content.localScale = new Vector3(fit, fit, 1f);
        contentGroup = contentGO.AddComponent<CanvasGroup>();

        headerText = NewText("Header", content, 26f, frameColor, TextAlignmentOptions.Left);
        Place(headerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -58f), new Vector2(-300f, -20f));

        statusText = NewText("Estado", content, 20f, outputColor, TextAlignmentOptions.Right);
        Place(statusText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-290f, -56f), new Vector2(-44f, -22f));

        promptText = NewText("Consigna", content, 24f, codeColor, TextAlignmentOptions.TopLeft);
        Place(promptText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -186f), new Vector2(-44f, -76f));

        codeText = NewText("Codigo", content, 40f, codeColor, TextAlignmentOptions.Left);
        Place(codeText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -274f), new Vector2(-44f, -198f));

        // Raya separadora entre la consigna y lo que la consola va escupiendo.
        GameObject rule = NewImage("Raya", content, new Color(frameColor.r, frameColor.g, frameColor.b, 0.35f));
        Place(rule.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -286f), new Vector2(-44f, -284f));

        outputText = NewText("Salida", content, 24f, outputColor, TextAlignmentOptions.TopLeft);
        Place(outputText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(44f, 176f), new Vector2(-44f, -296f));

        kiraText = NewText("Kira", content, 21f, kiraColor, TextAlignmentOptions.BottomLeft);
        Place(kiraText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(44f, 64f), new Vector2(-44f, 170f));

        footerText = NewText("Footer", content, 18f, new Color(0.45f, 0.62f, 0.56f, 1f), TextAlignmentOptions.Left);
        Place(footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(44f, 22f), new Vector2(-44f, 52f));
        footerText.text = "ENTER ejecutar    ESC salir";

        // ----- el sello, encima de todo el vidrio
        GameObject stampGO = NewImage("Sello", glass, new Color(1f, 1f, 1f, 0f));
        stampRect = stampGO.GetComponent<RectTransform>();
        stampImage = stampGO.GetComponent<Image>();
        stampImage.preserveAspect = true;
        // Cae en el hueco que queda entre el traceback y la línea de Kira: el
        // veredicto no puede taparle el error, que es lo que hay que leer.
        Center(stampRect, new Vector2(glassHeight * 0.68f, glassHeight * 0.21f));
        stampRect.anchoredPosition = new Vector2(0f, -glassHeight * 0.17f);
        stampGO.SetActive(false);
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

    /// <summary>Degradé vertical suave para la banda que recorre el tubo.</summary>
    Texture2D BandTexture()
    {
        const int h = 32;
        Texture2D tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            float a = Mathf.Sin(t * Mathf.PI);
            tex.SetPixel(0, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }

    /// <summary>Oscurecimiento hacia los bordes: la curvatura del tubo.</summary>
    Texture2D VignetteTexture()
    {
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                // Distancia al centro en coordenadas -1..1, pero por eje: así
                // la pantalla se apaga más en las esquinas que en los lados.
                float u = (x / (float)(n - 1)) * 2f - 1f;
                float v = (y / (float)(n - 1)) * 2f - 1f;
                float d = Mathf.Sqrt(u * u * u * u + v * v * v * v);
                float a = Mathf.Clamp01((d - 0.55f) / 0.55f) * 0.75f;
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        }
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
        statusText.text = "";

        HideStamp();
        flashImage.color = new Color(1f, 0.15f, 0.15f, 0f);

        if (flicker != null) StopCoroutine(flicker);
        flicker = StartCoroutine(Flicker());

        StartCoroutine(PowerOn());
    }

    public void Close()
    {
        StopAllCoroutines();
        flicker = null;
        stamping = null;
        root.SetActive(false);
    }

    /// <summary>El tubo no se prende de golpe: primero es una raya y después se
    /// abre. Son dos décimas, pero es la diferencia entre encender un monitor y
    /// que aparezca un panel.</summary>
    IEnumerator PowerOn()
    {
        contentGroup.alpha = 0f;
        glass.localScale = new Vector3(1f, 0.02f, 1f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.18f;
            glass.localScale = new Vector3(1f, Mathf.SmoothStep(0.02f, 1f, t), 1f);
            yield return null;
        }
        glass.localScale = Vector3.one;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.12f;
            contentGroup.alpha = t;
            yield return null;
        }
        contentGroup.alpha = 1f;

        StartCoroutine(Roll());
        StartCoroutine(Blink());
    }

    /// <summary>La banda de desincronismo bajando por el tubo.</summary>
    IEnumerator Roll()
    {
        float y = 0f;
        while (true)
        {
            y -= Time.deltaTime * glassHeight * 0.22f;
            if (y < -glassHeight - roll.sizeDelta.y) y = roll.sizeDelta.y;
            roll.anchoredPosition = new Vector2(0f, y);
            yield return null;
        }
    }

    /// <summary>El led de "en línea" del encabezado.</summary>
    IEnumerator Blink()
    {
        bool on = true;
        while (true)
        {
            statusText.text = on ? "● EN LÍNEA" : "<color=#00000000>●</color> EN LÍNEA";
            on = !on;
            yield return new WaitForSeconds(0.6f);
        }
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

    /// <summary>
    /// Escribe una salida letra por letra, como una consola de verdad. Si
    /// trae varias líneas (un for que imprime varias veces), va renglón por
    /// renglón: cada uno ocupa su lugar en la pantalla y empuja los viejos.
    /// </summary>
    public IEnumerator TypeLine(string text, Color color, float perChar)
    {
        string hex = ColorUtility.ToHtmlStringRGB(color);

        foreach (string line in text.Split('\n'))
        {
            outputLines.Add("");
            while (outputLines.Count > 6) outputLines.RemoveAt(0);

            int index = outputLines.Count - 1;

            for (int i = 1; i <= line.Length; i++)
            {
                outputLines[index] = "<color=#" + hex + ">" + line.Substring(0, i) + "</color>";
                outputText.text = string.Join("\n", outputLines.ToArray());
                yield return new WaitForSeconds(perChar);
            }
        }
    }

    // ----------------------------------------------------------------- sello

    /// <summary>El veredicto estampado en el vidrio. Un cartel de "correcto" se
    /// hace en cualquier página; que el monitor te lo escupa en la cara con el
    /// tubo temblando, no.</summary>
    public void ShowStamp(bool granted)
    {
        Sprite sprite = granted ? grantedSprite : deniedSprite;
        if (sprite == null) return;

        if (stamping != null) StopCoroutine(stamping);
        stamping = StartCoroutine(StampRoutine(sprite, granted));
    }

    public void HideStamp()
    {
        if (stamping != null) StopCoroutine(stamping);
        stamping = null;
        if (stampImage != null) stampImage.gameObject.SetActive(false);
    }

    IEnumerator StampRoutine(Sprite sprite, bool granted)
    {
        stampImage.sprite = sprite;
        stampImage.gameObject.SetActive(true);

        // Errar sacude la pantalla en rojo; acertar no necesita castigo visual.
        if (!granted) StartCoroutine(RedFlash());

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.14f;
            float s = Mathf.Lerp(1.45f, 1f, Mathf.SmoothStep(0f, 1f, t));
            stampRect.localScale = new Vector3(s, s, 1f);
            stampImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t * 1.6f));
            yield return null;
        }
        stampRect.localScale = Vector3.one;
        stampImage.color = Color.white;

        yield return new WaitForSeconds(granted ? 1.1f : 0.8f);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.25f;
            stampImage.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        stampImage.gameObject.SetActive(false);
        stamping = null;
    }

    IEnumerator RedFlash()
    {
        // 0.09 parece poquísimo escrito, pero el proyecto renderiza en espacio
        // lineal y un rojo saturado sobre el vidrio casi negro sube muchísimo:
        // a 0.22 la pantalla entera se vuelve roja y no se lee nada.
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            flashImage.color = new Color(1f, 0.15f, 0.15f, 0.09f * (1f - t));
            yield return null;
        }
        flashImage.color = new Color(1f, 0.15f, 0.15f, 0f);
    }

    /// <summary>El tubo no da una luz pareja: este temblor chiquito es la diferencia
    /// entre un monitor y un rectángulo de color.</summary>
    IEnumerator Flicker()
    {
        Color baseColor = screenColor;
        while (true)
        {
            float n = Random.Range(-0.012f, 0.012f);
            glassImage.color = new Color(
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
