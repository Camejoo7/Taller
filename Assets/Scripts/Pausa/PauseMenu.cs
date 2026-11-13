using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausaCanvas;

    [Header("Fondo de la pausa")]
    [Range(0, 6)] public int blurSteps = 4;

    [Range(0f, 1f)] public float dimAlpha = 0.6f;

    public static bool IsPaused { get; private set; }

    private RawImage blurImage;
    private RenderTexture blurTexture;

    void Start()
    {
        pausaCanvas.SetActive(false);
        IsPaused = false;

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
        if (KiraBook.BlocksInput) return;

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

    IEnumerator ShowMenu()
    {
        yield return new WaitForEndOfFrame();
        if (!IsPaused) yield break;

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

    void TakeBlurredShot()
    {
        Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
        if (shot == null) return;

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

        for (int i = 0; i < blurSteps; i++)
        {
            w = Mathf.Max(8, w / 2);
            h = Mathf.Max(8, h / 2);
            current = Step(source, current, w, h);
            source = current;
        }

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

    static RenderTexture Step(Texture source, RenderTexture previousStep, int w, int h)
    {
        RenderTexture next = RenderTexture.GetTemporary(w, h, 0);
        next.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, next);
        if (previousStep != null) RenderTexture.ReleaseTemporary(previousStep);
        return next;
    }

    void EnsureBlurImage()
    {
        if (blurImage != null) return;

        GameObject go = new GameObject("FondoBorroso", typeof(RectTransform), typeof(RawImage));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(pausaCanvas.transform, false);
        rt.SetAsFirstSibling();
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
