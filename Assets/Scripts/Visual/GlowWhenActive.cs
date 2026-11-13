using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GlowWhenActive : MonoBehaviour
{
    public Behaviour watch;

    public float onAlpha = 0.4f;

    public float fadeSpeed = 3f;

    public float pulse = 0.15f;

    private SpriteRenderer sr;
    private float level;
    private float seed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 10f;
        Apply();
    }

    void Update()
    {
        bool on = watch != null && watch.isActiveAndEnabled;
        level = Mathf.MoveTowards(level, on ? 1f : 0f, fadeSpeed * Time.deltaTime);
        Apply();
    }

    void Apply()
    {
        float p = 1f - pulse * (0.5f + 0.5f * Mathf.Sin((Time.time + seed) * 5f));
        Color c = sr.color;
        c.a = onAlpha * level * p;
        sr.color = c;
    }
}
