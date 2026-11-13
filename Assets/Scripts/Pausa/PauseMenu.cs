using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausaCanvas;

    [Header("Fondo de la pausa")]
    [Tooltip("Cuánto se desenfoca el juego detrás del menú. Cada punto achica " +
             "la foto del juego a la mitad antes de estirarla de nuevo: más " +
             "alto = más borroso. 0 = sin desenfoque.")]
    [Range(0, 6)] public int blurSteps = 4;

    [Tooltip("Qué tan oscuro queda el juego detrás del menú (0 = sin oscurecer, 1 = negro).")]
    [Range(0f, 1f)] public float dimAlpha = 0.6f;

    /// <summary>El libro de Kira lo consulta para no abrirse encima de la pausa.</summary>
    public static bool IsPaused { get; private set; }

    private RawImage blurImage;
    private RenderTexture blurTexture;

    void Start()
    {
        pausaCanvas.SetActive(false);
        IsPaused = false;

        // El oscurecido es la imagen "Dimmer" del canvas: su fuerza se maneja
        // desde acá para tener los dos ajustes del fondo en un solo lugar.
        Transform dimmer = pausaCanvas.transform.Find("Dimmer");
        Image dimmerImage = dimmer != null ? dimmer.GetComponent<Image>() : null;
        if (dimmerImage != null)
        {
            Color c = dimmerImage.color;
            c.a = dimAlpha;
            dimmerImage.color = c;
        }
    }

    void Update()
    {
        // El ESC que cierra el libro no tiene que abrir también la pausa.
        if (KiraBook.BlocksInput) return;

        // Adentro de una terminal ESC es de la terminal (la cierra), y en el
        // cuadro en que se cierra tampoco cuenta para la pausa.
        if (CodeTerminal.AnyOpen || CodeTerminal.ClosedFrame == Time.frameCount) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused)
                Reanudar();
            else
                Pausar();
        }
    }

    public void Pausar()
    {
        if (IsPaused) return;

        Time.timeScale = 0f;
        IsPaused = true;
        StartCoroutine(ShowMenu());
    }

    /// <summary>
    /// El menú se prende al final del cuadro y no en el acto: antes hay que
    /// sacarle la foto al juego para desenfocarla, y si el menú ya estuviera
    /// en pantalla saldría en la foto.
    /// </summary>
    IEnumerator ShowMenu()
    {
        yield return new WaitForEndOfFrame();
        if (!IsPaused) yield break;   // lo reanudaron en el mismo cuadro

        if (blurSteps > 0) TakeBlurredShot();
        pausaCanvas.SetActive(true);
    }

    public void Reanudar()
    {
        pausaCanvas.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
        ReleaseBlur();
    }

    public void Reiniciar()
    {
        // El stage arranca como cuando se entró: el libro con las páginas que
        // tenía y los diálogos de este stage otra vez. (Morir no hace esto.)
        RunProgress.RestartStage();
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        ReleaseBlur();
    }

    // ------------------------------------------------------------ desenfoque

    /// <summary>
    /// Saca una foto de la pantalla y la desenfoca. Como el juego está
    /// congelado, alcanza con una foto quieta: no hace falta desenfocar en
    /// vivo. El desenfoque es achicar la foto a la mitad varias veces (cada
    /// vez se promedian los píxeles vecinos) y volver a agrandarla.
    /// </summary>
    void TakeBlurredShot()
    {
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        if (shot == null) return;

        // El proyecto trabaja en espacio de color lineal, y ahí Unity entrega
        // la captura marcada como "lineal" aunque trae los colores tal cual
        // se ven en pantalla. Usada así, la foto sale lavada (más clara que
        // el juego). Se pasan los mismos píxeles a una textura bien marcada.
        if (QualitySettings.activeColorSpace == ColorSpace.Linear && !shot.isDataSRGB)
        {
            Texture2D corrected = new Texture2D(shot.width, shot.height, shot.format, false, false);
            corrected.LoadRawTextureData(shot.GetRawTextureData<byte>());
            corrected.Apply(false, true);
            Destroy(shot);
            shot = corrected;
        }

        RenderTexture previous = RenderTexture.active;

        int w = shot.width, h = shot.height;
        Texture source = shot;
        RenderTexture current = null;

        // Achicar.
        for (int i = 0; i < blurSteps; i++)
        {
            w = Mathf.Max(8, w / 2);
            h = Mathf.Max(8, h / 2);
            current = Step(source, current, w, h);
            source = current;
        }

        // Agrandar de a poco (dos escalones) para que no queden cuadraditos.
        for (int i = 0; i < Mathf.Min(2, blurSteps - 1); i++)
        {
            w *= 2;
            h *= 2;
            current = Step(source, current, w, h);
            source = current;
        }

        ReleaseBlur();
        blurTexture = new RenderTexture(w, h, 0);
        blurTexture.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, blurTexture);

        RenderTexture.active = previous;
        if (current != null) RenderTexture.ReleaseTemporary(current);
        Destroy(shot);

        EnsureBlurImage();
        blurImage.texture = blurTexture;
        blurImage.enabled = true;
    }

    /// <summary>Copia 'source' a una textura nueva de w x h y suelta la anterior.</summary>
    static RenderTexture Step(Texture source, RenderTexture previousStep, int w, int h)
    {
        RenderTexture next = RenderTexture.GetTemporary(w, h, 0);
        next.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, next);
        if (previousStep != null) RenderTexture.ReleaseTemporary(previousStep);
        return next;
    }

    /// <summary>La imagen que muestra la foto: se arma sola, al fondo del canvas de pausa.</summary>
    void EnsureBlurImage()
    {
        if (blurImage != null) return;

        GameObject go = new GameObject("FondoBorroso", typeof(RectTransform), typeof(RawImage));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(pausaCanvas.transform, false);
        rt.SetAsFirstSibling();   // detrás del oscurecido y del panel
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        blurImage = go.GetComponent<RawImage>();
        blurImage.raycastTarget = false;
    }

    void ReleaseBlur()
    {
        if (blurImage != null)
        {
            blurImage.texture = null;
            blurImage.enabled = false;
        }

        if (blurTexture != null)
        {
            blurTexture.Release();
            Destroy(blurTexture);
            blurTexture = null;
        }
    }
}
