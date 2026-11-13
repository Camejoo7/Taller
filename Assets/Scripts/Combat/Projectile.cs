using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 9f;

    public float lifeTime = 2f;

    public float radius = 0.04f;

    public int damage = 1;

    public GameObject impactEffect;

    private Vector2 direction = Vector2.right;
    private float lifeTimer;
    private bool launched;

    public void Launch(Vector2 dir)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        lifeTimer = lifeTime;
        launched = true;
    }

    void FixedUpdate()
    {
        if (!launched) return;

        lifeTimer -= Time.fixedDeltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.fixedDeltaTime;
        Collider2D blocker = SweepForHit(step);

        if (blocker != null)
        {
            Impact(blocker);
            return;
        }

        transform.position += (Vector3)(direction * step);
    }

    Collider2D SweepForHit(float step)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, radius, direction, step);

        Collider2D closest = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.CompareTag("Player")) continue;

            if (hit.collider.isTrigger && hit.collider.GetComponentInParent<CodeBlasterTarget>() == null)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closest = hit.collider;
            }
        }

        return closest;
    }

    void Impact(Collider2D other)
    {
        CodeBlasterTarget target = other.GetComponentInParent<CodeBlasterTarget>();
        if (target != null)
            target.Hit();

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
            enemy.TakeDamage(damage);

        BossController boss = other.GetComponentInParent<BossController>();
        if (boss != null)
            boss.TakeShot();

        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
