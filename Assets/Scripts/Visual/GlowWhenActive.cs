using UnityEngine;

/// <summary>
/// Un brillo que se prende solo cuando otra cosa está andando: mira si un
/// componente está habilitado (por ejemplo el SpriteFlipbook de un nodo, que
/// PowerNode prende recién cuando se resuelve la terminal) y funde su alfa en
/// consecuencia. Así el decorado acompaña a la consecuencia del ejercicio sin
/// tocar el código de la terminal.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GlowWhenActive : MonoBehaviour
{
    [Tooltip("El componente que manda: prendido = brillo prendido.")]
    public Behaviour watch;

    [Tooltip("Alfa del brillo cuando está prendido.")]
    public float onAlpha = 0.4f;

    [Tooltip("Qué tan rápido se prende/apaga (por segundo).")]
    public float fadeSpeed = 3f;

    [Tooltip("Pulso suave mientras está prendido (0 = fijo).")]
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
