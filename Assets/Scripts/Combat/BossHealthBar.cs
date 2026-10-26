using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// La barra del jefe, arriba al medio: el nombre, un tramo por fase (cada
/// terminal resuelta rompe uno), la temperatura (cuando se llena, el jefe
/// queda aturdido) y un renglón que dice qué hacer ahora.
///
/// Ese renglón es lo que hace entendible la pelea sin diálogos: la regla es
/// rara para un juego de tiros ("a los tiros no se le gana"), así que hay que
/// decirla en el momento.
///
/// Como la terminal, se arma sola en runtime.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Tooltip("PressStart2P.")]
    public TMP_FontAsset font;

    public Color nameColor = new Color(1f, 0.32f, 0.42f, 1f);
    public Color segmentColor = new Color(1f, 0.25f, 0.35f, 1f);
    public Color brokenColor = new Color(0.22f, 0.09f, 0.13f, 1f);
    public Color heatColor = new Color(1f, 0.62f, 0.18f, 1f);
    public Color statusColor = new Color(0.85f, 1f, 0.96f, 1f);

    private CanvasGroup group;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI statusText;
    private Image[] segments = new Image[0];
    private RectTransform segmentRow;
    private RectTransform heatFill;
    private Coroutine fading;

    void Awake()
    {
        Build();
        group.alpha = 0f;
        group.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------ lo que usa el jefe

    public void Show(string bossName, int phases)
    {
        nameText.text = bossName;
        BuildSegments(phases);
        SetHeat(0f);
        SetStatus("");
        group.gameObject.SetActive(true);
        Fade(1f);
    }

    public void Hide()
    {
        Fade(0f);
    }

    /// <summary>Cuántos tramos le quedan sanos.</summary>
    public void SetSegments(int left)
    {
        for (int i = 0; i < segments.Length; i++)
            segments[i].color = i < left ? segmentColor : brokenColor;
    }

    /// <summary>Temperatura, de 0 a 1.</summary>
    public void SetHeat(float t)
    {
        if (heatFill != null) heatFill.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
    }

    public void SetStatus(string text)
    {
        statusText.text = text;
    }

    // ------------------------------------------------------------ armado

    void Build()
    {
        GameObject canvasGO = new GameObject("BarraJefe");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Encima de los corazones (40), debajo de la terminal (90) y del libro (95).
        canvas.sortingOrder = 45;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        RectTransform p = panel.GetComponent<RectTransform>();
        p.SetParent(canvasGO.transform, false);
        p.anchorMin = p.anchorMax = new Vector2(0.5f, 1f);
        p.pivot = new Vector2(0.5f, 1f);
        p.sizeDelta = new Vector2(1000f, 160f);
        p.anchoredPosition = new Vector2(0f, -20f);
        group = panel.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        nameText = NewText("Nombre", p, 28f, nameColor);
        Row(nameText.rectTransform, 0f, 36f);

        GameObject row = new GameObject("Tramos", typeof(RectTransform));
        segmentRow = row.GetComponent<RectTransform>();
        segmentRow.SetParent(p, false);
        Row(segmentRow, 42f, 26f);

        // La temperatura: una rayita naranja debajo de los tramos.
        Image heatBack = NewImage("Temperatura", p, new Color(0f, 0f, 0f, 0.6f));
        RectTransform hb = heatBack.rectTransform;
        hb.anchorMin = hb.anchorMax = new Vector2(0.5f, 1f);
        hb.pivot = new Vector2(0.5f, 1f);
        hb.sizeDelta = new Vector2(486f, 10f);
        hb.anchoredPosition = new Vector2(0f, -76f);
        Image fill = NewImage("Relleno", hb, heatColor);
        heatFill = fill.rectTransform;
        heatFill.anchorMin = Vector2.zero;
        heatFill.anchorMax = new Vector2(0f, 1f);
        heatFill.offsetMin = Vector2.zero;
        heatFill.offsetMax = Vector2.zero;

        statusText = NewText("Que hacer", p, 18f, statusColor);
        statusText.textWrappingMode = TextWrappingModes.Normal;
        statusText.lineSpacing = 30f;
        Row(statusText.rectTransform, 96f, 60f);
    }

    void BuildSegments(int count)
    {
        foreach (Image old in segments)
            if (old != null) Destroy(old.gameObject);

        segments = new Image[Mathf.Max(1, count)];
        const float w = 150f, gap = 18f;
        float total = segments.Length * w + (segments.Length - 1) * gap;
        for (int i = 0; i < segments.Length; i++)
        {
            // Borde oscuro y el tramo adentro, para que se lea como una barra
            // partida y no como tres rectángulos sueltos.
            Image frame = NewImage("Tramo" + (i + 1), segmentRow, new Color(0f, 0f, 0f, 0.75f));
            RectTransform r = frame.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(w, 26f);
            r.anchoredPosition = new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), 0f);

            Image inside = NewImage("Vida", r, segmentColor);
            inside.rectTransform.anchorMin = Vector2.zero;
            inside.rectTransform.anchorMax = Vector2.one;
            inside.rectTransform.offsetMin = new Vector2(4f, 4f);
            inside.rectTransform.offsetMax = new Vector2(-4f, -4f);
            segments[i] = inside;
        }
    }

    void Fade(float to)
    {
        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(FadeRoutine(to));
    }

    IEnumerator FadeRoutine(float to)
    {
        float from = group.alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.4f;
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        group.alpha = to;
        if (to <= 0f) group.gameObject.SetActive(false);
        fading = null;
    }

    void Row(RectTransform r, float fromTop, float height)
    {
        r.anchorMin = new Vector2(0f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.offsetMin = new Vector2(0f, -fromTop - height);
        r.offsetMax = new Vector2(0f, -fromTop);
    }

    Image NewImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI NewText(string name, Transform parent, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        // Contorno oscuro: la barra va sobre el cielo y sobre el jefe, y sin
        // esto el texto claro se pierde.
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        return t;
    }
}
