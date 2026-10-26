using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausaCanvas;

    /// <summary>El libro de Kira lo consulta para no abrirse encima de la pausa.</summary>
    public static bool IsPaused { get; private set; }

    void Start()
    {
        pausaCanvas.SetActive(false);
        IsPaused = false;
    }

    void Update()
    {
        // El ESC que cierra el libro no tiene que abrir también la pausa.
        if (KiraBook.BlocksInput) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused)
                Reanudar();
            else
                Pausar();
        }
    }

    public void Pausar()
    {
        pausaCanvas.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;
    }

    public void Reanudar()
    {
        pausaCanvas.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene("MainMenu");
    }
}
