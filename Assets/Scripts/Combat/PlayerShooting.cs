using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Disparo del jugador: se apunta con el mouse y se dispara con click izquierdo.
/// Solo funciona despues de agarrar el arma en el garage (PlayerMovement.HasWeapon).
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerShooting : MonoBehaviour
{
    [Header("Bala")]
    public GameObject projectilePrefab;

    [Tooltip("Segundos entre disparo y disparo.")]
    public float fireCooldown = 0.28f;

    [Header("De donde sale")]
    [Tooltip("Altura del arma respecto del centro del jugador.")]
    public float gunHeight = 0.02f;

    [Tooltip("Cuanto se adelanta la bala para no nacer dentro del jugador.")]
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

        // Durante un dialogo el jugador esta congelado: tampoco dispara.
        return movement.CanMove;
    }

    /// <summary>
    /// Deja al jugador sin poder disparar un rato. La usa CodeBlasterFight
    /// como penalidad por errarle al fragmento.
    /// </summary>
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

        // Que el personaje mire hacia donde dispara, si no queda disparando de espaldas.
        movement.FaceDirection(dir.x);

        Vector3 spawn = origin + (Vector3)(dir * muzzleDistance);
        GameObject bullet = Instantiate(projectilePrefab, spawn, Quaternion.identity);

        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Launch(dir);

        cooldownTimer = fireCooldown;
    }
}
