using UnityEngine;

public class KiraTriggerZone : MonoBehaviour
{
    public DialogueManager dialogueManager;

    [Tooltip("Qué parte del diálogo de la stage dispara. La intro es la de " +
             "siempre; el final del Stage 2 usa la de cierre (Outro).")]
    public DialogueSegment segment = DialogueSegment.Intro;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            // InParent y no GetComponent: el hijo "Visual" del jugador también
            // tiene el tag Player, y si fuera él quien entra al trigger, el
            // congelado del diálogo no se aplicaría.
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            dialogueManager.StartDialogue(segment, player);
        }
    }
}