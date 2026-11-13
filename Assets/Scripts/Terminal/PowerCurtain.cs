using System.Collections;
using UnityEngine;

public class PowerCurtain : MonoBehaviour
{
    [Header("Qué zona tapa, relativo a este objeto")]
    public float leftEdge = 0.3f;

    public float rightEdge = 30f;

    public float bottom = -7f;
    public float top = 4f;

    [Header("Aspecto")]
    public float softEdge = 0.5f;

    public Color darkness = new Color(0.055f, 0.065f, 0.095f, 1f);

    [Header("Capa de dibujo")]
    public string sortingLayer = "Personaje";

    public int sortingOrder = 8;

    [Header("El encendido")]
    public float sweepDuration = 1.2f;

    public float sweepDelay = 0f;

    private Transform quad;
    private SpriteRenderer sr;
    private Vector2 spriteSize;
    private bool revealed;

    public bool IsRevealed { get { return revealed; } }

    void Awake()
    {
        Build();
        Place(leftEdge);
    }

    void Build()
    {
        GameObject go = new GameObject("Oscuridad");
        quad = go.transform;
        quad.SetParent(transform, false);

        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GradientSprite();

        Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (unlit != null) sr.material = new Material(unlit);

        sr.color = new Color(darkness.r, darkness.g, darkness.b, 1f);

        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        spriteSize = sr.sprite.bounds.size;
    }

    void Place(float x)
    {
        float width = Mathf.Max(0f, rightEdge - x);
        float height = top - bottom;

        quad.position = transform.position + new Vector3(x, (top + bottom) * 0.5f, 0f);
        quad.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
    }

    public void Reveal()
    {
        if (revealed) return;
        revealed = true;
        StartCoroutine(Sweep());
    }

    public void ResetCurtain()
    {
        StopAllCoroutines();
        revealed = false;
        quad.gameObject.SetActive(true);
        Place(leftEdge);
    }

    IEnumerator Sweep()
    {
        if (sweepDelay > 0f) yield return new WaitForSeconds(sweepDelay);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / sweepDuration;

            Place(Mathf.Lerp(leftEdge, rightEdge, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        quad.gameObject.SetActive(false);
    }

    Sprite GradientSprite()
    {
        const int w = 256;
        const int h = 16;

        float total = Mathf.Max(0.01f, rightEdge - leftEdge);
        int ramp = Mathf.Clamp(Mathf.RoundToInt(w * (softEdge / total)), 1, w - 1);

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < w; x++)
        {
            float a = x >= ramp ? 1f : Mathf.SmoothStep(0f, 1f, x / (float)ramp);
            Color c = new Color(1f, 1f, 1f, a);
            for (int y = 0; y < h; y++) tex.SetPixel(x, y, c);
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0f, 0.5f), 100f);
    }
}
