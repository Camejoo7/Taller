using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    public Transform respawnAt;

    private bool reached = false;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (reached) return;

        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        reached = true;

        Transform target = respawnAt != null ? respawnAt : transform;
        foreach (FallRespawn f in FindObjectsByType<FallRespawn>(FindObjectsSortMode.None))
            f.respawnPoint = target;
    }
}
