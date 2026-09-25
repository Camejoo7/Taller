using System.Collections;
using UnityEngine;

/// <summary>
/// Vida de un enemigo. Sin esto las balas le pasaban por encima sin hacer nada.
/// Parpadea en blanco al recibir un tiro para que se note que le pegaste, que
/// es lo que hace que disparar se sienta bien.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 3;

    [Tooltip("Opcional: algo que aparezca al morir.")]
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
