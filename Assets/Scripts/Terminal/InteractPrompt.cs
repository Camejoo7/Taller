using TMPro;
using UnityEngine;

public class InteractPrompt : MonoBehaviour
{
    [Header("Qué tecla")]
    public string key = "E";

    public TMP_FontAsset font;

    [Header("Tamaño")]
    public float size = 0.22f;

    public string sortingLayer = "Personaje";

    public int sortingOrder = 20;

    [Header("Colores")]
    public Color bodyColor = new Color(0.07f, 0.10f, 0.14f, 0.96f);
    public Color borderColor = new Color(0.35f, 1f, 0.85f, 1f);
    public Color letterColor = new Color(0.88f, 1f, 0.97f, 1f);

    [Header("Movimiento")]
    public float bobAmount = 0.035f;

    public float bobSpeed = 2.6f;

    public float popTime = 0.22f;

    private Transform cap;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer borderRenderer;
    private TextMeshPro label;
    private Vector3 baseLocalPos;
    private float popTimer;
    private float bobTimer;

    void Awake()
    {
        baseLocalPos = transform.localPosition;
        Build();
    }

    void OnEnable()
    {
        popTimer = 0f;
        bobTimer = 0f;
        Apply(0f);
    }

    void Update()
    {
        if (popTimer < popTime)
        {
            popTimer += Time.deltaTime;
            bobTimer += Time.deltaTime;
            Apply(Mathf.Clamp01(popTimer / popTime));
            return;
        }

        bobTimer += Time.deltaTime;
        Apply(1f);
    }

    void Apply(float t)
    {
        float scale = Overshoot(t);
        cap.localScale = new Vector3(scale, scale, 1f);

        float bob = Mathf.Sin(bobTimer * bobSpeed) * bobAmount * t;
        transform.localPosition = baseLocalPos + new Vector3(0f, bob, 0f);

        float pulse = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(bobTimer * 3.4f));
        borderRenderer.color = new Color(borderColor.r, borderColor.g, borderColor.b,
                                         borderColor.a * pulse * t);
        bodyRenderer.color = new Color(1f, 1f, 1f, t);
        label.alpha = t;
    }

    static float Overshoot(float t)
    {
        if (t >= 1f) return 1f;

        const float s = 1.9f;
        float u = t - 1f;
        return 1f + u * u * ((s + 1f) * u + s);
    }

    void Build()
    {
        GameObject capGO = new GameObject("Tecla");
        cap = capGO.transform;
        cap.SetParent(transform, false);

        bodyRenderer = capGO.AddComponent<SpriteRenderer>();
        bodyRenderer.sprite = KeycapSprite(false);
        bodyRenderer.sortingLayerName = sortingLayer;
        bodyRenderer.sortingOrder = sortingOrder;

        GameObject borderGO = new GameObject("Borde");
        borderGO.transform.SetParent(cap, false);
        borderRenderer = borderGO.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = KeycapSprite(true);
        borderRenderer.sortingLayerName = sortingLayer;
        borderRenderer.sortingOrder = sortingOrder + 1;
        borderRenderer.color = borderColor;

        GameObject labelGO = new GameObject("Letra");
        labelGO.transform.SetParent(cap, false);
        label = labelGO.AddComponent<TextMeshPro>();
        label.text = key;
        label.font = font != null ? font : FindGameFont();

        label.fontSize = size * 5.5f;
        label.color = letterColor;
        label.alignment = TextAlignmentOptions.Center;

        RectTransform lr = label.rectTransform;
        lr.sizeDelta = new Vector2(size * 2f, size * 2f);

        lr.localPosition = new Vector3(0f, size * 0.06f, -0.01f);

        label.GetComponent<MeshRenderer>().sortingLayerName = sortingLayer;
        label.GetComponent<MeshRenderer>().sortingOrder = sortingOrder + 2;
    }

    static TMP_FontAsset FindGameFont()
    {
        TMP_FontAsset f = Resources.Load<TMP_FontAsset>("PressStart2P-Regular SDF");
        return f != null ? f : TMP_Settings.defaultFontAsset;
    }

    Sprite KeycapSprite(bool border)
    {
        const int n = 16;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color shade = new Color(bodyColor.r * 0.55f, bodyColor.g * 0.55f,
                                bodyColor.b * 0.55f, bodyColor.a);
        Color shine = new Color(1f, 1f, 1f, 0.22f);

        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                bool corner = (x == 0 || x == n - 1) && (y == 0 || y == n - 1);
                bool edge = !corner && (x == 0 || x == n - 1 || y == 0 || y == n - 1);

                if (border)
                {
                    tex.SetPixel(x, y, edge ? Color.white : clear);
                    continue;
                }

                if (corner || edge) { tex.SetPixel(x, y, clear); continue; }

                if (y == 1) { tex.SetPixel(x, y, shade); continue; }
                if (y == n - 2) { tex.SetPixel(x, y, shine); continue; }

                tex.SetPixel(x, y, bodyColor);
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f),
                             n / size);
    }
}
