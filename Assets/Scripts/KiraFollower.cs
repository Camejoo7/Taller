using UnityEngine;

public class KiraFollower : MonoBehaviour
{
    public float followSpeed = 3f;
    public Vector2 offset = new Vector2(-2f, 1.5f);

    private Transform target;
    private bool following = false;
    private float floatTimer = 0f;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void StartFollowing(Transform player)
    {
        target = player;
        following = true;
    }

    void Update()
    {
        if (!following || target == null) return;

        floatTimer += Time.deltaTime;

        Vector3 targetPos = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y + Mathf.Sin(floatTimer * 1.5f) * 0.15f,
            transform.position.z
        );

        float distance = Vector3.Distance(transform.position, targetPos);
        bool isMoving = distance > 0.1f;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            followSpeed * Time.deltaTime
        );

        if (animator != null)
            animator.SetBool("isMoving", isMoving);

        if (target.position.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }
}