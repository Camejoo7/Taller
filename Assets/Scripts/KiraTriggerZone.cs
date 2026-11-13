using UnityEngine;
using UnityEngine.Events;

public class KiraTriggerZone : MonoBehaviour
{
    public DialogueManager dialogueManager;

    public string momentId;

    public DialogueSegment segment = DialogueSegment.Intro;

    public UnityEvent onEnded = new UnityEvent();

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;

            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

            if (!string.IsNullOrEmpty(momentId))
                dialogueManager.StartMoment(momentId, player, () => { if (onEnded != null) onEnded.Invoke(); });
            else
                dialogueManager.StartDialogue(segment, player);
        }
    }
}
