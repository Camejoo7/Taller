using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Kira dando vueltas por el menú principal, como una imagen de UI.
///
/// Usa las mismas ideas que KiraFollower para no parecer un sprite pegado:
/// inercia (SmoothDamp), inclinarse hacia donde va y una deriva con dos senos
/// que no repiten el ciclo. Lo que cambia es a dónde va:
///
/// - Entra volando desde afuera de la pantalla al abrir el menú.
/// - Pasea: elige un punto, va, se queda escaneando un rato (la animación
///   "Scan"), y elige otro. Prefiere los costados para no quedarse escondida
///   detrás del cartel.
/// - Cuando cambiás de botón, se acerca a mirarlo un par de segundos.
/// - Al apretar Jugar sale disparada por la derecha, mientras la pantalla se
///   funde a negro (FlyAway, enganchado al onClick del botón).
///
/// Va DETRÁS del cartel y los botones en el orden de dibujo: cuando cruza por
/// el medio pasa por atrás, y eso le da profundidad a la escena.
/// </summary>
[RequireComponent(typeof(Image))]
public class MenuKira : MonoBehaviour
{
    [Header("Animación")]
    [Tooltip("Quieta: la animación Scan del pack de drones.")]
    public Sprite[] idleFrames;
    [Tooltip("En movimiento: Walk_scan.")]
    public Sprite[] moveFrames;
    public float frameTime = 0.09f;
    [Tooltip("A partir de qué velocidad usa los cuadros de movimiento.")]
    public float moveThreshold = 60f;

    [Header("Paseo (unidades del canvas)")]
    [Tooltip("Cuánto se separa de los bordes de la pantalla.")]
    public Vector2 margin = new Vector2(140f, 110f);
    [Tooltip("La franja del medio, donde están el cartel y los botones. " +
             "Ahí casi nunca se para: pasa por atrás, nada más.")]
    public float centerHalfWidth = 430f;
    [Range(0f, 1f)] public float centerChance = 0.15f;
    public Vector2 pauseRange = new Vector2(1.5f, 4f);

    [Header("Movimiento")]
    public float smoothTime = 0.85f;
    public float maxSpeed = 700f;
    public float bankAngle = 14f;
    public float bankAtSpeed = 450f;
    public float driftAmount = 9f;

    [Header("Mirar los botones")]
    [Tooltip("Cuánto se queda al lado del botón que elegiste.")]
    public float visitTime = 2.2f;
    [Tooltip("Separación del borde derecho del botón.")]
    public float visitGap = 110f;

    [Header("Entrada")]
    public float enterDelay = 1.1f;

    private enum State { Waiting, Moving, Scanning, Leaving }

    private RectTransform rt;
    private RectTransform area;
    private Image image;

    private State state = State.Waiting;
    private Vector2 core;          // la posición "real", sin la deriva
    private Vector2 velocity;
    private Vector2 target;
    private float timer;
    private float driftTimer;
    private float frameTimer;
    private int frame;
    private float facing = 1f;
    private float bank;
    private GameObject lastSelected;
    private bool visiting;

    void Awake()
    {
        rt = (RectTransform)transform;
        area = (RectTransform)rt.parent;
        image = GetComponent<Image>();
        image.raycastTarget = false;
    }

    void Start()
    {
        // Arranca afuera, por la izquierda y un poco arriba.
        Vector2 half = area.rect.size * 0.5f;
        core = new Vector2(-half.x - 200f, half.y * 0.35f);
        rt.anchoredPosition = core;
        timer = enterDelay;
        driftTimer = Random.value * 10f;

        if (EventSystem.current != null)
            lastSelected = EventSystem.current.currentSelectedGameObject;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        WatchSelection();
        Think(dt);
        Move(dt);
        Animate(dt);
    }

    // ------------------------------------------------------------ decisiones

    void Think(float dt)
    {
        switch (state)
        {
            case State.Waiting:
                timer -= dt;
                if (timer <= 0f) GoTo(new Vector2(-centerHalfWidth - 150f, area.rect.height * 0.12f));
                break;

            case State.Moving:
                if (Vector2.Distance(core, target) < 12f && velocity.magnitude < 40f)
                {
                    state = State.Scanning;
                    timer = visiting ? visitTime : Random.Range(pauseRange.x, pauseRange.y);
                }
                break;

            case State.Scanning:
                timer -= dt;
                if (timer <= 0f)
                {
                    visiting = false;
                    GoTo(RandomSpot());
                }
                break;
        }
    }

    void WatchSelection()
    {
        if (state == State.Leaving || EventSystem.current == null) return;

        GameObject sel = EventSystem.current.currentSelectedGameObject;
        // Mientras espera para entrar solo toma nota: el botón que marca el
        // menú al abrir no es "cambiaste de botón", y le pisaría la entrada.
        if (state == State.Waiting) { lastSelected = sel; return; }
        if (sel == lastSelected) return;
        lastSelected = sel;
        if (sel == null) return;

        RectTransform button = sel.transform as RectTransform;
        if (button == null) return;

        // Se para a la derecha del botón, a su altura.
        Vector3[] corners = new Vector3[4];
        button.GetWorldCorners(corners);
        Vector2 right = area.InverseTransformPoint((corners[2] + corners[3]) * 0.5f);
        visiting = true;
        GoTo(right + new Vector2(visitGap, 6f));
    }

    void GoTo(Vector2 p)
    {
        target = p;
        state = State.Moving;
    }

    Vector2 RandomSpot()
    {
        Vector2 half = area.rect.size * 0.5f - margin;
        float y = Random.Range(-half.y * 0.2f, half.y);

        float x;
        if (Random.value < centerChance)
            x = Random.Range(-centerHalfWidth, centerHalfWidth);
        else
        {
            // Un costado, y preferentemente el otro del que está ahora: si no,
            // se queda dando vueltas siempre del mismo lado.
            float side = Random.value < 0.7f ? -Mathf.Sign(core.x) : Mathf.Sign(core.x);
            if (side == 0f) side = 1f;
            x = side * Random.Range(centerHalfWidth + 40f, Mathf.Max(centerHalfWidth + 41f, half.x));
        }
        return new Vector2(x, y);
    }

    /// <summary>Se engancha al onClick de Jugar: sale volando por la derecha.</summary>
    public void FlyAway()
    {
        Vector2 half = area.rect.size * 0.5f;
        target = new Vector2(half.x + 300f, core.y + 220f);
        state = State.Leaving;
    }

    // ------------------------------------------------------------ movimiento

    void Move(float dt)
    {
        driftTimer += dt;

        float st = state == State.Leaving ? 0.45f : smoothTime;
        float max = state == State.Leaving ? maxSpeed * 2.5f : maxSpeed;
        if (state != State.Waiting)
            core = Vector2.SmoothDamp(core, target, ref velocity, st, max, dt);

        // Deriva: más fuerte quieta que viajando, como un dron que se
        // estabiliza en el aire.
        float still = 1f - Mathf.Clamp01(velocity.magnitude / maxSpeed);
        Vector2 drift = new Vector2(
            Mathf.Sin(driftTimer * 0.83f) * driftAmount * 0.7f,
            Mathf.Sin(driftTimer * 1.17f + 1.3f) * driftAmount) * (0.4f + 0.6f * still);

        rt.anchoredPosition = core + drift;

        // Mira hacia donde viaja. El umbral evita que se dé vuelta por el
        // temblor del final del SmoothDamp.
        if (Mathf.Abs(velocity.x) > 25f) facing = Mathf.Sign(velocity.x);
        // Quieta al lado de un botón, lo mira a él (está a su derecha).
        if (state == State.Scanning && visiting) facing = -1f;

        float wanted = -Mathf.Clamp(velocity.x / bankAtSpeed, -1f, 1f) * bankAngle;
        bank = Mathf.Lerp(bank, wanted, 1f - Mathf.Exp(-6f * dt));

        // Igual que en KiraFollower: con la escala negativa la rotación se ve
        // al revés, así que se da vuelta para que siga tumbándose hacia donde va.
        Vector3 s = rt.localScale;
        s.x = Mathf.Abs(s.x) * facing;
        rt.localScale = s;
        rt.localRotation = Quaternion.Euler(0f, 0f, facing < 0f ? -bank : bank);
    }

    void Animate(float dt)
    {
        Sprite[] frames = velocity.magnitude > moveThreshold && moveFrames != null && moveFrames.Length > 0
            ? moveFrames : idleFrames;
        if (frames == null || frames.Length == 0) return;

        frameTimer += dt;
        if (frameTimer >= frameTime)
        {
            frameTimer -= frameTime;
            frame++;
        }
        image.sprite = frames[frame % frames.Length];
    }
}
