using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    private bool picked = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (picked || !other.CompareTag("Player")) return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        picked = true;
        player.EquipWeapon();
        gameObject.SetActive(false);
    }
}
