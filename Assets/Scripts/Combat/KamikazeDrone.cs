using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KamikazeDrone : MonoBehaviour
{
    enum State { Waiting, Aiming, Charging, Exploding }

    [Header("Activación")]
    public float activationRange = 6f;

    public float aimDuration = 0.35f;

    [Header("Vuelo")]
    public float speed = 2.5f;

    public bool matchPlayerHeight = true;

    public float maxFlightTime = 10f;

    [Header("Daño")]
    public int contactDamage = 1;

    [Header("Flotación en espera")]
    public float bobAmount = 0.06f;
    public float bobSpeed = 3f;

    [Header("Explosión")]
    public float explosionDuration = 0.22f;

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
    private float lastX;
    private int stuckSteps;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        body = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

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
        lastX = rb.position.x;
        stuckSteps = 0;
    }

    void TickCharging()
    {
        if (Time.time >= flightEndsAt)
        {
            Explode();
            return;
        }

        float moved = Mathf.Abs(rb.position.x - lastX);
        lastX = rb.position.x;
        stuckSteps = moved < speed * Time.fixedDeltaTime * 0.25f ? stuckSteps + 1 : 0;
        if (stuckSteps >= 6)
        {
            Explode();
            return;
        }

        rb.MovePosition(rb.position + new Vector2(direction * speed * Time.fixedDeltaTime, 0f));
    }

    void Face()
    {
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(scale.x) * direction, scale.y, scale.z);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (state != State.Charging) return;

        PlayerHealth health = col.collider.GetComponentInParent<PlayerHealth>();

        if (health != null)
        {
            if (!health.IsInvulnerable && !health.IsDead)
                health.TakeDamageFrom(contactDamage, rb.position);
        }

        Explode();
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (state != State.Charging) return;

        for (int i = 0; i < col.contactCount; i++)
        {
            if (col.GetContact(i).normal.x * direction < -0.5f)
            {
                Explode();
                return;
            }
        }
    }

    void Explode()
    {
        if (state == State.Exploding) return;

        state = State.Exploding;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (body != null) body.enabled = false;

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
