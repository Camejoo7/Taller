using UnityEngine;

/// <summary>
/// Le da vida a un brillo (o a cualquier sprite): un pulso suave que respira,
/// el parpadeo de un tubo de neón viejo, o el temblor de una llama/lámpara.
///
/// Toca solo el alfa del color de su propio SpriteRenderer, así que el tinte
/// se respeta. Los sprites de "linked" (por ejemplo, el cartel al que le
/// pertenece el brillo) se oscurecen en el mismo momento, para que el cartel y
/// su halo parpadeen juntos.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GlowFlicker : MonoBehaviour
{
    public enum Mode { Pulse, Neon, Lamp }

    [Tooltip("Pulse = respira parejo. Neon = prendido, con ráfagas de parpadeo " +
             "cada tanto. Lamp = tiembla un poco, como una lámpara vieja.")]
    public Mode mode = Mode.Pulse;

    [Tooltip("Cuánto baja en el punto más bajo. 0 = nada, 1 = se apaga del todo.")]
    [Range(0f, 1f)] public float depth = 0.25f;

    [Tooltip("Ciclos por segundo del pulso / velocidad del temblor.")]
    public float speed = 0.5f;

    [Tooltip("Neon: cada cuántos segundos (mínimo y máximo) viene una ráfaga.")]
    public Vector2 blinkEvery = new Vector2(3f, 9f);

    [Tooltip("Sprites que se oscurecen junto con este brillo (el cartel, el tubo).")]
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
                // Durante la ráfaga se prende y se apaga cuadro a cuadro.
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
            // El cartel no se vuelve transparente: se apaga (más oscuro).
            float dim = Mathf.Lerp(1f, 0.45f, (1f - k) / Mathf.Max(0.01f, depth));
            linked[i].color = new Color(lc.r * dim, lc.g * dim, lc.b * dim, lc.a);
        }
    }
}
