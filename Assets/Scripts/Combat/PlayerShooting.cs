using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerShooting : MonoBehaviour
{
    [Header("Bala")]
    public GameObject projectilePrefab;

    public float fireCooldown = 0.28f;

    [Header("De donde sale")]
    public float gunHeight = 0.02f;

    public float muzzleDistance = 0.22f;

    private PlayerMovement movement;
    private Camera cam;
    private float cooldownTimer;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        cam = Camera.main;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (!CanShootNow()) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Shoot();
    }

    bool CanShootNow()
    {
        if (projectilePrefab == null) return false;
        if (cooldownTimer > 0f) return false;
        if (movement == null || !movement.HasWeapon) return false;

        return movement.CanMove;
    }

    public void BlockFor(float seconds)
    {
        cooldownTimer = Mathf.Max(cooldownTimer, seconds);
    }

    void Shoot()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 origin = transform.position + new Vector3(0f, gunHeight, 0f);

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorld.z = 0f;

        Vector2 dir = mouseWorld - origin;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        movement.FaceDirection(dir.x);

        Vector3 spawn = origin + (Vector3)(dir * muzzleDistance);
        GameObject bullet = Instantiate(projectilePrefab, spawn, Quaternion.identity);

        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Launch(dir);

        cooldownTimer = fireCooldown;
    }
}
