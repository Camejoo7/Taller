using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// La puerta del final de la stage. Cuando el jugador la toca se va a la stage
/// siguiente — pero si Kira está hablando en ese momento, lo deja quieto ahí
/// hasta que el diálogo termine.
///
/// Esa espera es el punto del script: sin ella, cruzar la puerta en mitad de una
/// línea de Kira se come la explicación, que es justamente lo que el juego viene
/// a enseñar. Mejor que el jugador pierda dos segundos parado a que se pierda el
/// contenido.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StageExit : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Nombre de la escena siguiente. Tiene que estar en Build Settings " +
             "o la carga falla.")]
    public string nextScene = "Stage2";

    [Header("Referencias")]
    [Tooltip("Opcional: si se deja vacío lo busca solo en la escena.")]
    public DialogueManager dialogueManager;

    [Tooltip("Opcional: si se asigna, la salida se hace con el fundido a negro " +
             "en vez de cortar seco. El objeto tiene que estar activo.")]
    public SceneTransition transition;

    private bool leaving;

    void Reset()
    {
        // Es una puerta, no una pared: que no frene al jugador.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (leaving) return;

        // El hijo "Visual" del jugador también tiene el tag Player, así que
        // buscamos el componente y subimos al GameObject que de verdad se mueve.
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        leaving = true;
        StartCoroutine(Leave(player));
    }

    IEnumerator Leave(PlayerMovement player)
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        // Un frame de gracia: si el jugador pisa esta puerta y un disparador de
        // diálogo en el mismo instante, el orden entre los dos triggers no está
        // garantizado y sin esto nos iríamos antes de que Kira abra la boca.
        yield return null;

        if (dialogueManager != null && dialogueManager.IsActive)
        {
            // Lo clavamos en la puerta. No lo volvemos a soltar a propósito: ya
            // se está yendo de la stage y que camine durante el fundido queda
            // raro.
            player.SetCanMove(false);

            while (dialogueManager.IsActive)
                yield return null;

            player.SetCanMove(false);
        }

        if (transition != null && transition.isActiveAndEnabled)
            transition.CargarEscena(nextScene);
        else
            SceneManager.LoadScene(nextScene);
    }
}
