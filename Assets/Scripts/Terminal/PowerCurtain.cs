using System.Collections;
using UnityEngine;

/// <summary>
/// El sector que todavía no tiene energía. Tapa con oscuridad todo lo que hay
/// del otro lado de una puerta cerrada, y cuando el jugador resuelve la
/// terminal la luz entra barriendo de izquierda a derecha.
///
/// El punto no es esconder: es que acertar **prenda el sector**. Una puerta que
/// se abre es un obstáculo menos; un pasillo que se enciende delante tuyo es
/// algo que pasó en el mundo porque escribiste bien una línea de Python.
///
/// No usa luces 2D a propósito. Agregar un Light2D a una stage que no tiene
/// ninguna manda toda la escena a negro (ver ROADMAP): esto es un sprite con
/// un degradé, así que no toca la iluminación de nadie.
/// </summary>
public class PowerCurtain : MonoBehaviour
{
    // Relativos a la posición de este objeto, no de mundo: así el prefab
    // Terminal+Puerta se puede soltar en cualquier lado y la oscuridad viaja
    // con la puerta. (Se ignoran la escala y la rotación del objeto.)
    [Header("Qué zona tapa, relativo a este objeto")]
    [Tooltip("Donde arranca la oscuridad. Va un poco a la derecha de la puerta.")]
    public float leftEdge = 0.3f;

    [Tooltip("Hasta dónde llega. Conviene pasarse del borde del nivel.")]
    public float rightEdge = 30f;

    public float bottom = -7f;
    public float top = 4f;

    [Header("Aspecto")]
    [Tooltip("Ancho del degradé del borde izquierdo. Sin esto la oscuridad " +
             "corta en una línea recta y se ve como un rectángulo pegado " +
             "encima, no como falta de luz. Pero si se pasa, el jugador " +
             "igual ve medio pasillo: medio metro alcanza.")]
    public float softEdge = 0.5f;

    [Tooltip("El alfa de acá SE IGNORA y se fuerza a 1 — ver el comentario de " +
             "Build(). La transparencia del borde la hace la textura.")]
    public Color darkness = new Color(0.055f, 0.065f, 0.095f, 1f);

    [Header("Capa de dibujo")]
    [Tooltip("Tiene que tapar los tilemaps (capa Default) y los drones " +
             "(Personaje, orden 1), pero quedar DEBAJO del jugador (orden 10).")]
    public string sortingLayer = "Personaje";

    public int sortingOrder = 8;

    [Header("El encendido")]
    [Tooltip("Lo que tarda la luz en barrer el sector.")]
    public float sweepDuration = 1.2f;

    [Tooltip("Espera antes de arrancar. Sirve para escalonarlo con la puerta " +
             "si se quiere que una cosa pase antes que la otra.")]
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

        // Unlit a propósito: esto es una tapa, no algo del mundo. Si algún día
        // la stage recibe luces 2D, la oscuridad no tiene que iluminarse.
        Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (unlit != null) sr.material = new Material(unlit);

        // ⚠️ El alfa del color va SIEMPRE en 1. Los shaders de sprite de URP
        // trabajan con alfa premultiplicado, así que un color oscuro con alfa
        // apenas por debajo de 1 (0,965 probado) no tapa NADA: se ve igual que
        // si el objeto no existiera. Un rojo saturado con el mismo alfa sí se
        // ve, lo que despista mucho al diagnosticar. El degradé del borde lo
        // hace el canal alfa de la TEXTURA, que sí funciona bien.
        sr.color = new Color(darkness.r, darkness.g, darkness.b, 1f);

        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        spriteSize = sr.sprite.bounds.size;
    }

    /// <summary>
    /// Pone el borde izquierdo de la oscuridad en x, estirándola hasta
    /// rightEdge. Barrer es mover ese borde hacia la derecha.
    /// </summary>
    void Place(float x)
    {
        float width = Mathf.Max(0f, rightEdge - x);
        float height = top - bottom;

        quad.position = transform.position + new Vector3(x, (top + bottom) * 0.5f, 0f);
        quad.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
    }

    /// <summary>Se engancha al onSolved de la terminal desde el Inspector.</summary>
    public void Reveal()
    {
        if (revealed) return;
        revealed = true;
        StartCoroutine(Sweep());
    }

    /// <summary>
    /// Vuelve a dejar el sector a oscuras. Ojo con el nombre: NO se puede
    /// llamar Reset(), porque ese es un callback mágico que el editor dispara
    /// solo al agregar o resetear el componente, y ahí quad todavía es null.
    /// </summary>
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
            // SmoothStep: arranca despacio y frena al final, como una ola de
            // corriente llenando el pasillo en vez de una persiana corrediza.
            Place(Mathf.Lerp(leftEdge, rightEdge, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        quad.gameObject.SetActive(false);
    }

    /// <summary>
    /// Franja horizontal: transparente a la izquierda, opaca del degradé en
    /// adelante. El ancho del degradé en pixeles sale del ancho real que va a
    /// tener en el mundo, así que softEdge se respeta de verdad sin importar
    /// cuánto mida el sector.
    /// </summary>
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

        // Pivote en el borde izquierdo: posicionar = mover ese borde.
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0f, 0.5f), 100f);
    }
}
