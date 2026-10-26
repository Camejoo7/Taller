using UnityEngine;

/// <summary>
/// Kira escribe una página del libro. Dos formas de usarlo:
/// - Con un Collider2D en trigger: se escribe cuando el jugador entra (una
///   franja alta al principio de cada zona del Stage 2).
/// - Sin collider: se llama <see cref="Unlock"/> desde un evento (en el
///   Stage 1, al terminar la intro de Kira, que es cuando entrega el libro).
/// </summary>
public class BookPageUnlock : MonoBehaviour
{
    [Tooltip("El id del tema en el LibroDeKira (print, variables...). " +
             "Vacío = solo entrega el libro, con la portada.")]
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
