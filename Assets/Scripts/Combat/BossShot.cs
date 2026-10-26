using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lo que tira el jefe: una bola que va derecho o una bomba que cae.
///
/// Se mueve a mano con un CircleCast, igual que la bala del jugador
/// (<see cref="Projectile"/>): a la escala de este juego, algo rápido con un
/// collider común atraviesa al jugador entre un cuadro y el siguiente.
/// Le pega al jugador y se apaga; contra el piso o una pared, explota.
/// </summary>
public class BossShot : MonoBehaviour
{
    [Tooltip("Gravedad propia. 0 = va derecho (la bola). Más de 0 = cae (la bomba).")]
    public float gravity = 0f;

    [Tooltip("Grosor para detectar el golpe.")]
    public float radius = 0.08f;

    [Tooltip("Al chocar, también lastima a lo que esté a esta distancia: la " +
             "explosión de la bomba. 0 = sin explosión.")]
    public float blastRadius = 0f;

    public int damage = 1;

    [Tooltip("Segundos antes de desaparecer sola si no choca con nada.")]
    public float lifeTime = 6f;

    [Tooltip("Gira el dibujo hacia donde va. La bomba arranca acostada y cae " +
             "de punta.")]
    public bool faceVelocity = false;

    [Tooltip("El fogonazo del impacto. Vacío = usa el mismo dibujo.")]
    public Sprite blastSprite;

    // Todas las que están en el aire: el jefe las borra de golpe cuando queda
    // aturdido, así no te pegan mientras vas caminando a la consola.
    static readonly List<BossShot> live = new List<BossShot>();

    private Vector2 velocity;
    private float life;
    private bool done;
    private SpriteRenderer sr;

    public static void ClearAll()
    {
        for (int i = live.Count - 1; i >= 0; i--)
            if (live[i] != null) Destroy(live[i].gameObject);
        live.Clear();
    }

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        live.Add(this);
    }

    void OnDisable()
    {
        live.Remove(this);
    }

    public void Launch(Vector2 startVelocity)
    {
        velocity = startVelocity;
        life = lifeTime;
        Face();
    }

    void FixedUpdate()
    {
        if (done) return;

        life -= Time.fixedDeltaTime;
        if (life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y -= gravity * Time.fixedDeltaTime;
        Vector2 step = velocity * Time.fixedDeltaTime;
        float distance = step.magnitude;

        if (distance > 0f)
        {
            // Los resultados vienen ordenados del más cercano al más lejano.
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(transform.position, radius, step / distance, distance))
            {
                Collider2D other = hit.collider;
                if (other == null || other.isTrigger) continue;
                if (other.GetComponentInParent<BossController>() != null) continue;

                PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
                if (player != null && !player.IsDead)
                    player.TakeDamageFrom(damage, hit.point);

                Explode(hit.point);
                return;
            }
        }

        transform.position += (Vector3)step;
        Face();
    }

    void Face()
    {
        if (!faceVelocity || velocity.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Explode(Vector2 point)
    {
        done = true;
        transform.position = point;

        // La bomba lastima alrededor: si cae al lado tuyo, también cuenta.
        if (blastRadius > 0f)
        {
            foreach (Collider2D c in Physics2D.OverlapCircleAll(point, blastRadius))
            {
                PlayerHealth player = c.GetComponentInParent<PlayerHealth>();
                if (player != null && !player.IsDead)
                    player.TakeDamageFrom(damage, point);
            }
        }

        StartCoroutine(Blast());
    }

    /// <summary>El fogonazo: crece y se apaga en un cuarto de segundo.</summary>
    IEnumerator Blast()
    {
        if (sr == null)
        {
            Destroy(gameObject);
            yield break;
        }

        if (blastSprite != null) sr.sprite = blastSprite;
        transform.rotation = Quaternion.identity;

        Vector3 from = transform.localScale;
        float grow = blastRadius > 0f ? 3f : 2f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.25f;
            transform.localScale = from * Mathf.Lerp(1f, grow, t);
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
