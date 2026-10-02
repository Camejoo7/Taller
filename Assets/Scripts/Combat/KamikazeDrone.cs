using System.Collections;
using UnityEngine;

/// <summary>
/// El dron del Stage 1. No persigue ni forcejea como el <see cref="DroneEnemy"/>
/// del Stage 2: espera flotando en su lugar, y cuando el jugador se le acerca se
/// pone a su altura y sale derecho para ese lado, siempre a la misma velocidad,
/// hasta que choca contra algo.
///
/// Si choca contra el jugador, explota y le hace daño. Si choca contra una pared,
/// explota igual. En los dos casos desaparece — no hay un segundo intento.
///
/// Esa previsibilidad es a propósito: el jugador todavía no tiene arma en esta
/// stage, así que el dron tiene que ser un obstáculo que se esquiva saltando, no
/// un enemigo que se pelea. El instante de apuntado antes de salir es el aviso
/// que hace que esquivarlo sea una reacción y no una lotería.
///
/// Espera al jugador en vez de salir disparado apenas aparece porque el trigger
/// que lo crea puede estar lejos del corredor donde está parado: si arrancara
/// solo, se estrellaría contra la pared antes de que el jugador llegue a verlo.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class KamikazeDrone : MonoBehaviour
{
    enum State { Waiting, Aiming, Charging, Exploding }

    [Header("Activación")]
    [Tooltip("A qué distancia del jugador se despierta y sale. Hasta entonces " +
             "flota quieto en su lugar.")]
    public float activationRange = 6f;

    [Tooltip("Instante que tarda en acomodarse a la altura del jugador antes de " +
             "salir. Es el aviso para esquivarlo: si se baja a cero, no hay forma " +
             "de reaccionar.")]
    public float aimDuration = 0.35f;

    [Header("Vuelo")]
    [Tooltip("Velocidad constante del embate, en unidades por segundo.")]
    public float speed = 2.5f;

    [Tooltip("Si está marcado, al salir se acomoda a la altura del cuerpo del " +
             "jugador. Si no, vuela a la altura en la que lo dejaste en la escena.")]
    public bool matchPlayerHeight = true;

    [Tooltip("Red de seguridad por si sale volando hacia un tramo sin paredes: a " +
             "los tantos segundos de arrancar explota solo.")]
    public float maxFlightTime = 10f;

    [Header("Daño")]
    public int contactDamage = 1;

    [Header("Flotación en espera")]
    public float bobAmount = 0.06f;
    public float bobSpeed = 3f;

    [Header("Explosión")]
    [Tooltip("Cuánto dura el estallido antes de que el dron se borre.")]
    public float explosionDuration = 0.22f;

    [Tooltip("Cuánto se agranda el sprite mientras estalla.")]
    public float explosionScale = 2.2f;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator animator;
    private Collider2D body;

    private Transform player;
    private Collider2D playerBody;

    private State state = State.Waiting;
    private float direction = -1f;
    private float restY;
    private float targetY;
    private float aimStartY;
    private float aimElapsed;
    private float flightEndsAt;
    private float bobTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        body = GetComponent<Collider2D>();

        // Es un bicho volador y va en línea recta: nada de gravedad ni de girar
        // al chocar.
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // El hijo "Visual" del jugador también tiene el tag Player, así que
        // buscamos el componente y subimos a su GameObject en vez de confiar en
        // lo que devuelva FindWithTag.
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            playerBody = pm.GetComponent<Collider2D>();
        }

        restY = rb.position.y;
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Waiting: TickWaiting(); break;
            case State.Aiming: TickAiming(); break;
            case State.Charging: TickCharging(); break;
        }
    }

    void TickWaiting()
    {
        // Flota en el lugar para que no parezca colgado. El seno va sobre una
        // altura fija guardada al nacer, no sobre la posición actual: sumando
        // sobre sí mismo el dron se iría yendo para arriba de a poco.
        bobTimer += Time.fixedDeltaTime * bobSpeed;
        rb.MovePosition(new Vector2(rb.position.x, restY + Mathf.Sin(bobTimer) * bobAmount));

        if (player == null) return;
        if (Vector2.Distance(rb.position, player.position) > activationRange) return;

        StartAiming();
    }

    void StartAiming()
    {
        float delta = player.position.x - rb.position.x;
        if (Mathf.Abs(delta) > 0.01f) direction = Mathf.Sign(delta);
        Face();

        aimStartY = rb.position.y;
        aimElapsed = 0f;

        // La altura del cuerpo, no la del transform: el pivote del jugador está
        // en los pies y apuntarle ahí lo haría pasar por abajo.
        targetY = aimStartY;
        if (matchPlayerHeight)
            targetY = playerBody != null ? playerBody.bounds.center.y : player.position.y;

        state = State.Aiming;
    }

    void TickAiming()
    {
        aimElapsed += Time.fixedDeltaTime;

        float t = aimDuration <= 0f ? 1f : Mathf.Clamp01(aimElapsed / aimDuration);
        rb.MovePosition(new Vector2(rb.position.x, Mathf.SmoothStep(aimStartY, targetY, t)));

        if (t < 1f) return;

        state = State.Charging;
        flightEndsAt = Time.time + maxFlightTime;
    }

    void TickCharging()
    {
        if (Time.time >= flightEndsAt)
        {
            Explode();
            return;
        }

        rb.MovePosition(rb.position + new Vector2(direction * speed * Time.fixedDeltaTime, 0f));
    }

    void Face()
    {
        // El sprite del dron mira a la derecha en su escala positiva.
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(scale.x) * direction, scale.y, scale.z);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Mientras espera o apunta no se mata contra el piso ni contra una pared
        // pegada: recién cuenta cuando ya salió disparado.
        if (state != State.Charging) return;

        PlayerHealth health = col.collider.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            // Explota igual aunque el jugador esté en los segundos de gracia: se
            // estrelló contra él, solo que esta vez no le duele.
            if (!health.IsInvulnerable && !health.IsDead)
                health.TakeDamageFrom(contactDamage, rb.position);
        }

        Explode();
    }

    void Explode()
    {
        if (state == State.Exploding) return;

        state = State.Exploding;

        // Se frena y se vuelve intangible para que el estallido no siga
        // empujando ni pegando mientras se apaga.
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (body != null) body.enabled = false;

        // El Animator maneja el color y la escala del dron vivo: si sigue
        // corriendo, pisa el flash frame a frame.
        if (animator != null) animator.enabled = false;

        StartCoroutine(ExplosionFlash());
    }

    IEnumerator ExplosionFlash()
    {
        if (sprite == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector3 fromScale = transform.localScale;
        Vector3 toScale = fromScale * explosionScale;
        float elapsed = 0f;

        while (elapsed < explosionDuration)
        {
            float t = elapsed / explosionDuration;

            transform.localScale = Vector3.Lerp(fromScale, toScale, t);
            sprite.color = new Color(1f, 1f, 1f, 1f - t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
