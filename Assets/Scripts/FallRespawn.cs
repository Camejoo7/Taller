using UnityEngine;

public class FallRespawn : MonoBehaviour
{
    public Transform respawnPoint;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.position = respawnPoint.position;
            other.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
    }
}