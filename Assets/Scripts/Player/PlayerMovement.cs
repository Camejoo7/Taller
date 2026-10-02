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

    /// <summary>Si esta pisando piso. Lo usa PlayerHealth para saber donde es seguro reaparecer.</summary>
    public bool IsGrounded { get { return isGrounded; } }

    private Rigidbody2D rb;
    private Animator animator;
    private Transform visual;
    private bool isGrounded = false;
    private bool canMove = true;

    /// <summary>
    /// Se va llenando con los contactos de TODOS los colliders del paso de
    /// fisica y recien ahi se publica en isGrounded. Antes cada callback
    /// pisaba el resultado del anterior: si el jugador tocaba el piso y una
    /// pared (o un dron) al mismo tiempo, el callback de la pared decia "no
    /// hay piso" y borraba el del piso. Por eso no saltaba contra una pared.
    /// </summary>
    private bool groundedThisStep;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        visual = transform.Find("Visual");
        animator = visual.GetComponent<Animator>();

        // Un Rigidbody2D quieto se duerme, y dormido DEJA DE TIRAR
        // OnCollisionStay2D. Como isGrounded se recalcula de cero en cada
        // FixedUpdate a partir de esos contactos, el jugador parado quieto se
        // quedaba sin piso y no podia saltar. Un solo cuerpo despierto no
        // cuesta nada.
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
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
            // Despegamos en el acto: isGrounded recien se recalcula en el
            // proximo paso de fisica, y sin esto dos toques muy seguidos de
            // espacio en el mismo paso darian dos saltos.
            isGrounded = false;
            groundedThisStep = false;
        }

        if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
            animator.SetBool("isJumping", false);
    }

    /// <summary>
    /// Unity llama a FixedUpdate ANTES de simular y de tirar los callbacks de
    /// colision, asi que aca publicamos lo que juntaron los callbacks del paso
    /// anterior y arrancamos la cuenta de cero. El retraso es de un paso de
    /// fisica (0,02 s): no se siente, y de yapa deja una pizca de "coyote
    /// time" al salir de una plataforma.
    /// </summary>
    void FixedUpdate()
    {
        isGrounded = groundedThisStep;
        groundedThisStep = false;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        CheckForGround(col);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        CheckForGround(col);
    }

    /// <summary>
    /// Solo suma: nunca borra. Que este collider no sea piso no quiere decir
    /// que no haya piso — puede haberlo informado otro callback del mismo
    /// paso. Ver el comentario de groundedThisStep.
    /// </summary>
    void CheckForGround(Collision2D col)
    {
        foreach (ContactPoint2D contact in col.contacts)
        {
            // La normal apunta hacia el jugador: si viene de abajo, es piso.
            if (contact.normal.y > 0.5f)
            {
                groundedThisStep = true;
                return;
            }
        }
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