using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;

    public GameObject deathEffect;

    public int Current { get; private set; }
    public bool IsDead { get { return Current <= 0; } }

    private SpriteRenderer sprite;
    private Color baseColor;
    private Coroutine flashing;

    void Awake()
    {
        Current = maxHealth;
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        Current -= amount;

        if (Current <= 0)
        {
            Die();
            return;
        }

        if (flashing != null) StopCoroutine(flashing);
        flashing = StartCoroutine(Flash());
    }

    IEnumerator Flash()
    {
        if (sprite == null) yield break;

        sprite.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sprite.color = baseColor;
        flashing = null;
    }

    void Die()
    {
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
