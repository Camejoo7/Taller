using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Una franja invisible: cuando el jugador la cruza, Kira habla. Una sola vez.
/// </summary>
public class KiraTriggerZone : MonoBehaviour
{
    public DialogueManager dialogueManager;

    [Tooltip("El id del momento del StageData que dispara (1.1, 2.3...). " +
             "Si está vacío, usa el segmento de abajo (sistema viejo).")]
    public string momentId;

    [Tooltip("Sistema viejo: qué segmento dispara si no hay momentId.")]
    public DialogueSegment segment = DialogueSegment.Intro;

    [Tooltip("Lo que pasa cuando Kira termina de hablar (solo con momentId). " +
             "En el Stage 1, acá entrega el libro.")]
    public UnityEvent onEnded = new UnityEvent();

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

            if (!string.IsNullOrEmpty(momentId))
                dialogueManager.StartMoment(momentId, player, () => { if (onEnded != null) onEnded.Invoke(); });
            else
                dialogueManager.StartDialogue(segment, player);
        }
    }
}
