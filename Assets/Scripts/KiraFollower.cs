using UnityEngine;

/// <summary>
/// Kira volando atrás del jugador.
///
/// La versión vieja la clavaba en un offset fijo con MoveTowards: como el
/// offset nunca cambiaba y su velocidad era la misma que la del jugador, Kira
/// quedaba siempre en el mismo punto exacto de la pantalla — "un gif pegado
/// ahí", en palabras de Kevin. Y como el offset la ponía siempre a la
/// izquierda, el flip de mirada nunca llegaba a dispararse: miraba a la
/// derecha toda la partida.
///
/// Lo que la hace parecer viva no es moverse más, es **llegar tarde**. Acá eso
/// se consigue con cuatro cosas: inercia (SmoothDamp, no MoveTowards), que se
/// pase de lado cuando el jugador se da vuelta, que se incline hacia donde va,
/// y una deriva que nunca repite el mismo ciclo.
/// </summary>
public class KiraFollower : MonoBehaviour
{
    [Header("Dónde se para")]
    [Tooltip("x = a qué distancia va por detrás (el signo no importa: el lado " +
             "lo elige sola según para dónde mire el jugador). y = qué tan " +
             "arriba de él.")]
    public Vector2 offset = new Vector2(-1f, 1f);

    [Tooltip("Techo de velocidad cuando viene corriendo a alcanzarlo. Tiene " +
             "que ser MAYOR que la del jugador (3) o nunca lo alcanza.")]
    public float followSpeed = 8f;

    [Tooltip("Que empiece a seguir apenas arranca la escena. En el Stage 1 va en " +
             "falso, porque Kira recién aparece durante el diálogo de intro; de la " +
             "Stage 2 en adelante ya viene con el jugador, así que va en verdadero.")]
    public bool followFromStart = false;

    [Header("Inercia")]
    [Tooltip("Cuánto tarda en acomodarse. Más alto = más perezosa y más se " +
             "queda atrás. Es lo que más cambia la sensación: en 0 vuelve a " +
             "ser un sprite clavado al jugador.")]
    public float smoothTime = 0.38f;

    [Header("Cambio de lado")]
    [Tooltip("Qué tan rápido cruza al otro lado cuando el jugador se da " +
             "vuelta. Bajo = cruza con pachorra y se nota lindo.")]
    public float sideSwitchSpeed = 2.2f;

    [Tooltip("Cuánto sube mientras cruza. Sin esto pasa atravesando al " +
             "jugador; con esto le pasa por arriba haciendo un arco.")]
    public float crossLift = 0.35f;

    [Header("Inclinación")]
    [Tooltip("Cuánto se tumba hacia donde va, en grados. Un dron que nunca " +
             "se inclina se lee como una calcomanía.")]
    public float bankAngle = 16f;

    [Tooltip("A qué velocidad propia llega a la inclinación máxima.")]
    public float bankAtSpeed = 4f;

    [Header("Deriva")]
    [Tooltip("Cuánto flota sola cuando no va a ningún lado.")]
    public float driftAmount = 0.1f;

    private Transform target;
    private bool following;
    private Animator animator;

    private Vector3 velocity;      // la usa SmoothDamp
    private float driftTimer;
    private float facing = 1f;     // para dónde mira el jugador
    private float side = -1f;      // -1 = Kira a la izquierda, +1 = a la derecha
    private float lastTargetX;
    private float bank;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (followFromStart && target == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                StartFollowing(player.transform);
        }
    }

    public void StartFollowing(Transform player)
    {
        target = player;
        following = true;
        lastTargetX = player.position.x;
    }

    void Update()
    {
        if (!following || target == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        driftTimer += dt;

        // --- para dónde va el jugador
        float dx = target.position.x - lastTargetX;
        lastTargetX = target.position.x;
        // El umbral evita que el temblor del solver de física la haga dudar
        // cuando el jugador está parado.
        if (Mathf.Abs(dx) > 0.001f) facing = Mathf.Sign(dx);

        // --- de qué lado se pone: siempre del contrario al que mira
        side = Mathf.MoveTowards(side, -facing, sideSwitchSpeed * dt);

        // Mientras cruza (side cerca de 0) se levanta, así pasa por arriba del
        // jugador en vez de atravesarlo.
        float lift = crossLift * (1f - Mathf.Abs(side));

        // --- deriva: dos senos de frecuencias que no son múltiplos entre sí,
        // así el ciclo tarda muchísimo en repetirse y no se lee como un loop.
        float driftX = Mathf.Sin(driftTimer * 0.83f) * driftAmount * 0.7f;
        float driftY = Mathf.Sin(driftTimer * 1.17f + 1.3f) * driftAmount;

        Vector3 desired = new Vector3(
            target.position.x + side * Mathf.Abs(offset.x) + driftX,
            target.position.y + offset.y + lift + driftY,
            transform.position.z);

        Vector3 pos = Vector3.SmoothDamp(transform.position, desired,
                                         ref velocity, smoothTime, followSpeed, dt);
        pos.z = transform.position.z;
        transform.position = pos;

        // --- mira para el mismo lado que el jugador
        float scaleX = facing > 0f ? 1f : -1f;

        // --- se tumba hacia donde va
        float wanted = -Mathf.Clamp(velocity.x / bankAtSpeed, -1f, 1f) * bankAngle;
        bank = Mathf.Lerp(bank, wanted, 1f - Mathf.Exp(-6f * dt));

        // Al espejar con escala negativa, una rotación se ve al revés: hay que
        // darla vuelta para que siga tumbándose hacia donde viaja.
        transform.localScale = new Vector3(scaleX, 1f, 1f);
        transform.rotation = Quaternion.Euler(0f, 0f, scaleX < 0f ? -bank : bank);

        if (animator != null)
            animator.SetBool("isMoving", velocity.sqrMagnitude > 0.04f);
    }
}
