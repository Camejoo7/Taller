using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Partes")]
    public Transform visual;

    [Header("Flotación")]
    public float floatAmount = 0.06f;
    public float floatSpeed = 2f;

    [Header("Diálogo al agarrarla")]
    public DialogueManager dialogueManager;

    public string momentOnPickup;

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
        {
            if (!string.IsNullOrEmpty(momentOnPickup))
                dialogueManager.StartMoment(momentOnPickup, player);
            else
                dialogueManager.StartDialogue(segmentOnPickup, player);
        }

        gameObject.SetActive(false);
    }
}
