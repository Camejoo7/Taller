using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class StageExit : MonoBehaviour
{
    [Header("Destino")]
    public string nextScene = "Stage2";

    [Header("Referencias")]
    public DialogueManager dialogueManager;

    public SceneTransition transition;

    public bool irisTransition = true;

    private bool leaving;

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (leaving) return;

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        leaving = true;
        StartCoroutine(Leave(player));
    }

    IEnumerator Leave(PlayerMovement player)
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        yield return null;

        if (dialogueManager != null && dialogueManager.IsActive)
        {
            player.SetCanMove(false);

            while (dialogueManager.IsActive)
                yield return null;

            player.SetCanMove(false);
        }

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null) PlayerHealth.CarryToNextScene(health.Current);

        if (irisTransition)
        {
            player.SetCanMove(false);
            IrisTransition.LoadScene(nextScene, player.transform);
        }
        else if (transition != null && transition.isActiveAndEnabled)
            transition.CargarEscena(nextScene);
        else
            SceneManager.LoadScene(nextScene);
    }
}
