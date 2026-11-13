using UnityEngine;

public class FallRespawn : MonoBehaviour
{
    public Transform respawnPoint;

    public int fallDamage = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.IsDead) return;

        health.TakeFallDamage(fallDamage);

        if (health.IsDead) return;

        if (respawnPoint != null)
            health.transform.position = respawnPoint.position;

        Rigidbody2D rb = health.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
