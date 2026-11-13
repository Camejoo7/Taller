using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PowerNode : MonoBehaviour
{
    public Color offColor = new Color(0.35f, 0.35f, 0.45f, 1f);

    public float delay = 0f;

    public float flashTime = 0.25f;

    private SpriteRenderer sr;
    private SpriteFlipbook flipbook;
    private Color onColor;
    private bool on;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        flipbook = GetComponent<SpriteFlipbook>();
        onColor = sr.color;
        sr.color = offColor;
        if (flipbook != null) flipbook.enabled = false;
    }

    public void PowerOn()
    {
        if (on) return;
        on = true;
        StartCoroutine(Ignite());
    }

    IEnumerator Ignite()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (flipbook != null) flipbook.enabled = true;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / flashTime;
            sr.color = Color.Lerp(Color.white * 1.5f, onColor, t);
            yield return null;
        }
        sr.color = onColor;
    }
}
