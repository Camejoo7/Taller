using System.Collections;
using UnityEngine;

/// <summary>
/// Un nodo de la red: una bobina que arranca apagada (oscura y quieta) y se
/// enciende chispeando cuando la terminal lo reinicia. Se engancha al onSolved
/// de la terminal, uno por nodo.
///
/// Es la consecuencia física del ejercicio de bucles: el for "reinicia" N
/// nodos y el jugador VE los N nodos que acaba de cruzar prendiéndose uno
/// atrás del otro.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PowerNode : MonoBehaviour
{
    [Tooltip("El color mientras está apagado. Se multiplica con el sprite.")]
    public Color offColor = new Color(0.35f, 0.35f, 0.45f, 1f);

    [Tooltip("Espera antes de prenderse. Escalonando los nodos se ve el bucle " +
             "pasando de uno en uno.")]
    public float delay = 0f;

    [Tooltip("El fogonazo al prenderse.")]
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

        // Fogonazo blanco que baja al color normal.
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
