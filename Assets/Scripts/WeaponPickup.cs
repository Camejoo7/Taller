using UnityEngine;

/// <summary>
/// El arma tirada en la puerta del garage. Flota de arriba abajo para que se
/// note que es algo que se puede agarrar y no parte del decorado.
/// Al tocarla, el jugador queda armado y (si hay diálogo cargado) Kira explica
/// para qué sirve.
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    [Header("Partes")]
    [Tooltip("El dibujo del arma. Flota; el collider se queda quieto.")]
    public Transform visual;

    [Header("Flotación")]
    public float floatAmount = 0.06f;
    public float floatSpeed = 2f;

    [Header("Diálogo al agarrarla")]
    [Tooltip("Opcional. Si está puesto, Kira habla cuando el jugador la levanta.")]
    public DialogueManager dialogueManager;

    public DialogueSegment segmentOnPickup = DialogueSegment.Mid;

    private bool picked = false;
    private Vector3 visualBasePos;

    void Start()
    {
        if (visual != null)
            visualBasePos = visual.localPosition;
    }

    void Update()
    {
        if (picked || visual == null) return;

        float y = visualBasePos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        visual.localPosition = new Vector3(visualBasePos.x, y, visualBasePos.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (picked || !other.CompareTag("Player")) return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        picked = true;
        player.EquipWeapon();

        if (dialogueManager != null)
            dialogueManager.StartDialogue(segmentOnPickup, player);

        gameObject.SetActive(false);
    }
}
