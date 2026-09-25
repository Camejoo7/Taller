using UnityEngine;

/// <summary>
/// Dron enemigo de NEXCORP. No se queda encima del jugador dándole sin parar:
/// se acerca, embiste de golpe, y si pega (o si falla) se aleja un poco antes
/// de volver a intentarlo. Ese respiro es lo que hace que se pueda esquivar y
/// contraatacar en vez de ser un forcejeo.
///
/// Es un bicho volador, así que no usa gravedad: se mueve a mano con MovePosition.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DroneEnemy : MonoBehaviour
{
    enum State { Idle, Approach, Lunge, Retreat }

    [Header("Distancias")]
    [Tooltip("A partir de acá el dron se despierta y persigue.")]
    public float activationRange = 6f;

    [Tooltip("A partir de acá embiste.")]
    public float lungeRange = 1.6f;

    [Header("Velocidades")]
    public float approachSpeed = 1.4f;
    public float lungeSpeed = 5f;
    public float retreatSpeed = 3f;

    [Header("Tiempos")]
    public float lungeDuration = 0.45f;

    [Tooltip("Cuánto se aleja después de embestir. Es el respiro del jugador " +
             "para disparar o correrse: si se baja mucho, el dron se pega encima " +
             "y no hay forma de reaccionar.")]
    public float retreatDuration = 1.1f;

    [Tooltip("Instante quieto antes de tirarse, que es el aviso para esquivar.")]
    public float pauseBeforeLunge = 0.35f;

    [Header("Daño")]
    public int contactDamage = 1;

    [Header("Movimiento")]
    [Tooltip("Cuánto flota arriba y abajo mientras se acerca.")]
    public float bobAmount = 0.1f;
    public float bobSpeed = 3f;

    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private PlayerHealth playerHealth;

    private State state = State.Idle;
    private float stateUntil;
    private Vector2 lungeDirection;
    private float bobTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();

        // El hijo "Visual" del jugador también tiene el tag Player, así que
        // buscamos el componente y subimos a su GameObject en vez de confiar
        // en lo que devuelva FindWithTag.
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            playerHealth = pm.GetComponent<PlayerHealth>();
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distance = Vector2.Distance(rb.position, player.position);
        FacePlayer();

        switch (state)
        {
            case State.Idle: TickIdle(distance); break;
            case State.Approach: TickApproach(distance); break;
            case State.Lunge: TickLunge(); break;
            case State.Retreat: TickRetreat(); break;
        }

        if (animator != null)
            animator.SetBool("isMoving", state == State.Approach || state == State.Lunge || state == State.Retreat);
    }

    void TickIdle(float distance)
    {
        Bob();

        if (distance <= activationRange)
            state = State.Approach;
    }

    void TickApproach(float distance)
    {
        if (distance > activationRange * 1.3f)
        {
            state = State.Idle;
            return;
        }

        if (distance <= lungeRange)
        {
            // Se queda un instante quieto antes de tirarse: ese aviso es lo que
            // le da al jugador la chance de saltar o correrse.
            lungeDirection = ((Vector2)player.position - rb.position).normalized;
            state = State.Lunge;
            stateUntil = Time.time + lungeDuration + pauseBeforeLunge;
            return;
        }

        Vector2 dir = ((Vector2)player.position - rb.position).normalized;
        Vector2 target = rb.position + dir * approachSpeed * Time.fixedDeltaTime;
        target.y += Mathf.Sin(bobTimer) * bobAmount * Time.fixedDeltaTime * 10f;
        rb.MovePosition(target);

        bobTimer += Time.fixedDeltaTime * bobSpeed;
    }

    void TickLunge()
    {
        // La pausa inicial: todavía no se mueve, solo avisa.
        if (Time.time < stateUntil - lungeDuration) return;

        if (Time.time >= stateUntil)
        {
            StartRetreat();
            return;
        }

        rb.MovePosition(rb.position + lungeDirection * lungeSpeed * Time.fixedDeltaTime);
    }

    void TickRetreat()
    {
        if (Time.time >= stateUntil)
        {
            state = State.Approach;
            return;
        }

        Vector2 away = (rb.position - (Vector2)player.position).normalized;
        if (away.sqrMagnitude < 0.01f) away = Vector2.up;

        rb.MovePosition(rb.position + away * retreatSpeed * Time.fixedDeltaTime);
    }

    void StartRetreat()
    {
        state = State.Retreat;
        stateUntil = Time.time + retreatDuration;
    }

    void Bob()
    {
        bobTimer += Time.fixedDeltaTime * bobSpeed;
        rb.MovePosition(rb.position + new Vector2(0f, Mathf.Sin(bobTimer) * bobAmount * Time.fixedDeltaTime));
    }

    void FacePlayer()
    {
        if (player == null) return;

        float dir = player.position.x > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(dir, 1f, 1f);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        TryHit(col.collider);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        TryHit(col.collider);
    }

    void TryHit(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerHealth == null || playerHealth.IsInvulnerable || playerHealth.IsDead) return;

        playerHealth.TakeDamageFrom(contactDamage, rb.position);
        StartRetreat();
    }
}
