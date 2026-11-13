using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DroneEnemy : MonoBehaviour
{
    enum State { Idle, Approach, Lunge, Retreat }

    [Header("Distancias")]
    public float activationRange = 6f;

    public float lungeRange = 1.6f;

    [Header("Velocidades")]
    public float approachSpeed = 1.3f;

    public float lungeSpeed = 2.5f;

    public float retreatSpeed = 3f;

    [Header("Tiempos")]
    public float lungeDuration = 0.55f;

    public float retreatDuration = 1.3f;

    public float pauseBeforeLunge = 0.6f;

    public Color telegraphColor = new Color(1f, 0.35f, 0.3f, 1f);

    [Header("Daño")]
    public int contactDamage = 1;

    [Header("Movimiento")]
    public float bobAmount = 0.1f;
    public float bobSpeed = 3f;

    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private SpriteRenderer sprite;
    private Color baseColor = Color.white;
    private bool wasTelegraphing;

    private State state = State.Idle;
    private float stateUntil;
    private Vector2 lungeDirection;
    private float bobTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;

        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            playerHealth = pm.GetComponent<PlayerHealth>();
            playerMovement = pm;
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distance = Vector2.Distance(rb.position, player.position);
        FacePlayer();

        if (playerMovement != null && !playerMovement.CanMove)
        {
            if (state == State.Approach || state == State.Lunge) StartRetreat();
            if (state == State.Retreat && Time.time < stateUntil) TickRetreat();
            else { state = State.Idle; Bob(); }

            if (animator != null) animator.SetBool("isMoving", state == State.Retreat);
            Telegraph();
            return;
        }

        switch (state)
        {
            case State.Idle: TickIdle(distance); break;
            case State.Approach: TickApproach(distance); break;
            case State.Lunge: TickLunge(); break;
            case State.Retreat: TickRetreat(); break;
        }

        if (animator != null)
            animator.SetBool("isMoving", state == State.Approach || state == State.Lunge || state == State.Retreat);

        Telegraph();
    }

    void Telegraph()
    {
        if (sprite == null) return;

        bool avisando = state == State.Lunge && Time.time < stateUntil - lungeDuration;

        if (avisando)
            sprite.color = Color.Lerp(baseColor, telegraphColor,
                                      Mathf.PingPong(Time.time * 9f, 1f));
        else if (wasTelegraphing)
            sprite.color = baseColor;

        wasTelegraphing = avisando;
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
        if (playerMovement != null && !playerMovement.CanMove) return;

        playerHealth.TakeDamageFrom(contactDamage, rb.position);
        StartRetreat();
    }
}
