using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Un cartel de neón que falla: cada tanto el texto parpadea en ráfagas
/// cortas y, a veces, se corre de costado un par de píxeles como una señal
/// con interferencia. Para el título de un juego que se llama CodeBreak, en
/// una ciudad controlada por NEXCORP.
///
/// La clave es que sea RARO. Un parpadeo constante cansa y se lee como bug;
/// uno cada varios segundos se lee como un cartel viejo de verdad.
/// </summary>
public class NeonFlicker : MonoBehaviour
{
    [Tooltip("El texto o imagen que parpadea. Si queda vacío, usa el de este objeto.")]
    public Graphic target;

    [Header("Cada cuánto")]
    public float minWait = 2.5f;
    public float maxWait = 6.5f;

    [Header("La ráfaga")]
    [Tooltip("Cuántos apagones seguidos tiene cada falla.")]
    public int minBlinks = 2;
    public int maxBlinks = 4;

    [Tooltip("Qué tan apagado queda en cada parpadeo (1 = no se apaga).")]
    [Range(0f, 1f)] public float dimTo = 0.3f;

    [Header("Interferencia")]
    [Range(0f, 1f)] public float glitchChance = 0.4f;
    [Tooltip("Cuánto se corre de costado, en unidades del canvas.")]
    public float glitchOffset = 7f;

    private RectTransform rt;
    private Color baseColor;
    private Vector2 basePos;

    void Awake()
    {
        if (target == null) target = GetComponent<Graphic>();
        rt = (RectTransform)transform;
        baseColor = target.color;
        basePos = rt.anchoredPosition;
    }

    void OnEnable() { StartCoroutine(Loop()); }

    void OnDisable()
    {
        // Si se apaga a mitad de una falla, que no quede corrido ni apagado.
        if (target != null) target.color = baseColor;
        if (rt != null) rt.anchoredPosition = basePos;
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(minWait, maxWait));

            int blinks = Random.Range(minBlinks, maxBlinks + 1);
            for (int i = 0; i < blinks; i++)
            {
                bool glitch = Random.value < glitchChance;
                Set(dimTo, glitch ? Random.Range(-1f, 1f) * glitchOffset : 0f);
                yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.08f));

                Set(1f, 0f);
                yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.12f));
            }
        }
    }

    void Set(float brightness, float dx)
    {
        Color c = baseColor;
        c.a = baseColor.a * brightness;
        target.color = c;
        rt.anchoredPosition = basePos + new Vector2(dx, 0f);
    }
}
