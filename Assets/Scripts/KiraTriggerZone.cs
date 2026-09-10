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
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            dialogueManager.StartDialogue(player);
        }
    }
}