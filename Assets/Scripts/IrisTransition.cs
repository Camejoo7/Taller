using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pasaje entre escenas con un círculo: la pantalla se cierra en un círculo
/// negro que se achica sobre el jugador, carga la escena siguiente y se abre
/// otra vez desde el jugador nuevo.
///
/// Se arma sola en runtime (no hay que poner nada en la escena) y sobrevive al
/// cambio de escena con DontDestroyOnLoad. El círculo es una imagen negra con
/// un agujero redondo, rodeada de cuatro paneles negros que tapan el resto:
/// achicar la imagen achica el agujero y los paneles la siguen.
/// Todo en tiempo real, por si se sale con el juego congelado.
/// </summary>
public class IrisTransition : MonoBehaviour
{
    /// <summary>Mientras hay un círculo en curso. El fundido viejo lo mira para no taparlo.</summary>
    public static bool Active { get; private set; }

    const float CloseTime = 0.9f;
    const float HoldTime = 0.25f;
    const float OpenTime = 0.9f;

    RectTransform iris;
    Transform target;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { Active = false; }

    /// <summary>Cierra sobre <paramref name="focus"/>, carga la escena y abre sobre el jugador nuevo.</summary>
    public static void LoadScene(string sceneName, Transform focus)
    {
        if (Active) return;
        Active = true;

        GameObject go = new GameObject("TransicionCirculo");
        DontDestroyOnLoad(go);
        IrisTransition t = go.AddComponent<IrisTransition>();
        t.Build();
        t.target = focus;
        t.StartCoroutine(t.Run(sceneName));
    }

    IEnumerator Run(string sceneName)
    {
        float max = MaxRadius();

        for (float t = 0f; t < CloseTime; t += Time.unscaledDeltaTime)
        {
            SetIris(Mathf.Lerp(max, 0f, Ease(t / CloseTime)));
            yield return null;
        }
        SetIris(0f);

        yield return new WaitForSecondsRealtime(HoldTime);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
        yield return null;   // que la escena nueva haga su Awake/Start

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        target = player != null ? player.transform : null;

        // Un cuadro más para que la cámara se acomode sobre el jugador.
        yield return null;

        max = MaxRadius();
        for (float t = 0f; t < OpenTime; t += Time.unscaledDeltaTime)
        {
            SetIris(Mathf.Lerp(0f, max, Ease(t / OpenTime)));
            yield return null;
        }

        Active = false;
        Destroy(gameObject);
    }

    static float Ease(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    /// <summary>Dónde está el foco en la pantalla, en píxeles. Sin foco, el centro.</summary>
    Vector2 FocusOnScreen()
    {
        Camera cam = Camera.main;
        if (target == null || cam == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // El centro del dibujo y no los pies del personaje.
        Vector3 world = target.position;
        SpriteRenderer sr = target.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) world = sr.bounds.center;

        return cam.WorldToScreenPoint(world);
    }

    /// <summary>Radio que deja toda la pantalla a la vista desde el foco.</summary>
    float MaxRadius()
    {
        Vector2 c = FocusOnScreen();
        float dx = Mathf.Max(c.x, Screen.width - c.x);
        float dy = Mathf.Max(c.y, Screen.height - c.y);
        return Mathf.Sqrt(dx * dx + dy * dy) + 20f;
    }

    void SetIris(float radius)
    {
        iris.anchoredPosition = FocusOnScreen();
        iris.sizeDelta = Vector2.one * Mathf.Max(0f, radius * 2f);
    }

    // ------------------------------------------------------------ armado

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;   // por encima de todo, también de la pausa

        GameObject irisGO = new GameObject("Circulo", typeof(RectTransform), typeof(RawImage));
        iris = (RectTransform)irisGO.transform;
        iris.SetParent(transform, false);
        iris.anchorMin = iris.anchorMax = Vector2.zero;   // posición en píxeles desde abajo a la izquierda
        iris.pivot = new Vector2(0.5f, 0.5f);
        RawImage img = irisGO.GetComponent<RawImage>();
        img.texture = HoleTexture();
        img.raycastTarget = true;   // que no se pueda clickear nada a través

        float big = 20000f;
        // Izquierda y derecha: altísimos, tapan también las esquinas.
        Panel(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(big, big));
        Panel(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(big, big));
        // Arriba y abajo: del ancho del círculo.
        Panel(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, big));
        Panel(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, big));

        SetIris(MaxRadius());
    }

    void Panel(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size)
    {
        GameObject go = new GameObject("Negro", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(iris, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = Color.black;
    }

    /// <summary>Negro con un círculo transparente que toca los bordes, con el borde suavizado.</summary>
    static Texture2D HoleTexture()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float r = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                byte a = (byte)(Mathf.Clamp01(d - r + 1f) * 255f);
                px[y * size + x] = new Color32(0, 0, 0, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
