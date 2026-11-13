using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IrisTransition : MonoBehaviour
{
    public static bool Active { get; private set; }

    const float CloseTime = 0.9f;
    const float HoldTime = 0.25f;
    const float OpenTime = 0.9f;

    RectTransform iris;
    Transform target;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { Active = false; }

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
        yield return null;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        target = player != null ? player.transform : null;

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

    Vector2 FocusOnScreen()
    {
        Camera cam = Camera.main;
        if (target == null || cam == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Vector3 world = target.position;
        SpriteRenderer sr = target.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) world = sr.bounds.center;

        return cam.WorldToScreenPoint(world);
    }

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

    void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        GameObject irisGO = new GameObject("Circulo", typeof(RectTransform), typeof(RawImage));
        iris = (RectTransform)irisGO.transform;
        iris.SetParent(transform, false);
        iris.anchorMin = iris.anchorMax = Vector2.zero;
        iris.pivot = new Vector2(0.5f, 0.5f);
        RawImage img = irisGO.GetComponent<RawImage>();
        img.texture = HoleTexture();
        img.raycastTarget = true;

        float big = 20000f;

        Panel(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(big, big));
        Panel(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(big, big));

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
