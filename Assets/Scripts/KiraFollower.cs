using UnityEngine;

public class KiraFollower : MonoBehaviour
{
    [Header("Dónde se para")]
    public Vector2 offset = new Vector2(-1f, 1f);

    public float followSpeed = 8f;

    public bool followFromStart = false;

    [Header("Inercia")]
    public float smoothTime = 0.38f;

    [Header("Cambio de lado")]
    public float sideSwitchSpeed = 2.2f;

    public float crossLift = 0.35f;

    [Header("Inclinación")]
    public float bankAngle = 16f;

    public float bankAtSpeed = 4f;

    [Header("Deriva")]
    public float driftAmount = 0.1f;

    private Transform target;
    private bool following;
    private Animator animator;

    private Vector3 velocity;
    private float driftTimer;
    private float facing = 1f;
    private float side = -1f;
    private float lastTargetX;
    private float bank;

    void Start()
    {
        animator = GetComponent<Animator>();

        if (followFromStart && target == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                StartFollowing(player.transform);
        }
    }

    public void StartFollowing(Transform player)
    {
        target = player;
        following = true;
        lastTargetX = player.position.x;
    }

    void Update()
    {
        if (!following || target == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        driftTimer += dt;

        float dx = target.position.x - lastTargetX;
        lastTargetX = target.position.x;

        if (Mathf.Abs(dx) > 0.001f) facing = Mathf.Sign(dx);

        side = Mathf.MoveTowards(side, -facing, sideSwitchSpeed * dt);

        float lift = crossLift * (1f - Mathf.Abs(side));

        float driftX = Mathf.Sin(driftTimer * 0.83f) * driftAmount * 0.7f;
        float driftY = Mathf.Sin(driftTimer * 1.17f + 1.3f) * driftAmount;

        Vector3 desired = new Vector3(
            target.position.x + side * Mathf.Abs(offset.x) + driftX,
            target.position.y + offset.y + lift + driftY,
            transform.position.z);

        Vector3 pos = Vector3.SmoothDamp(transform.position, desired,
                                         ref velocity, smoothTime, followSpeed, dt);
        pos.z = transform.position.z;
        transform.position = pos;

        float scaleX = facing > 0f ? 1f : -1f;

        float wanted = -Mathf.Clamp(velocity.x / bankAtSpeed, -1f, 1f) * bankAngle;
        bank = Mathf.Lerp(bank, wanted, 1f - Mathf.Exp(-6f * dt));

        transform.localScale = new Vector3(scaleX, 1f, 1f);
        transform.rotation = Quaternion.Euler(0f, 0f, scaleX < 0f ? -bank : bank);

        if (animator != null)
            animator.SetBool("isMoving", velocity.sqrMagnitude > 0.04f);
    }
}
