using UnityEngine;

/// <summary>
/// La bala que dispara el jugador. Se mueve a mano con un CircleCast en vez de
/// usar la fisica: a la escala de este juego (el jugador mide 0.38 unidades) una
/// bala rapida con collider normal se "teletransporta" entre frames y atraviesa
/// los blancos sin tocarlos. El CircleCast barre todo el tramo recorrido, asi que
/// no se escapa ningun impacto por mas rapida que vaya.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("Unidades por segundo.")]
    public float speed = 9f;

    [Tooltip("Segundos antes de desaparecer sola si no le pega a nada.")]
    public float lifeTime = 2f;

    [Tooltip("Grosor de la bala para detectar impactos.")]
    public float radius = 0.04f;

    [Tooltip("Opcional: efecto que aparece donde impacta.")]
    public GameObject impactEffect;

    private Vector2 direction = Vector2.right;
    private float lifeTimer;
    private bool launched;

    /// <summary>Lanza la bala. La llama PlayerShooting apenas la instancia.</summary>
    public void Launch(Vector2 dir)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        // El sprite de la bala apunta a +X, asi que la rotamos hacia donde va.
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

    /// <summary>
    /// Barre el tramo que la bala esta por recorrer y devuelve contra que choca,
    /// ignorando al propio jugador (si no, la bala moriria al salir del arma).
    /// </summary>
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

            // Las zonas invisibles del nivel (la que despierta a Kira, la del
            // respawn, los spawners) son triggers y estan por todos lados: si la
            // bala muriera contra ellas se apagaria en el aire sin motivo visible.
            // Los unicos triggers que frenan la bala son los fragmentos de codigo.
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
        // El collider puede estar en un hijo del fragmento, por eso InParent.
        CodeBlasterTarget target = other.GetComponentInParent<CodeBlasterTarget>();
        if (target != null)
            target.Hit();

        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
