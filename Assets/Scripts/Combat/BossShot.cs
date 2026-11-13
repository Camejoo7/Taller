using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossShot : MonoBehaviour
{
    public float gravity = 0f;

    public float radius = 0.08f;

    public float blastRadius = 0f;

    public int damage = 1;

    public float lifeTime = 6f;

    public bool faceVelocity = false;

    public Sprite blastSprite;

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
