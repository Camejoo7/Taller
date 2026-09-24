using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 10f;
    public RuntimeAnimatorController armedController;

    public bool HasWeapon { get; private set; }

    /// <summary>Si el jugador esta suelto (falso mientras habla Kira).</summary>
    public bool CanMove { get { return canMove; } }

    private Rigidbody2D rb;
    private Animator animator;
    private Transform visual;
    private bool isGrounded = false;
    private bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        visual = transform.Find("Visual");
        animator = visual.GetComponent<Animator>();
    }

    void LateUpdate()
    {
        visual.localPosition = Vector3.zero;
    }

    void Update()
    {
        if (!canMove)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isJumping", false);
            return;
        }

        float moveX = Keyboard.current.aKey.isPressed ? -1f :
                      Keyboard.current.dKey.isPressed ? 1f :
                      Keyboard.current.leftArrowKey.isPressed ? -1f :
                      Keyboard.current.rightArrowKey.isPressed ? 1f : 0f;

        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        FaceDirection(moveX);

        animator.SetBool("isWalking", moveX != 0);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetBool("isJumping", true);
        }

        if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
            animator.SetBool("isJumping", false);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        foreach (ContactPoint2D contact in col.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    void OnCollisionStay2D(Collision2D col)
    {
        foreach (ContactPoint2D contact in col.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
        isGrounded = false;
    }

    void OnCollisionExit2D(Collision2D col)
    {
        isGrounded = false;
    }

    /// <summary>
    /// Da vuelta el sprite hacia un lado. Ojo que la escala va invertida:
    /// mirando a la derecha es -2 y a la izquierda +2.
    /// </summary>
    public void FaceDirection(float dirX)
    {
        if (visual == null || Mathf.Abs(dirX) < 0.01f) return;

        visual.localScale = dirX > 0f ? new Vector3(-2, 2, 1) : new Vector3(2, 2, 1);
    }

    public void EquipWeapon()
    {
        if (HasWeapon) return;
        HasWeapon = true;
        if (armedController != null)
            animator.runtimeAnimatorController = armedController;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!value)
            rb.linearVelocity = Vector2.zero;
    }
}