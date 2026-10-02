using TMPro;
using UnityEngine;

/// <summary>
/// El cartelito de "apretá E" que aparece al lado de algo usable.
///
/// Una letra suelta flotando no se lee como un juego: se lee como texto de
/// debug. Esto dibuja una **tecla** — cuerpo, borde, brillo arriba y sombra
/// abajo — que entra de golpe con un rebote, flota, y respira. El objeto que
/// lo usa no tiene que saber nada de eso: lo prende y lo apaga con SetActive,
/// que es lo que <see cref="CodeTerminal"/> ya hacía.
///
/// Se arma solo en runtime, así que alcanza con un GameObject vacío.
/// </summary>
public class InteractPrompt : MonoBehaviour
{
    [Header("Qué tecla")]
    public string key = "E";

    [Tooltip("La del juego. Si queda vacío busca PressStart2P en el proyecto.")]
    public TMP_FontAsset font;

    [Header("Tamaño")]
    [Tooltip("Lado de la tecla en unidades de mundo. El jugador mide 0,38, " +
             "así que 0,22 es una tecla que se ve sin tapar el objeto.")]
    public float size = 0.22f;

    [Tooltip("Capa de dibujo. Tiene que estar por delante del nivel.")]
    public string sortingLayer = "Personaje";

    [Tooltip("Por encima del jugador: el cartel es información, no decorado " +
             "del mundo, y que el jugador lo tape es peor que al revés.")]
    public int sortingOrder = 20;

    [Header("Colores")]
    public Color bodyColor = new Color(0.07f, 0.10f, 0.14f, 0.96f);
    public Color borderColor = new Color(0.35f, 1f, 0.85f, 1f);
    public Color letterColor = new Color(0.88f, 1f, 0.97f, 1f);

    [Header("Movimiento")]
    [Tooltip("Cuánto sube y baja, en unidades.")]
    public float bobAmount = 0.035f;

    public float bobSpeed = 2.6f;

    [Tooltip("Lo que tarda el rebote de entrada.")]
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
        // Cada vez que el jugador se acerca, la tecla vuelve a entrar de golpe.
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

    /// <summary>t = 0 recién aparecida, t = 1 ya instalada y flotando.</summary>
    void Apply(float t)
    {
        // Rebote: se pasa de largo y vuelve. Entrar derecho a la escala final
        // se siente como que el objeto "estaba ahí", no como que apareció.
        float scale = Overshoot(t);
        cap.localScale = new Vector3(scale, scale, 1f);

        float bob = Mathf.Sin(bobTimer * bobSpeed) * bobAmount * t;
        transform.localPosition = baseLocalPos + new Vector3(0f, bob, 0f);

        // El borde respira; el cuerpo no, para que la letra siga legible.
        float pulse = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(bobTimer * 3.4f));
        borderRenderer.color = new Color(borderColor.r, borderColor.g, borderColor.b,
                                         borderColor.a * pulse * t);
        bodyRenderer.color = new Color(1f, 1f, 1f, t);
        label.alpha = t;
    }

    static float Overshoot(float t)
    {
        if (t >= 1f) return 1f;
        // Arranca en 0, se pasa hasta ~1,25 y asienta en 1.
        const float s = 1.9f;
        float u = t - 1f;
        return 1f + u * u * ((s + 1f) * u + s);
    }

    // ---------------------------------------------------------------- armado

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
        // fontSize en mundo rinde ~0,1 unidades por punto: 1,2 da una letra de
        // 0,12, que adentro de una tecla de 0,22 deja aire a los costados.
        label.fontSize = size * 5.5f;
        label.color = letterColor;
        label.alignment = TextAlignmentOptions.Center;

        RectTransform lr = label.rectTransform;
        lr.sizeDelta = new Vector2(size * 2f, size * 2f);
        // Un pelín arriba del centro: la PressStart2P cuelga de la línea base
        // y sin esto la letra se ve hundida adentro de la tecla.
        lr.localPosition = new Vector3(0f, size * 0.06f, -0.01f);

        label.GetComponent<MeshRenderer>().sortingLayerName = sortingLayer;
        label.GetComponent<MeshRenderer>().sortingOrder = sortingOrder + 2;
    }

    static TMP_FontAsset FindGameFont()
    {
        // La misma que usa el resto del juego. Si no está, TMP pone la suya.
        TMP_FontAsset f = Resources.Load<TMP_FontAsset>("PressStart2P-Regular SDF");
        return f != null ? f : TMP_Settings.defaultFontAsset;
    }

    /// <summary>
    /// Dibuja la tecla en dos capas separadas: el cuerpo (oscuro, con sombra
    /// abajo y brillo arriba) y el borde (blanco, para que el tinte del
    /// SpriteRenderer lo pueda hacer latir sin tocar el cuerpo).
    ///
    /// 16x16 a ~73 PPU da los 0,22 de <see cref="size"/> con pixeles del
    /// tamaño de los del nivel. Una textura más grande se vería más "fina"
    /// que el resto y cantaría que viene de otro lado.
    /// </summary>
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

                // y crece hacia arriba en una Texture2D.
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
