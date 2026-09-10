using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform droneSpawnPoint;
    public DialogueManager dialogueManager;
    private bool spawned = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!spawned && other.CompareTag("Player"))
        {
            spawned = true;
            Instantiate(enemyPrefab, droneSpawnPoint.position, Quaternion.identity);
            if (dialogueManager != null)
                dialogueManager.StartDialogue(DialogueSegment.Mid);
        }
    }
}