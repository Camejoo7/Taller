using UnityEngine;

public class KiraTriggerZone : MonoBehaviour
{
    public DialogueManager dialogueManager;
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
            dialogueManager.StartDialogue(DialogueSegment.Intro, player);
        }
    }
}