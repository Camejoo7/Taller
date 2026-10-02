using System.Collections;
using UnityEngine;

/// <summary>
/// Una puerta blindada que solo se abre cuando algo del nivel la alimenta —
/// normalmente una <see cref="CodeTerminal"/> resuelta.
///
/// Existe para que acertar tenga consecuencia física. Un cartel de "¡Correcto!"
/// se puede hacer en cualquier página; una puerta de media tonelada que
/// tiembla, rechina y se mete en el techo mientras el jugador la mira, no.
/// </summary>
public class PoweredDoor : MonoBehaviour
{
    [Header("Apertura")]
    [Tooltip("Los frames del postigo abriéndose, del cerrado al abierto. Si se " +
             "cargan, la puerta se abre animando el dibujo. Es lo que corresponde " +
             "cuando no hay techo donde meterla.")]
    public Sprite[] openFrames;

    [Tooltip("Cuánto se corre al abrirse, relativo a donde está ahora. Dejar en " +
             "cero si se usan los frames.")]
    public Vector2 openOffset = new Vector2(0f, 0.72f);

    public float openDuration = 1.1f;

    [Tooltip("Tiembla un momento antes de arrancar, como si forzara los motores. " +
             "Sin esto la puerta se desliza sola y parece una animación de CSS.")]
    public float shakeDuration = 0.45f;

    public float shakeAmount = 0.02f;

    [Header("Luces")]
    [Tooltip("Opcional: sprites que cambian de color cuando la puerta se habilita.")]
    public SpriteRenderer[] indicators;

    public Color lockedColor = new Color(1f, 0.25f, 0.25f, 1f);
    public Color unlockedColor = new Color(0.2f, 1f, 0.55f, 1f);

    [Tooltip("Apagarlas cuando termina de abrirse. Hace falta si la puerta se " +
             "retrae del todo: si no, las luces quedan flotando en el aire " +
             "sobre una puerta que ya no está.")]
    public bool hideIndicatorsWhenOpen = true;

    private Collider2D blocker;
    private SpriteRenderer sprite;
    private Sprite closedSprite;
    private Vector3 closedPos;
    private bool opening;

    void Awake()
    {
        blocker = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) closedSprite = sprite.sprite;
        closedPos = transform.position;
        Tint(lockedColor);
    }

    /// <summary>Lo que engancha el <c>onSolved</c> de la terminal.</summary>
    public void Open()
    {
        if (opening) return;
        opening = true;
        StartCoroutine(OpenRoutine());
    }

    public void CloseInstantly()
    {
        StopAllCoroutines();
        opening = false;
        transform.position = closedPos;
        if (sprite != null && closedSprite != null) sprite.sprite = closedSprite;
        if (blocker != null) blocker.enabled = true;
        if (indicators != null)
            for (int i = 0; i < indicators.Length; i++)
                if (indicators[i] != null) indicators[i].enabled = true;
        Tint(lockedColor);
    }

    IEnumerator OpenRoutine()
    {
        Tint(unlockedColor);

        // Forcejeo: los motores arrancan antes de que la chapa ceda.
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            transform.position = closedPos + (Vector3)(Random.insideUnitCircle * shakeAmount);
            yield return null;
        }

        Vector3 target = closedPos + (Vector3)openOffset;
        bool animar = openFrames != null && openFrames.Length > 0;
        t = 0f;

        while (t < openDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / openDuration);

            // SmoothStep: arranca pesada y frena al final, que es como se mueve
            // algo con masa. Lineal parecería un panel deslizándose.
            transform.position = Vector3.Lerp(closedPos, target, Mathf.SmoothStep(0f, 1f, p));

            if (animar && sprite != null)
            {
                int i = Mathf.Min(openFrames.Length - 1, Mathf.FloorToInt(p * openFrames.Length));
                if (openFrames[i] != null) sprite.sprite = openFrames[i];
            }

            yield return null;
        }

        transform.position = target;
        if (animar && sprite != null && openFrames[openFrames.Length - 1] != null)
            sprite.sprite = openFrames[openFrames.Length - 1];

        // Recién acá deja pasar: si se desactiva al principio, el jugador puede
        // cruzar a través de la puerta mientras todavía se está abriendo.
        if (blocker != null) blocker.enabled = false;

        if (hideIndicatorsWhenOpen && indicators != null)
            for (int i = 0; i < indicators.Length; i++)
                if (indicators[i] != null) indicators[i].enabled = false;
    }

    void Tint(Color c)
    {
        if (indicators == null) return;
        for (int i = 0; i < indicators.Length; i++)
            if (indicators[i] != null) indicators[i].color = c;
    }
}
