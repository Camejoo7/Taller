using System.Collections;
using UnityEngine;

/// <summary>
/// Un contenedor colgado de la grúa del puerto. Cuando la terminal lo pide
/// (Lower, enganchado al onSolved), tiembla, baja y queda apoyado sobre el
/// agua haciendo de puente.
///
/// Es la consecuencia del ejercicio de listas: el jugador elige con un índice
/// cuál de los contenedores de la fila baja la grúa, y el que baja es el que
/// le deja cruzar.
///
/// Mientras cuelga no es sólido (si no, el jugador podría pararse en un
/// contenedor en el aire); recién al apoyarse se vuelve piso. El cable se
/// estira solo entre el carro de la grúa y el techo del contenedor.
/// </summary>
[ExecuteAlways]
public class CraneLoad : MonoBehaviour
{
    [Tooltip("Cuánto baja, desde donde está colgado.")]
    public Vector2 lowerOffset = new Vector2(0f, -2.16f);

    public float duration = 1.8f;

    [Tooltip("El tironeo antes de que arranquen los motores.")]
    public float shakeDuration = 0.4f;
    public float shakeAmount = 0.025f;

    [Header("Grúa")]
    [Tooltip("El carro de la grúa, de donde cuelga el cable.")]
    public Transform cart;

    [Tooltip("Un sprite fino que se estira de alto: el cable.")]
    public SpriteRenderer cable;

    [Tooltip("Dónde se engancha el cable, relativo al carro (en y).")]
    public float hookOffset = -0.35f;

    private Collider2D solid;
    private SpriteRenderer body;
    private Vector3 hangPos;
    private bool lowered;

    void Awake()
    {
        solid = GetComponent<Collider2D>();
        body = GetComponent<SpriteRenderer>();
        hangPos = transform.position;
        if (Application.isPlaying && solid != null) solid.enabled = false;
    }

    void LateUpdate()
    {
        UpdateCable();
    }

    /// <summary>Estira el cable entre el carro y el contenedor (también en el editor).</summary>
    public void UpdateCable()
    {
        if (cable == null || cart == null || body == null || cable.sprite == null) return;
        float top = cart.position.y + hookOffset;
        float bottom = body.bounds.max.y;
        float len = Mathf.Max(0.01f, top - bottom);
        cable.transform.position = new Vector3(transform.position.x, (top + bottom) * 0.5f, cable.transform.position.z);
        Vector3 s = cable.transform.localScale;
        s.y = len / cable.sprite.bounds.size.y;
        cable.transform.localScale = s;
    }

    /// <summary>Lo que engancha el onSolved de la terminal.</summary>
    public void Lower()
    {
        if (lowered) return;
        lowered = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            transform.position = hangPos + (Vector3)(Random.insideUnitCircle * shakeAmount);
            yield return null;
        }

        Vector3 target = hangPos + (Vector3)lowerOffset;
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            // Arranca y frena despacio: algo de varias toneladas colgando.
            transform.position = Vector3.Lerp(hangPos, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration)));
            yield return null;
        }
        transform.position = target;

        // Ahora sí es piso.
        if (solid != null) solid.enabled = true;

        // Un golpecito al apoyarse en el agua.
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.position = target + Vector3.down * Mathf.Sin(t / 0.25f * Mathf.PI) * 0.05f;
            yield return null;
        }
        transform.position = target;
    }
}
