using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GlowFlicker : MonoBehaviour
{
    public enum Mode { Pulse, Neon, Lamp }

    public Mode mode = Mode.Pulse;

    [Range(0f, 1f)] public float depth = 0.25f;

    public float speed = 0.5f;

    public Vector2 blinkEvery = new Vector2(3f, 9f);

    public SpriteRenderer[] linked;

    private SpriteRenderer sr;
    private Color baseColor;
    private Color[] linkedColors;
    private float seed;
    private float nextBurst;
    private float burstEnd;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        seed = Random.value * 100f;
        nextBurst = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);
        if (linked != null)
        {
            linkedColors = new Color[linked.Length];
            for (int i = 0; i < linked.Length; i++)
                if (linked[i] != null) linkedColors[i] = linked[i].color;
        }
    }

    void Update()
    {
        float t = Time.time + seed;
        float k = 1f;

        switch (mode)
        {
            case Mode.Pulse:
                k = 1f - depth * (0.5f + 0.5f * Mathf.Sin(t * speed * Mathf.PI * 2f));
                break;

            case Mode.Lamp:
                k = 1f - depth * Mathf.PerlinNoise(t * speed * 4f, seed);
                break;

            case Mode.Neon:
                if (Time.time > nextBurst)
                {
                    burstEnd = Time.time + Random.Range(0.08f, 0.35f);
                    nextBurst = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);
                }

                if (Time.time < burstEnd) k = Random.value < 0.55f ? 1f - depth : 1f;
                break;
        }

        Color c = baseColor;
        c.a *= k;
        sr.color = c;

        if (linked == null) return;
        for (int i = 0; i < linked.Length; i++)
        {
            if (linked[i] == null) continue;
            Color lc = linkedColors[i];

            float dim = Mathf.Lerp(1f, 0.45f, (1f - k) / Mathf.Max(0.01f, depth));
            linked[i].color = new Color(lc.r * dim, lc.g * dim, lc.b * dim, lc.a);
        }
    }
}
