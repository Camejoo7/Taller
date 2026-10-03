using UnityEngine;

/// <summary>
/// Al pasar por acá, caerse al vacío te devuelve a este punto en vez de al
/// principio del mapa. Con un Stage 2 largo y con pozos, sin esto una caída en
/// la última zona te mandaba a caminar todo de nuevo.
///
/// Mueve el respawnPoint de todos los FallRespawn de la escena. Es de una
/// sola vez: volver para atrás no te devuelve el checkpoint viejo.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Dónde reaparece. Si queda vacío, en este mismo objeto.")]
    public Transform respawnAt;

    private bool reached = false;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (reached) return;
        // El hijo "Visual" también tiene el tag Player: buscamos la vida, como
        // FallRespawn, para no confundirnos de objeto.
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        reached = true;

        Transform target = respawnAt != null ? respawnAt : transform;
        foreach (FallRespawn f in FindObjectsByType<FallRespawn>(FindObjectsSortMode.None))
            f.respawnPoint = target;
    }
}
