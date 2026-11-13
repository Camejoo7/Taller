using UnityEngine;

public class BookPageUnlock : MonoBehaviour
{
    public string chapterId;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        Unlock();
    }

    public void Unlock()
    {
        if (triggered) return;
        triggered = true;
        KiraBookProgress.Unlock(chapterId);
    }
}
