using UnityEngine;

/// <summary>
/// El vacío de abajo del nivel. Caerse ya no es gratis: cuesta un corazón y te
/// devuelve al punto firme más cercano. Si era el último corazón, no lo mueve —
/// de eso se encarga <see cref="PlayerHealth"/>, que reinicia la stage.
/// </summary>
public class FallRespawn : MonoBehaviour
{
    public Transform respawnPoint;

    [Tooltip("Cuántos corazones cuesta caerse al vacío.")]
    public int fallDamage = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        // El hijo "Visual" del jugador también tiene el tag Player, así que
        // buscamos el componente y laburamos sobre el GameObject que de verdad
        // tiene la vida. Moviendo "other" a secas se podía terminar moviendo
        // solo el sprite y dejando el cuerpo atrás.
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.IsDead) return;

        health.TakeFallDamage(fallDamage);

        // Se quedó sin vidas: la stage ya se está reiniciando, moverlo no tiene
        // sentido y además lo haría aparecer un instante antes del corte.
        if (health.IsDead) return;

        if (respawnPoint != null)
            health.transform.position = respawnPoint.position;

        Rigidbody2D rb = health.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
